using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	internal sealed class TextureAction {
		private readonly Action Executor;
		internal readonly int Texels;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal TextureAction(Action executor, int texels) {
			Executor = executor;
			Texels = texels;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal void Invoke() {
			Executor.Invoke();
		}
	}
}
