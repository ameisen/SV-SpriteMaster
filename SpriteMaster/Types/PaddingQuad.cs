using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types;

[DebuggerDisplay("[{X}, {Y}]")]
[StructLayout(LayoutKind.Sequential, Pack = Vector2I.Alignment * 2, Size = Vector2I.ByteSize * 2)]
internal readonly struct PaddingQuad {
	internal static readonly PaddingQuad Zero = new(Vector2I.Zero, Vector2I.Zero);

	internal readonly Vector2I X;
	internal readonly Vector2I Y;

	internal Vector2I Offset => (X.X, Y.X);

	internal Vector2I InverseOffset => (X.Y, Y.Y);

	internal Vector2I Sum => (X.Sum, Y.Sum);

	internal bool IsZero => X.IsZero && Y.IsZero;

	internal PaddingQuad(Vector2I x, Vector2I y) {
		X = x;
		Y = y;
	}
}
