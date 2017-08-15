using System;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Collections.Generic;
using System.Reflection;

namespace rosidl_generator_cs
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
		//TODO increase perfomance by parsing all messages in a package at once
		public static void Main (string[] args)
		{
			bool IsService = false;
			//Check the amount of arguments
			if (args.Length < 1) {
				PrintHelp ();
				return;
			}
			if (args [0] == "-m") {
				//-m means we want te generate messages
				if (args.Length < 4) {
					PrintHelp ();
					return;
				}
				string messageFile = args [1];
				string packageName = args [2];
				string outputPath = args [3];
				//Check if the output paths exists
				if (!Directory.Exists (outputPath))
					Directory.CreateDirectory (outputPath);

				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine ("Parsing message file: " + messageFile);
				Console.ResetColor ();
				//Determine if we are processing a service
				if (messageFile.Contains ("Request") || messageFile.Contains ("Response")) {
					IsService = true;
				}
				//We dont need to process to srv file. Just the two messages resulting by the srv file
				if (Path.GetExtension (messageFile) == ".srv")
					return;

				//Get the message name
				string name = Path.GetFileName (messageFile);
				//Remove the extenstion
				name = name.Replace (Path.GetExtension (messageFile), "");
				//Generate a message description
				MessageDescription description = new MessageDescription (messageFile, packageName);
				//Parse the message
				MessageParser parser = new MessageParser (description);
				try {
					parser.Parse ();
				} catch (Exception ex) {
					//Parsing went wrong
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ("Exception parsing: " + messageFile);
					Console.ResetColor ();
					Console.WriteLine (ex.ToString ());
					Environment.Exit (1);
				}
				//Generate code from the description
				IMessageCodeGenerator codeGenerator = new CodeDomMessageGenerator ();
				codeGenerator.GenerateCode (description);

				//Write the generated code to a text file
				if (!IsService)
					System.IO.File.WriteAllText (Path.Combine (outputPath, description.Name + "_msg.cs"), codeGenerator.GetGeneratedCode ());
				else
					System.IO.File.WriteAllText (Path.Combine (outputPath, description.Name + "_srv.cs"), codeGenerator.GetGeneratedCode ());
			} 
			else if (args [0] == "-c") {
				//-c means we want to compile a message package
				if (args.Length < 3) {
					PrintHelp ();
					return;
				}
				//The directory the .cs files lay in
				string classDir = args [1];
				//This converts the / to \ on windows and leaves them / on linux
				classDir = Path.GetFullPath (classDir);
				Console.WriteLine (classDir);
               
				//The path we want to place the resulting assembly in
				string assemblyPath = args [2];
				assemblyPath = Path.GetFullPath (assemblyPath);
				Console.WriteLine ("Assembly Path: " + assemblyPath);
              	
				//The passed directory did not exists
				if (!Directory.Exists (classDir)) {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ("Directory does not exist: " + classDir);
					Console.ResetColor ();
					return;
				}
				//Get all C# files in the directory
				List<string> cs_files = new List<string> ();
				foreach (var item in Directory.GetFiles(classDir)) {
					FileInfo info = new FileInfo (item);
					if (info.Extension == ".cs") {
						cs_files.Add (item);
					}
				}
				//Compile them
				CompileToAssembly (assemblyPath, cs_files);
			} 
			else {
				PrintHelp ();
			}
			Console.WriteLine ("");

		}

		public static void CompileToAssembly (string AssemblyPath, List<string> files)
		{

			CompilerParameters cp = new CompilerParameters ();

			//We need to allow unsafe code
			cp.CompilerOptions += " /unsafe ";

			string AssemblyName = Path.GetFileNameWithoutExtension (AssemblyPath);
			CSharpCodeProvider provider = new CSharpCodeProvider ();

			//Retrieve search paths from the AMENT_PREFIX_PATH
			string rclcsPath = Environment.GetEnvironmentVariable ("AMENT_PREFIX_PATH");
			Console.WriteLine ("Ament Prefix Path: " + rclcsPath);

			string[] pathElements;
			//On linux paths variables are seperated by : on windows by ;
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				pathElements = rclcsPath.Split (new char[] { ':' });
			} else {
				pathElements = rclcsPath.Split (new char[] { ';' });
			}

			//We need to reference the System.dll
			cp.ReferencedAssemblies.Add ("System.dll");

			//For all elements in the AMENT_PREFIX_PATH
			foreach (var pathElement in pathElements) {

				//Look into the lib folder
				//TODO on windows look into bin folder?
				string ros2libPath = Path.Combine (pathElement, "lib");
   				
				Console.WriteLine ("ros2 libs path: " + ros2libPath);
				//Check the files
				foreach (var item in Directory.GetFiles(ros2libPath)) {
					//A dll could be an assembly
					if (Path.GetExtension (item) == ".dll") {
						try {
							//Try loading the assembly -> if it works it is an assembly if not it's just normal dll
							System.Reflection.AssemblyName testAssembly = System.Reflection.AssemblyName.GetAssemblyName (item);
							Console.WriteLine (testAssembly.FullName);
							if(testAssembly.Name != AssemblyName)
								cp.ReferencedAssemblies.Add (item);

						} catch (Exception ex) {

						}

					}
				}
			}
			//messages allways should result in a library
			cp.GenerateExecutable = false;
			cp.OutputAssembly = AssemblyPath;
			cp.GenerateInMemory = false;

			try {
				//Compile it
				CompilerResults results = Compile (cp, files, provider);

				if (results.Errors.Count > 0) {

					// Display compilation errors.
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine ("Errors/Warnings building: " + AssemblyPath);
					Console.ResetColor ();

					Console.WriteLine ("AMENT_PREFIX_PATH was: " + rclcsPath);
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
				Console.WriteLine (ex.ToString ());
			}

		}

		public static CompilerResults Compile (CompilerParameters cp, List<String> files, CSharpCodeProvider provider)
		{
			return  provider.CompileAssemblyFromFile (cp, files.ToArray ());
		}
	}
}

