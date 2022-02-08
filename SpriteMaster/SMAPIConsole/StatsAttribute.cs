using System;

namespace SpriteMaster.SMAPIConsole;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
sealed class StatsAttribute : Attribute {
	internal readonly string Name;

	internal StatsAttribute(string name) => Name = name;
}
