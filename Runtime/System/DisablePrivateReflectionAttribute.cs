namespace System.Runtime.CompilerServices {
  using System;

  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
  public sealed class DisablePrivateReflectionAttribute : Attribute {
    public DisablePrivateReflectionAttribute () { }
  }
}
