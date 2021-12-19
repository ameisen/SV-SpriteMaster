namespace System.Runtime.CompilerServices {
  using System;

  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
  internal sealed class DisablePrivateReflectionAttribute : Attribute {
    internal DisablePrivateReflectionAttribute () { }
  }
}
