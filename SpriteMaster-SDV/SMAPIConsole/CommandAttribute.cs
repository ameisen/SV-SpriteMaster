using JetBrains.Annotations;
using System;

[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
[AttributeUsage(AttributeTargets.Method)]
internal class CommandAttribute : Attribute {
	internal readonly string Name;
	internal readonly string Description;

	internal CommandAttribute(string name, string description) {
		Name = name;
		Description = description;
	}
}
