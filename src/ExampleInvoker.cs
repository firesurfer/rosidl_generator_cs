using System;
using System.CodeDom;
using System.CodeDom.Compiler;
namespace ROS2CSMessageGenerator
{
	public class ExampleInvoker
	{
		public static void Main (string[] args)
		{
			//This is a example for myself as long as I change the Program.cs ;)
			MessageDescription mD = new MessageDescription("Dummy", "test_msgs", "int16 testint\n" +
				"int32 test32int\n" +
				"string teststring\n" +
				"string[] teststringarray\n" +
				"int16[] testintarray\n" +
				"int32[4] fixedtesttest234test\n" +
				"int8[] thisisaint8array\n" +
				"builtin_interfaces/Time thisisatime\n", false);


			MessageParser parser = new MessageParser (mD);
			parser.Parse ();
			foreach (var item in mD.Members) {
				Console.WriteLine (item.Name + " " + item.RosType + " " + item.MemberType);
			}
			IMessageCodeGenerator codeGenerator = new CodeDomMessageGenerator ();
			codeGenerator.GenerateCode (mD);
			System.IO.File.WriteAllText ("/home/firesurfer/Test.cs", codeGenerator.GetGeneratedCode ());
			string[] splittedLines = codeGenerator.GetGeneratedCode ().Split (new string[]{ "\n" },StringSplitOptions.None);
			int count = 0;
			foreach (var item in splittedLines) {
				Console.WriteLine (count.ToString () + " " + item);
				count++;
			}

			codeGenerator.TestCompileGeneratedCode ("/home/firesurfer/workspace/ros2_ws/install/lib/rclcs.dll", new string[]{ "/home/firesurfer/workspace/ros2_ws/install/lib/builtin_interfaces.dll"});
			CodeDomMessageGenerator domGenerator = codeGenerator as CodeDomMessageGenerator;
			if (domGenerator.GetLastCompilationResults ().Errors.Count > 0) {
				foreach (CompilerError ce in domGenerator.GetLastCompilationResults().Errors) {
					if (ce.IsWarning) {
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine (ce.FileName + " " + ce.ErrorNumber);
						Console.ResetColor ();
						Console.WriteLine ("  {0}", ce.ToString ());
						Console.WriteLine ();
					} else {

						Console.ForegroundColor = ConsoleColor.DarkRed;
						Console.WriteLine (ce.FileName + " " + ce.ErrorNumber);
						Console.ResetColor ();
						Console.WriteLine ("  {0}", ce.ToString ());
						Console.WriteLine ();
					}
				}
			}
		}
	}
}

