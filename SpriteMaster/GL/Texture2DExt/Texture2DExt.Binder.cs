using MonoGame.OpenGL;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.GL;

internal static partial class Texture2DExt {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static GLExt.ObjectId GetCurrentBoundTexture2D() {
		const int TextureBinding2D = 0x8069;

		int result = 0;
		unsafe {
			MonoGame.OpenGL.GL.GetIntegerv(TextureBinding2D, &result);
			GLExt.CheckError();
		}

		return (GLExt.ObjectId)result;
	}

	[StructLayout(LayoutKind.Auto)]
	private readonly ref struct TextureBinder {
		private readonly GLExt.ObjectId PreviousTexture;
		private readonly GLExt.ObjectId CurrentTexture;

		[MethodImpl(Runtime.MethodImpl.Inline)]
		internal TextureBinder(GLExt.ObjectId texture) {
			var boundTexture = GetCurrentBoundTexture2D();
			PreviousTexture = boundTexture;
			CurrentTexture = texture;
			if (texture == boundTexture) {
				return;
			}

			GLExt.Checked(() => MonoGame.OpenGL.GL.BindTexture(TextureTarget.Texture2D, (int)texture));
		}

		[MethodImpl(Runtime.MethodImpl.Inline)]
		internal TextureBinder(Func<GLExt.ObjectId> textureFactory) {
			var boundTexture = GetCurrentBoundTexture2D();
			PreviousTexture = boundTexture;

			var texture = textureFactory();
			CurrentTexture = texture;
			if (texture == boundTexture) {
				return;
			}

			GLExt.Checked(() => MonoGame.OpenGL.GL.BindTexture(TextureTarget.Texture2D, (int)texture));
		}

		[MethodImpl(Runtime.MethodImpl.Inline)]
		internal void Dispose() {
			var previousTexture = PreviousTexture;

			if (previousTexture == CurrentTexture) {
				return;
			}

			GLExt.Checked(() => MonoGame.OpenGL.GL.BindTexture(TextureTarget.Texture2D, (int)previousTexture));
		}
	}
}
