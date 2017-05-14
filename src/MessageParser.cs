using System;
using System.IO;
using System.Collections.Generic;

namespace ROS2CSMessageGenerator
{
	public class MessageParser
	{
		private MessageDescription Description;
		//This dictionary contains the mapping between ros message types and c# message types
		private Dictionary<string,string> RosCsTypes = new Dictionary<string, string> ();

		public MessageParser (MessageDescription _Description)
		{
			this.Description = _Description;

			//Fill type conversion table
			RosCsTypes.Add ("bool", "System.Byte");
			RosCsTypes.Add ("char", "System.Char");
			RosCsTypes.Add ("byte", "System.Byte");
			RosCsTypes.Add ("int8", "System.Byte");
			RosCsTypes.Add ("uint8", "System.SByte");
			RosCsTypes.Add ("int16", "System.Int16");
			RosCsTypes.Add ("uint16", "System.UInt16");
			RosCsTypes.Add ("int32", "System.Int32");
			RosCsTypes.Add ("uint32", "System.UInt32");
			RosCsTypes.Add ("int64", "System.Int64");
			RosCsTypes.Add ("uint64", "System.UInt64");
			RosCsTypes.Add ("float32", "System.Single");
			RosCsTypes.Add ("float64", "System.Double");
			RosCsTypes.Add ("string", "rosidl_generator_c__String");
		}

		/// <summary>
		/// Starts the parsing process
		/// </summary>
		public void Parse ()
		{
			
			string[] Lines = Description.InputFile.Split (new string[]{ "\n" }, StringSplitOptions.RemoveEmptyEntries);
			//Step through all lines
			foreach (var item in Lines) {
				//Read a new line
				string LastLine = item;

				//First check if it's an empty line
				if (LastLine.Trim () == "") {
					//Do nothing -> it's nothing ;)
				}
				//Second check if it's a comment
				else if (IsComment (LastLine)) {
					//Do nothing -> it's just a comment
				} 
				//Okey we can start with the real parsing 
				else {
					//Create a new MessageMember and fill it
					MessageMemberDescription MessageMember = new MessageMemberDescription ();

					MessageMember.Name = GetMemberName (LastLine);
					MessageMember.DefaultInitialisation = GetDefaultInitialisation (LastLine);
					MessageMember.IsArray = IsArray (LastLine);
					MessageMember.IsFixedSizeArray = IsFixedSizeArray (LastLine);
					MessageMember.IsBoundedArray = IsBoundedArray(LastLine);
					MessageMember.FixedArraySize = GetFixedArraySize (LastLine);
					MessageMember.IsNested = IsNestedType (LastLine);
					MessageMember.RosType = GetRosMessageType (LastLine);

					//Now we make difference between arrays, nested types and primitive types
					if (MessageMember.IsNested) {

						string NestedNamespace = "";
						string NestedType = "";
						if (MessageMember.RosType.Contains ("/")) {
							//It's a nested type
							NestedNamespace = MessageMember.RosType.Split (new string[]{ "/" }, StringSplitOptions.RemoveEmptyEntries) [0];
							NestedType = MessageMember.RosType.Split (new string[]{ "/" }, StringSplitOptions.RemoveEmptyEntries) [1];
						} else {
							NestedType = MessageMember.RosType;
						}
						//Build the C# type (The full qualified name we can find the generated message afterwards")
						string CsType = NestedNamespace + ".msg." + NestedType + "_t";
						//Fix in case the type is in the same namespace
						if (CsType.StartsWith(".msg."))
							CsType = CsType.Remove(0, 5);
						
						MessageMember.MemberType = CsType;

						//Just some debug outputs
						if (MessageMember.IsArray) {
							//It's a nested array
							Console.WriteLine ("Assuming"+ MessageMember.RosType + "  is a nested array: " + LastLine);

						} else {
							Console.WriteLine ("Assuming "+ MessageMember.RosType + "  is a nested type: " + LastLine);

						}

						//TODO this is an ugly statement
					} else if (MessageMember.IsArray && !MessageMember.IsNested) {
						//TODO How can I check for a nested type without a /
						Console.WriteLine ("Assuming "+ MessageMember.RosType + "  is an array type and NOT a nested type: " + LastLine);
						//It's an array
						if (!MessageMember.IsFixedSizeArray) {
							//Unbounded array
							//Get the primitive type of the array and make it an array type
							string CsType = "rosidl_generator_c__primitive_array_" + MessageMember.RosType;

							//And now let it be the type
							MessageMember.MemberType = CsType;
							MessageMember.ArrayReturnType = GetCsPrimitiveType (MessageMember.RosType);

						} else {
							//Fixed size array
							//It's simply the primitive type
							MessageMember.MemberType = GetCsPrimitiveType (MessageMember.RosType);
						}

					} else if(IsPrimitiveType(MessageMember.RosType)){
						Console.WriteLine ("Assuming "+ MessageMember.RosType + "  is a primitve type: " + LastLine);
						//It's a primitive type
						string CsType = GetCsPrimitiveType (MessageMember.RosType);
						MessageMember.MemberType = CsType;
					} else {
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine ("Unknown Type: " + MessageMember.RosType + " : " + LastLine);
						Console.WriteLine ("Trying the type as a nested type");
						MessageMember.IsNested = true;
						MessageMember.MemberType = MessageMember.RosType;
						Console.ResetColor ();
						continue;
					}
					Description.AddMemberDescription (MessageMember);
				}
			}
		}

