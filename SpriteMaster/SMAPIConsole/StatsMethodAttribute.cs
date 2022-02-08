using System;

namespace SpriteMaster.SMAPIConsole;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
class StatsMethodAttribute : Attribute {
	internal StatsMethodAttribute() { }
}
