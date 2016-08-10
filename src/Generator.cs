using System;
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

		bool ClassWasFinalized = false;

		public CsClassGenerator (string _Path, string _PackageName)
		{
			FileReader = new StreamReader (_Path);

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
			ClassString.AppendLine ("    public struct " + Name);
			ClassString.AppendLine ("    {");
			ClassString.AppendLine ("        [DllImport (\"lib"+Namespace+"__rosidl_typesupport_introspection_c.so\")]");
			ClassString.AppendLine ("        public static extern IntPtr " +GetTypeSupportMessageFunctionName()+"();");
			ClassString.AppendLine ("");

		

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
				if (line.StartsWith ("#") || line.StartsWith ("-")) {
				}
				else if (line.Trim () != "") {
					string[] splitted = line.Split (new string[]{ " " }, StringSplitOptions.RemoveEmptyEntries);
					if(IsArray(splitted[0])){
						string csType = GetPrimitiveType (splitted [0].Remove(splitted[0].IndexOf("["),2));
						if (!(csType.Trim () == "")) {
							csType += "[]";
							string memberName = splitted [1];
							if (memberName.Contains ("=")) {
								memberName = memberName.Split (new char[]{ '=' }) [0];
							}
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
						//Console.WriteLine ("Adding member of type: " + csType + " with name: " + memberName);
						Members.Add ("public "+ csType + " " + memberName + ";");
					}
							
				}
			}

		}
		public bool HasInitializer(string type)
		{
			return type.Contains ("=");
				
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
				return "System.Boolean";
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
			ClassString.AppendLine ("}");
			ClassWasFinalized = true;
		}
		public string GetTypeSupportMessageFunctionName()
		{
			string func = "rosidl_typesupport_introspection_c_get_message__"+Namespace+"__"+MsgSubfolder+"msg__"+Name;
			return func;
		}


	}
}

