using System;

namespace ROS2CSMessageGenerator
{
	public class CodeDomMessageGenerator:IMessageCodeGenerator
	{
		public CodeDomMessageGenerator ()
		{
		}
		public void GenerateCode(MessageDescription description)
		{

		}
		public string GetGeneratedCode()
		{
			return "";
		}
		public bool TestCompileGeneratedCode()
		{
			return false;
		}
	}
}

