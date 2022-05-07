using System;

namespace SpriteMaster.SMAPIConsole;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class StatsAttribute : Attribute {
	internal readonly string Name;

	internal StatsAttribute(string name) => Name = name;
}
