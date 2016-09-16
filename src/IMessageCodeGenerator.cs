using System;

namespace ROS2CSMessageGenerator
{
	public interface IMessageCodeGenerator
	{
		void GenerateCode(MessageDescription description);
		string GetGeneratedCode();
		bool TestCompileGeneratedCode(string rclcsPath, string[] referencesPaths);
	}
}