		/// <summary>
		/// Checks if the given Line is a comment by checking for a # at the start
		/// </summary>
		/// <returns><c>true</c> if the Line starts with a #; otherwise, <c>false</c>.</returns>
		/// <param name="Line">Line.</param>
		public bool IsComment (string Line)
		{
			return Line.Trim ().StartsWith ("#");
		}

		/// <summary>
		/// Checks if the given line specifes an array (doesn't make a difference between fixed size array and unbounded array
		/// </summary>
		/// <returns><c>true</c> if the Line specifies an array (contains a [ and a ]; otherwise, <c>false</c>.</returns>
		/// <param name="Line">Line.</param>
		public bool IsArray (string Line)
		{
			//We treat a string like an array
			return Line.Contains ("[") && Line.Contains ("]") ;
		}

		/// <summary>
		/// Checks if the given line specifies a bounded array.
		/// </summary>
		/// <returns><c>true</c>, if bounded array was ised, <c>false</c> otherwise.</returns>
		/// <param name="Line">Line.</param>
		public bool IsBoundedArray(string Line)
		{
			return IsArray(Line) && Line.Contains("<=");
		}

		/// <summary>
		/// Checks if the given line specifies a string
		/// </summary>
		/// <returns><c>true</c> if the Line specifies a string; otherwise, <c>false</c>.</returns>
		/// <param name="Line">Line.</param>
		public bool IsString(string Line)
		{
			return Line.StartsWith ("string");
		}
		/// <summary>
		/// Checks if the given line specifies a fixed size array
		/// </summary>
		/// <returns><c>true</c> if this Line specifies a fixed size array; otherwise, <c>false</c>.</returns>
		/// <param name="Line">Line.</param>
		public bool IsFixedSizeArray (string Line)
		{
			if (GetFixedArraySize (Line) > 0)
				return true;
			return false;
		}

		/// <summary>
		/// Gets the size of the fixed array.
		/// </summary>
		/// <returns>The fixed array size. Or -1 in case it is an unbounded array. Or -2 in case it isn't an array</returns>
		/// <param name="Line">Line.</param>
		public int GetFixedArraySize (string Line)
		{
			//Check if it's a string because we need to treat strings like arrays
			if (IsString (Line))
				return -2;
			//First check if it's an array at all
			if (IsArray (Line)) {
				//Extract the size number
				int index_opening_bracket = Line.IndexOf ("[");
				int index_closing_bracket = Line.IndexOf ("]");
				string number = Line.Substring (index_opening_bracket + 1, index_closing_bracket - index_opening_bracket - 1);

				//Just ignore bounded arrays and make them fixed size arrays. //TODO does this work?
				if (IsBoundedArray(Line))
					number.Replace("<=", "");
				//Check that we did extract something
				if (number != "") {
					//Init return value
					int ret_val = -1;
					//Try to parse the extracted string
					if (int.TryParse (number, out ret_val)) {
						//Return array size
						return ret_val;
					}
					//Return -1
					return ret_val;
				} 
				//We didn't extract anything
				else
					return -1;
			} 
			//This wasn't an array
			else {
				return -2;
			}

		}

