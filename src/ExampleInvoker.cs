using System;

namespace ROS2CSMessageGenerator
{
	public class ExampleInvoker
	{
		public static void Main (string[] args)
		{
			//This is a example for myself as long as I change the Program.cs ;)
			MessageDescription mD = new MessageDescription("Test.msg", "test_msgs", "int16 testint\n" +
				"int32 test32int\n" +
				"string teststring\n" +
				"int16[] testintarray\n", false);
			MessageParser parser = new MessageParser (mD);
			parser.Parse ();
			foreach (var item in mD.Members) {
				Console.WriteLine (item.Name + " " + item.MemberType + " " + item.RosType);
			}
		}
	}
}

