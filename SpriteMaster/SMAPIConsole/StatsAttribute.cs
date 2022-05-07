using System;

namespace SpriteMaster.SMAPIConsole;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
internal sealed class StatsAttribute : Attribute {
	internal readonly string Name;

	internal StatsAttribute(string name) => Name = name;
}
