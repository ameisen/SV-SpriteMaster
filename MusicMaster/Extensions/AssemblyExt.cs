using LinqFasterer;
using System;
using System.Reflection;

namespace MusicMaster.Extensions;

internal static class AssemblyExt {
	internal static Assembly? GetAssembly(string assemblyName) =>
		AppDomain.CurrentDomain.GetAssemblies().SingleOrDefaultF(assembly => assembly.GetName().Name == assemblyName);

	internal static Assembly GetRequiredAssembly(string assemblyName) =>
		GetAssembly(assemblyName) ?? ThrowHelper.ThrowNullReferenceException<Assembly>($"Could not find required assembly '{assemblyName}'");
}