		/// <summary>
		/// Gets the name of the member.
		/// </summary>
		/// <returns>The member name.</returns>
		/// <param name="Line">Line.</param>
		public string GetMemberName (string Line)
		{
			string MemberName = "";
			//Split the line by empty space
			string[] splitted = Line.Split (new string[]{ " " }, StringSplitOptions.RemoveEmptyEntries);
			if (splitted.Length < 2)
				return "";
			//Check if there's a default initialisation
			if (splitted [1].Contains ("=")) {
				//And remove it
				MemberName = splitted [1].Split (new string[]{ "=" }, StringSplitOptions.RemoveEmptyEntries) [0];
			} else
				MemberName = splitted [1];
			//Do some error handling
			if (MemberName.Trim () == "")
				throw new Exception ("Extracted an empty member name - this is a major error");
			return MemberName;
		}

		/// <summary>
		/// Gets the default initialisation value.
		/// </summary>
		/// <returns>The default initialisation value.</returns>
		/// <param name="Line">Line.</param>
		public string GetDefaultInitialisation (string Line)
		{
			string DefaultInit = "";
			//Check if the line contains a default initialisation value
			if (Line.Contains ("=")) {
				//Extract it
				DefaultInit = Line.Split (new string[]{ "=" }, StringSplitOptions.RemoveEmptyEntries) [1];
			}

			return DefaultInit;
		}

		/// <summary>
		/// Checks if the line specifies a nested type
		/// </summary>
		/// <returns><c>true</c> if this line specifies a nested type; otherwise, <c>false</c>.</returns>
		/// <param name="Line">Line.</param>
		public bool IsNestedType (string Line)
		{
			return !IsPrimitiveType (GetRosMessageType (Line));
			//return Line.Contains ("/");
		}

		/// <summary>
		/// Gets the ros type of the message member
		/// </summary>
		/// <returns>The ros type.</returns>
		/// <param name="Line">Line.</param>
		public string GetRosMessageType (string Line)
		{
			string RosType = "";

			//Split the line by empty space
			string[] Splitted = Line.Split (new string[]{ " " }, StringSplitOptions.RemoveEmptyEntries);
			//The type is in the beginning of the line
			string RawType = Splitted [0];
			//In case it's an array we remove the array brackets
			if (IsArray (Line)) {
				RosType = RawType.Split (new string[]{ "[" }, StringSplitOptions.RemoveEmptyEntries) [0];
			} else
				RosType = RawType;
			if (RosType.Trim () == "")
				throw new ArgumentException ("Parsed an empty type in the message - this looks like an error");
			return RosType;
		}

		/// <summary>
		/// Determines whether the specified RosType is a primitive type.
		/// </summary>
		/// <returns><c>true</c> if this the RosType is a primitive type; otherwise, <c>false</c>.</returns>
		/// <param name="RosType">Ros type.</param>
		public bool IsPrimitiveType (string RosType)
		{
			return RosCsTypes.ContainsKey (RosType);
		}

		/// <summary>
		/// Gets the correlation C# type to the given RosType
		/// </summary>
		/// <returns>The cs primitive type.</returns>
		/// <param name="RosType">Ros type.</param>
		public string GetCsPrimitiveType (string RosType)
		{
			string CsType = "";
			if (RosCsTypes.ContainsKey (RosType)) {
				CsType = RosCsTypes [RosType];
			} else {
				
			
				throw new ArgumentException ("The given type wasn't a primitive type: " + RosType);

			}
			return CsType;

		}
	}
}

