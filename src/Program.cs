using System;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Collections.Generic;
using System.Reflection;

namespace ROS2CSMessageGenerator
{
	class MainClass
	{
		public static void PrintHelp ()
		{
			Console.WriteLine ("ROS2CSMessageGenerator version: " + typeof(MainClass).Assembly.GetName ().Version);
			Console.WriteLine ("This tool generates a C# assembly from ROS2 message definitions");
			Console.WriteLine ("Usage: ");
			Console.WriteLine ("  Parse message file and generate cs code:");
			Console.WriteLine ("     mono ROS2CSMessageGenerator.exe -m <path to message file> <package name> <output path>");
			Console.WriteLine ("  Compile generated cs files to assembly:");
			Console.WriteLine ("     mono ROS2CSMessageGenerator.exe -c <directory with cs files> <path to resulting assembly>");
			

		}

		public static void Main (string[] args)
		{
			bool IsService = false;
			if (args.Length < 1) {
				PrintHelp ();
				return;
			}
			if (args [0] == "-m") {
				if (args.Length < 4) {
					PrintHelp ();
					return;
				}
				string messageFile = args [1];
				string packageName = args [2];
				string outputPath = args [3];
				if (!Directory.Exists (outputPath))
					Directory.CreateDirectory (outputPath);

				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine ("Parsing message file: " + messageFile);
				Console.ResetColor();
				if (messageFile.Contains ("Request") || messageFile.Contains ("Response")) {
					IsService = true;
				}
				if (Path.GetExtension (messageFile) == ".srv")
					return;
				
				string name = Path.GetFileName (messageFile);
				name = name.Replace (Path.GetExtension (messageFile), "");
				MessageDescription description = new MessageDescription (messageFile, packageName);
				MessageParser parser = new MessageParser (description);
				try {
					parser.Parse();
				} catch (Exception ex) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ("Exception parsing: " + messageFile);
					Console.ResetColor ();
					Console.WriteLine (ex.ToString ());
					Environment.Exit (1);
				}

				IMessageCodeGenerator codeGenerator = new CodeDomMessageGenerator ();
				codeGenerator.GenerateCode (description);

				//CsClassGenerator generator = new CsClassGenerator (messageFile, packageName);
				//generator.Parse ();
				//generator.FinalizeClass ();
				//Console.WriteLine (generator.GetResultingClass ());
				if (!IsService)
					System.IO.File.WriteAllText (Path.Combine (outputPath, description.Name + "_msg.cs"), codeGenerator.GetGeneratedCode ());
				else
					System.IO.File.WriteAllText (Path.Combine (outputPath, description.Name + "_srv.cs"), codeGenerator.GetGeneratedCode ());
			} else if (args [0] == "-c") {
				if (args.Length < 3) {
					PrintHelp ();
					return;
				}
				string classDir = args [1];
				string assemblyPath = args [2];
				if (!Directory.Exists (classDir)) {
					Console.WriteLine ("Directory does not exist: " + classDir);
					return;
				}
				List<string> cs_files = new List<string> ();
				foreach (var item in Directory.GetFiles(classDir)) {
					FileInfo info = new FileInfo (item);
					if (info.Extension == ".cs") {
						cs_files.Add (item);
					}
				}
				CompileToAssembly (assemblyPath, cs_files);
			} else {
				PrintHelp ();
			}
			Console.WriteLine("");

		}

		public static void CompileToAssembly (string AssemblyPath, List<string> files)
		{

			CompilerParameters cp = new CompilerParameters ();
			cp.CompilerOptions += " /unsafe /warn";

		
			CSharpCodeProvider provider = new CSharpCodeProvider ();


			string rclcsPath = Environment.GetEnvironmentVariable ("AMENT_PREFIX_PATH");
			string firstPathElement = rclcsPath.Split (new char[]{ ':' }) [0];
			rclcsPath = firstPathElement;

			string ros2libPath = Path.Combine (rclcsPath, "lib");
			cp.ReferencedAssemblies.Add ("System.dll");
			foreach (var item in Directory.GetFiles(ros2libPath)) {
				if (Path.GetExtension (item) == ".dll") {
					try {
						System.Reflection.AssemblyName testAssembly = System.Reflection.AssemblyName.GetAssemblyName (item);
						cp.ReferencedAssemblies.Add (item);

					} catch (Exception ex) {
						
					}

				}
			}

			cp.GenerateExecutable = false;
			cp.OutputAssembly = AssemblyPath;

			cp.GenerateInMemory = false;
			try {
				CompilerResults results = Compile(cp, files, provider);

				if (results.Errors.Count > 0) {

					// Display compilation errors.
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ("Errors/Warnings building: " + AssemblyPath);
					Console.ResetColor ();

					Console.WriteLine ("Search path for rclcs was: " + rclcsPath);
					foreach (CompilerError ce in results.Errors) {
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
				} else {
					Console.WriteLine (results.PathToAssembly + " build successfull");

				}
			} catch (Exception ex) {
				Console.WriteLine ("Using fallback without /nowarn option");
				cp.CompilerOptions = "/unsafe";
				CompilerResults results = Compile(cp, files, provider);

				if (results.Errors.Count > 0) {

					// Display compilation errors.
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ("Errors/Warnings building: " + AssemblyPath);
					Console.ResetColor ();

					Console.WriteLine ("Search path for rclcs was: " + rclcsPath);
					foreach (CompilerError ce in results.Errors) {
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
		public static CompilerResults Compile(CompilerParameters cp, List<String> files, CSharpCodeProvider provider)
		{
			return  provider.CompileAssemblyFromFile (cp, files.ToArray ());
		}
	}
}
