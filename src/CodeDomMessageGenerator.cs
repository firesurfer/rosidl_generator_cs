using System;
using System.Text;
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
		private CodeTypeDeclaration MessageClass;
		private CodeNamespace MessageNamespace;
		private CompilerResults LastCompilationResults;
		public CodeDomMessageGenerator ()
		{
		}
		/// <summary>
		/// Generates the code.
		/// </summary>
		/// <param name="description">Description.</param>
		public void GenerateCode(MessageDescription description)
		{
			//Create a compile unit
			TargetUnit = new CodeCompileUnit ();

			//Create the message namespace with srv or msg.
			if (description.IsService)
				MessageNamespace = new CodeNamespace (description.Namespace + ".srv");
			else
				MessageNamespace = new CodeNamespace (description.Namespace+ ".msg");
			//Add using statements
			MessageNamespace.Imports.Add (new CodeNamespaceImport ("System"));
			MessageNamespace.Imports.Add (new CodeNamespaceImport ("rclcs"));
			MessageNamespace.Imports.Add (new CodeNamespaceImport ("System.Runtime.InteropServices"));

			//Create the message struct
			MessageStruct = new CodeTypeDeclaration (description.StructName);
			MessageStruct.IsStruct = true;
			MessageStruct.BaseTypes.Add (new CodeTypeReference ("IRosMessage"));
			//Add layoutkind attribute
			CodeAttributeDeclaration StructAttribute = new CodeAttributeDeclaration ("StructLayout",new CodeAttributeArgument(new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(System.Runtime.InteropServices.LayoutKind)),"Sequential")));
			MessageStruct.CustomAttributes.Add (StructAttribute);
			MessageStruct.TypeAttributes = TypeAttributes.Public; 

			//Add struct members
			foreach (var item in description.Members) {
				AddStructMember (item);
			}

			//Add the interop method which obtains the typesupport
			AddTypeIntrospectionMethod (description);
			//Add the free method needed for memory handling
			AddFreeMethod ();
			//Add the struct to the namespace
			MessageNamespace.Types.Add (MessageStruct);
			//Add to namespace
			TargetUnit.Namespaces.Add(MessageNamespace);

			//Create the wrapper class
			MessageClass = new CodeTypeDeclaration(description.Name);
			MessageClass.IsClass = true;
			MessageClass.BaseTypes.Add ("MessageWrapper");
			//Add the wrapper class to the namespace
			MessageNamespace.Types.Add(MessageClass);
		}
		/// <summary>
		/// Gets the generated code.
		/// </summary>
		/// <returns>The generated code as a string.</returns>
		public string GetGeneratedCode()
		{
			CSharpCodeProvider provider = new CSharpCodeProvider ();
			StringBuilder codeString = new StringBuilder ();
			StringWriter writer = new StringWriter (codeString);
			CodeGeneratorOptions generatorOptions = new CodeGeneratorOptions ();
			provider.GenerateCodeFromCompileUnit (TargetUnit, writer, generatorOptions);
			//This adds the unsafe statement which is not supported by codedom ->roselyn supports it
			//We need the unsafe statement for fixed size arrays
			string returnCode = AddUnsafeStatementToStruct (codeString.ToString());
			return returnCode;
		}
		/// <summary>
		/// Adds the unsafe statement to struct code.
		/// </summary>
		/// <returns>The generated code with the unsafe statement.</returns>
		/// <param name="code">Code.</param>
		private string AddUnsafeStatementToStruct(string code)
		{
			int struct_pos = code.IndexOf ("struct");
			code = code.Insert (struct_pos, " unsafe ");
			return code;
		}
		/// <summary>
		/// Tests if the generated code compiles.
		/// </summary>
		/// <returns><c>true</c>, if the generated code compiles, <c>false</c> otherwise.</returns>
		/// <param name="rclcsPath">Rclcs path.</param>
		/// <param name="referencesPaths">References paths.</param>
		public bool TestCompileGeneratedCode(string rclcsPath, string[] referencesPaths)
		{
			CSharpCodeProvider provider = new CSharpCodeProvider ();
			CompilerParameters compilerOptions = new CompilerParameters ();
			compilerOptions.CompilerOptions += " /unsafe";
			//Add the needed references to the compiler parameters
			compilerOptions.ReferencedAssemblies.Add ("System");
			compilerOptions.ReferencedAssemblies.Add (rclcsPath);
			compilerOptions.ReferencedAssemblies.AddRange (referencesPaths);
			//And compile it
			LastCompilationResults = provider.CompileAssemblyFromSource(compilerOptions, new string[]{GetGeneratedCode()});
			return false;
		}
		/// <summary>
		/// Gets the last compilation results.
		/// </summary>
		/// <returns>The last compilation results.</returns>
		public CompilerResults GetLastCompilationResults()
		{
			if (LastCompilationResults == null)
				throw new NullReferenceException ("You need to start a compilation before");
			return LastCompilationResults;
		}
		/// <summary>
		/// Adds the given struct member to the struct.
		/// </summary>
		/// <param name="member">Member.</param>
		private void AddStructMember(MessageMemberDescription member)
		{

			if (!member.IsFixedSizeArray) {
				//Create new field
				CodeMemberField memberField = new CodeMemberField();
				//Set it to public
				memberField.Attributes = MemberAttributes.Public ;
				//Set the member name
				memberField.Name = member.Name;
				//Set the member type 
				memberField.Type = new CodeTypeReference (member.MemberType);
				//Add the field to the struct
				MessageStruct.Members.Add (memberField);
			}
			else {
				//For a fixed size array we need a customMeber
				CodeSnippetTypeMember customMember = new CodeSnippetTypeMember ();
				//With some custom code
				customMember.Text = "        public fixed " + member.MemberType + " " + member.Name + "["+member.FixedArraySize.ToString()+"] ;";
				//Add this code to the struct
				MessageStruct.Members.Add (customMember);
			}
				

		}
		/// <summary>
		/// Adds the type introspection method.
		/// </summary>
		/// <param name="description">Message description.</param>
		private void AddTypeIntrospectionMethod(MessageDescription description)
		{
			CodeSnippetTypeMember introspectionMethod = new CodeSnippetTypeMember ();
			string introspectionMethodName = "";
			if (description.IsService) {
				string tempName = description.Name.Replace ("_", "");
				tempName = tempName.Replace ("Request", "");
				tempName = tempName.Replace ("Response", "");
				string	func = "rosidl_typesupport_introspection_c_get_message__"+description.Namespace+"__srv__"+tempName;
			} else {
				introspectionMethodName = "rosidl_typesupport_introspection_c_get_message__"+description.Namespace+"__msg__"+description.Name;
			}
			introspectionMethod.Text = "        [DllImport (\"lib" + description.Namespace + "__rosidl_typesupport_introspection_c.so\")]\n"+
				"        public static extern IntPtr " + introspectionMethodName + "();";
			MessageStruct.Members.Add (introspectionMethod);
		}
		/// <summary>
		/// Adds the free method for memory handling.
		/// </summary>
		private void AddFreeMethod()
		{
			CodeMemberMethod freeMethod = new CodeMemberMethod ();
			freeMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			freeMethod.Name = "Free";
			freeMethod.ReturnType = new CodeTypeReference (typeof(void));

			freeMethod.Comments.Add(new CodeCommentStatement("This method calls free on every sub element if the subelement implements the IRosTransportItem interface"));
			CodeSnippetStatement freeIteration = new CodeSnippetStatement ();
			freeIteration.Value = 
				"           foreach (var item in this.GetType().GetFields()) " +
				"{\n            " +
					"  if (typeof(IRosTransportItem).IsAssignableFrom (item.FieldType))" +
					"{\n               " +
						"    IRosTransportItem ros_transport_item = (IRosTransportItem)item.GetValue(this);" +
					"\n                   ros_transport_item.Free();" +
				"\n              }" +
				"\n           }";
			freeMethod.Statements.Add (freeIteration);
			MessageStruct.Members.Add (freeMethod);
		}
	}
}

