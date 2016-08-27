﻿using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Generic;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
namespace ROS2CSMessageGenerator
{
	public class CsClassGenerator
	{
		StreamReader FileReader;

		public string Namespace{ get; private set;}
		public string Name{ get; private set; }
		public string MsgSubfolder{get;private set;}
		List<string> Members = new List<string>();
		StringBuilder ClassString =  new StringBuilder();
		bool IsService = false;

		bool ClassWasFinalized = false;

		public CsClassGenerator (string _Path, string _PackageName)
		{
			FileReader = new StreamReader (_Path);
			if (_Path.Contains("Reponse") || _Path.Contains("Request")) {
				IsService = true;
			}
			Console.WriteLine ("Is this a service?: " + IsService + " Because filextension is: " + Path.GetExtension (_Path));
			Namespace = _PackageName;
			Console.WriteLine ("Using packagename as namespace: " +Namespace);
			Name = Path.GetFileName (_Path);
			Name = Name.Replace (Path.GetExtension (_Path), "");

			Console.WriteLine ("Using message file name as message name: " + Name);

			//Console.WriteLine ("Preparing class");

			ClassString.AppendLine ("using System;");
			ClassString.AppendLine ("using ROS2Sharp;");
			ClassString.AppendLine ("using System.Runtime.InteropServices;");
			ClassString.AppendLine ("namespace " + Namespace);
			ClassString.AppendLine ("{");

			if(!IsService)
				ClassString.AppendLine ("    namespace msg");
			else
				ClassString.AppendLine ("    namespace srv");
			ClassString.AppendLine ("    {");
			ClassString.AppendLine ("    [StructLayout (LayoutKind.Sequential)]");

			if(!IsService)
				ClassString.AppendLine ("    public struct " + Name + ":IRosMessage");
			else
				ClassString.AppendLine ("    public struct " + Name + ":IRosService");
			
			ClassString.AppendLine ("    {");
			if (!IsService) {
				ClassString.AppendLine ("        [DllImport (\"lib" + Namespace + "__rosidl_typesupport_introspection_c.so\")]");
				ClassString.AppendLine ("        public static extern IntPtr " + GetTypeSupportMessageFunctionName () + "();");
			} else {
				//TODO Generate correct function name
				ClassString.AppendLine ("        [DllImport (\"lib" + Namespace + "__rosidl_typesupport_introspection_c.so\")]");
				ClassString.AppendLine ("        public static extern IntPtr " + GetTypeSupportServiceFunctionName () + "();");
			}
			ClassString.AppendLine ("");
			ClassString.AppendLine( "        public void Free(){}");

		

		}
		public string GetResultingClass()
		{
			if (!ClassWasFinalized)
				throw new Exception ("Class wasn't finalized");
			return ClassString.ToString ();
			
		}
		public void Parse()
		{
			while (!FileReader.EndOfStream) 
			{
				string line = FileReader.ReadLine ();
				//Console.WriteLine (line);

				if (line.Trim () != "") {
					string[] splitted = line.Split (new string[]{ " " }, StringSplitOptions.RemoveEmptyEntries);
					if(IsArray(splitted[0])){
						string nativeType = splitted [0].Remove (splitted [0].IndexOf ("["), 2);
						string csType = GetPrimitiveType (splitted [0].Remove(splitted[0].IndexOf("["),2));
						if (!(csType.Trim () == "")) {
							csType = "rosidl_generator_c__primitive_array_" + nativeType  ;
							string memberName = splitted [1];
							if (memberName.Contains ("=")) {
								memberName = memberName.Split (new char[]{ '=' }) [0];

							}
							//memberName += " = new rosidl_generator_c__primitive_array_" + nativeType + "()";
							//Console.WriteLine ("Adding member of type: " + csType + " with name: " + memberName);
							Members.Add ("public " + csType + " " + memberName + ";");
						}
					}
					else if (IsPrimitiveType (splitted [0])) {
						string csType = GetPrimitiveType (splitted [0]);
						string memberName = splitted [1];
						if (memberName.Contains ("=")) {
							memberName = memberName.Split (new char[]{ '=' }) [0];

						}
						if (csType == "rosidl_generator_c__String") {
							//memberName += "= new rosidl_generator_c__String()";
							Members.Add ("public "+ csType + " " + memberName + ";");
						}
						else if (csType == "System.Boolean") {
							
							Members.Add ("[MarshalAs(UnmanagedType.U1)]\n        public "+ csType + " " + memberName + ";");
						}
						else
						{
							Members.Add ("public "+ csType + " " + memberName + ";");
						}
						//Console.WriteLine ("Adding member of type: " + csType + " with name: " + memberName);

					}
							
				}
			}

		}
		public bool IsComment(string line)
		{
			line = line.Trim ();
			return line.StartsWith ("#");
		}
		public bool IsPrimitiveType(string type)
		{
			if (GetPrimitiveType (type) != "")
				return true;
			return false;
		}
		public bool IsArray(string type)
		{
			if (type.Contains ("[]"))
				return true;
			return false;
		}

		public string GetPrimitiveType(string primitiveType)
		{
			switch (primitiveType) {
			case "bool":
				return "byte";
			case "byte":
				return "System.Byte";
			case "int8":
				return "System.Byte";
			case "uint8":
				return "System.SByte";
			case "int16":
				return "System.Int16";
			case "uint16":
				return "System.UInt16";
			case "int32":
				return "System.Int32";
			case "uint32":
				return "System.UInt32";
			case "int64":
				return "System.Int64";
			case "uint64":
				return "System.UInt64";
			case "float32":
				return "float";
			case "float64":
				return "double";
			case "string":
				return "rosidl_generator_c__String";
				//TODO time and duration
			default:
				//Console.WriteLine ("Error: couldn't parse specified primitive type: " + primitiveType);
				return "";

			}


		}

		public void FinalizeClass()
		{	
			foreach (var item in Members) {
				ClassString.AppendLine ("        " + item);
			}
			ClassString.AppendLine ("    }");
			ClassString.AppendLine ("    }");
			ClassString.AppendLine ("}");
			ClassWasFinalized = true;
		}
		public string GetTypeSupportMessageFunctionName()
		{
			string func = "rosidl_typesupport_introspection_c_get_message__"+Namespace+"__msg__"+Name;
			return func;
		}
		public string GetTypeSupportServiceFunctionName()
		{
			string reducedName = Name.Replace ("_Request", "");
			reducedName = reducedName.Replace("_Response", "");
			string func = "rosidl_typesupport_introspection_c_get_service__"+Namespace+"__srv__"+reducedName;
			return func;
		}


	}
}

