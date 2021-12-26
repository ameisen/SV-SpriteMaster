using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster;
sealed record TextureAction(Action Executor, int Texels) {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Invoke() => Executor.Invoke();
}
