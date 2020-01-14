using System;

namespace SpriteMaster {
	internal sealed class TextureAction {
		private readonly Action Executor;
		public readonly int Texels;

		internal TextureAction(Action executor, int texels) {
			Executor = executor;
			Texels = texels;
		}

		internal void Invoke() {
			Executor.Invoke();
		}
	}
}
