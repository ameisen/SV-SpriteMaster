using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal class CommandAttribute : Attribute {
	internal readonly string Name;
	internal readonly string Description;

	internal CommandAttribute(string name, string description) {
		Name = name;
		Description = description;
	}
}
