using System;
using System.IO;
using System.Collections.Generic;
namespace rosidl_generator_cs
{
	/// <summary>
	/// Message description.
	/// </summary>
	public class MessageDescription
	{
		
		public string InputFile { get; set; }

		public string OutputFile { get; set; }
		public bool IsService {get;set;}
		/// <summary>
		/// Gets or sets the message name.
		/// </summary>
		/// <value>The name.</value>
		public string Name { get; set; }
		public string Namespace { get; set; }
		/// <summary>
		/// Gets or sets the type name of the struct.
		/// The struct name ist the message name + "_T".
		/// </summary>
		/// <value>The name of the struct.</value>
		public string StructName { get; set; }
		public DateTime ParsingDate { get; set; }

		public List<MessageMemberDescription> Members { get; private set; }
		/// <summary>
		/// Initializes a new instance of the <see cref="ROS2CSMessageGenerator.MessageDescription"/> class.
		/// </summary>
		/// <param name="_InputFilePath">Full path to the message file</param>
		/// <param name="_PackageName">Name of the package the message comes from</param>
		public MessageDescription (string _InputFilePath, string _PackageName)
		{
			//Check is input file exists
			if (!File.Exists (_InputFilePath))
				throw new FileNotFoundException ("Path: " + _InputFilePath + " could not be found");
			

			//Now we read the whole file
			using (StreamReader FileReader = new StreamReader (_InputFilePath)) {
				InputFile = FileReader.ReadToEnd ();
			}
				
			//The message name should be the filename of the message and remove the file extension
			Name = Path.GetFileName (_InputFilePath).Replace(Path.GetExtension(_InputFilePath),"");
			//The struct name has simply a _t attached
			StructName = Name +"_t";
			//The namespace is the same as the package name so all parsed messages in a package are in the same namespace afterwards
			Namespace = _PackageName;

			//Check if the file belongs to service
			if (_InputFilePath.Contains("Response") ||  _InputFilePath.Contains("Request")) 
				IsService = true;

			ParsingDate = DateTime.Now;
			Members = new List<MessageMemberDescription> ();
			
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="ROS2CSMessageGenerator.MessageDescription"/> class.
		/// Use this constructor in order to create message code from a file stored in memory
		/// </summary>
		/// <param name="_Name">Message _Name</param>
		/// <param name="_PackageName">Package name.</param>
		/// <param name="_InputFile">Input file.</param>
		public MessageDescription(string _Name, string _Namespace, string _InputFile, bool _IsService)
		{
			//Just set the "File content"
			InputFile = _InputFile;


			Name = _Name;
			//The struct name has simply a _t attached
			StructName = Name +"_t";
			//The user has to know if it's an service
			IsService = _IsService;
			//The namespace is the same as the package name so all parsed messages in a package are in the same namespace afterwards
			Namespace = _Namespace;

			ParsingDate = DateTime.Now;
			Members = new List<MessageMemberDescription> ();
		}
		
		/// <summary>
		/// Adds a new member description to the 
		/// </summary>
		/// <param name="_Description">Description.</param>
		public void AddMemberDescription(MessageMemberDescription _Description)
		{
			Members.Add (_Description);
		}
	}
}

