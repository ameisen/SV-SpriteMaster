using SpriteMaster.Resample.Scalers.SuperXBR.Cg;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Resample.Scalers.SuperXBR.Passes;

internal abstract class Pass {
	protected readonly Config Configuration;
	protected readonly Vector2I SourceSize;
	protected readonly Vector2I TargetSize;
	protected Pass(Config config, Vector2I sourceSize, Vector2I targetSize) {
		Configuration = config;
		SourceSize = sourceSize;
		TargetSize = targetSize;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	protected int GetX(int x, Vector2I size) {
		if (Configuration.Wrapped.X) {
			x = (x + size.Width) % size.Width;
		}
		else {
			x = Math.Clamp(x, 0, size.Width - 1);
		}
		return x;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	protected int GetY(int y, Vector2I size) {
		if (Configuration.Wrapped.Y) {
			y = (y + size.Height) % size.Height;
		}
		else {
			y = Math.Clamp(y, 0, size.Height - 1);
		}
		return y;
	}

	protected readonly ref struct Texture {
		private readonly Pass Pass;
		private readonly ReadOnlySpan<Float4> Data;
		private readonly Vector2I Size;

		internal Texture(Pass pass, ReadOnlySpan<Float4> data, Vector2I size) {
			Pass = pass;
			Data = data;
			Size = size;
		}
		private int GetOffset(int x, int y) {
			if (Pass.Configuration.Wrapped.X) {
				x = (x + Size.Width) % Size.Width;
			}
			else {
				x = Math.Clamp(x, 0, Size.Width - 1);
			}
			if (Pass.Configuration.Wrapped.Y) {
				y = (y + Size.Height) % Size.Height;
			}
			else {
				y = Math.Clamp(y, 0, Size.Height - 1);
			}

			var offset = (y * Size.Width) + x;
			if (offset < 0 || offset >= Data.Length) {
				throw new IndexOutOfRangeException($"{offset} <\\> {Data.Length}");
			}
			return offset;
		}

		internal readonly Float4 Sample(int x, int y) {
			return Data[GetOffset(x, y)];
		}

		internal readonly Float4 Sample(in Float2 xy) => Sample((int)xy.X, (int)xy.Y);

		internal readonly Float4 Sample(in Vector2I xy) => Sample(xy.X, xy.Y);
	}
}
