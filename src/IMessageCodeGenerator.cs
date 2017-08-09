using System;

namespace rosidl_generator_cs
{
	public interface IMessageCodeGenerator
	{
		void GenerateCode(MessageDescription description);
		string GetGeneratedCode();
		bool TestCompileGeneratedCode(string rclcsPath, string[] referencesPaths);
	}
}

