namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
sealed class DisablePrivateReflectionAttribute : Attribute {
  internal DisablePrivateReflectionAttribute () { }
}
