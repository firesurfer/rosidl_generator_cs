using System;
using System.Reflection;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace ROS2CSMessageGenerator
{
	public class CodeDomMessageGenerator:IMessageCodeGenerator
	{
		private CodeCompileUnit TargetUnit;
		private CodeTypeDeclaration MessageStruct;
		private CodeNamespace MessageNamespace;

		public CodeDomMessageGenerator ()
		{
		}
		public void GenerateCode(MessageDescription description)
		{
			//Create a compile unit
			TargetUnit = new CodeCompileUnit ();

			//Create the message namespace with srv or msg.
			if (description.IsService)
				MessageNamespace = new CodeNamespace (description.Namespace + ".srv");
			else
				MessageNamespace = new CodeNamespace (description.Namespace+ ".msg");
			
			//Create the message struct
			MessageStruct = new CodeTypeDeclaration (description.StructName);
			MessageStruct.IsStruct = true;
			MessageStruct.TypeAttributes = TypeAttributes.Public; 

			TargetUnit.Namespaces.Add(MessageNamespace);
		}
		public string GetGeneratedCode()
		{
			CSharpCodeProvider provider = new CSharpCodeProvider ();
			CompilerParameters parameters = new CompilerParameters ();
			return "";
		}
		public bool TestCompileGeneratedCode()
		{
			return false;
		}
	}
}

