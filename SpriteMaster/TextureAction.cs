using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster;
sealed record TextureAction(Action Executor, int Size) {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Invoke() => Executor.Invoke();
}
