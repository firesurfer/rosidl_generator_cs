using System;

namespace ROS2CSMessageGenerator
{
	/// <summary>
	/// Describes a parsed message member
	/// </summary>
	public class MessageMemberDescription
	{
		public string Name{ get; set; }
		public string MemberType{ get; set; }
		public string ArrayReturnType { get; set;}
		public string RosType { get; set; }
		public string DefaultInitialisation { get; set; }
		public bool IsArray { get; set; }
		public bool IsNested{ get; set; }
		public bool IsFixedSizeArray { get; set; }
		public bool IsBoundedArray { get; set; }
		public int FixedArraySize { get; set; }

		public MessageMemberDescription ()
		{
		}

		public MessageMemberDescription (string _Name, string _MemberType, int _FixedArraySize) : this (_Name, _MemberType, true, true, _FixedArraySize)
		{

		}

		public MessageMemberDescription (string _Name, string _MemberType) : this (_Name, _MemberType, false, false, 0)
		{

		}

		public MessageMemberDescription (string _Name, string _MemberType, bool _IsArray, bool _IsFixedSizeArray, int _FixedArraySize)
		{
			this.Name = _Name;
			this.MemberType = _MemberType;
			this.IsArray = _IsArray;
			if (_IsFixedSizeArray && !_IsFixedSizeArray)
				throw new Exception ("You need to set IsArray=true in case it's a FixedSizeArray");
			this.IsFixedSizeArray = _IsFixedSizeArray;
			if (_IsFixedSizeArray && FixedArraySize < 1)
				throw new Exception ("FixedArraySize has to be larger than 0");
			this.FixedArraySize = _FixedArraySize;
		}
	}
}

