using System;

namespace ROS2CSMessageGenerator
{
	public class RoselynMessageGenerator:IMessageCodeGenerator
	{
		public RoselynMessageGenerator ()
		{
		}
		public void GenerateCode(MessageDescription description)
		{

		}
		public string GetGeneratedCode()
		{
			return "";
		}
		public bool TestCompileGeneratedCode(string rclcsPath, string[] referencesPaths)
		{
			return false;
		}
	}
}

