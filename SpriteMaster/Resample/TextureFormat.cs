using SpriteMaster.Extensions;
using SpriteMaster.Hashing;
using SpriteMaster.Types;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using XSurfaceFormat = Microsoft.Xna.Framework.Graphics.SurfaceFormat;

namespace SpriteMaster.Resample;

[StructLayout(LayoutKind.Auto)]
internal readonly struct TextureFormat : IEquatable<TextureFormat> {

	[MarshalAs(UnmanagedType.I4)]
	private readonly XSurfaceFormat SurfaceFormat;
	[MarshalAs(UnmanagedType.I4)]
	private readonly CompressionFormat CompressionFormat;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal TextureFormat(XSurfaceFormat surfaceFormat, CompressionFormat compressionFormat) {
		SurfaceFormat = surfaceFormat;
		CompressionFormat = compressionFormat;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public static implicit operator XSurfaceFormat(TextureFormat format) => format.SurfaceFormat;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	public static implicit operator CompressionFormat(TextureFormat format) => format.CompressionFormat;

	internal bool IsSupported => SMConfig.Resample.SupportedFormats.Contains(this);

	internal TextureFormat? SupportedOr => IsSupported ? this : null;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal long SizeBytes(int area) => SurfaceFormat.SizeBytes(area);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal long SizeBytes(Vector2I size) => SurfaceFormat.SizeBytes(size);

	internal static readonly TextureFormat None = new((XSurfaceFormat)(-1), (CompressionFormat)(-1));

	internal static readonly TextureFormat Color = new(XSurfaceFormat.Color, CompressionFormat.BGRA);
	internal static readonly TextureFormat ColorS = new(XSurfaceFormat.ColorSRgb, CompressionFormat.BGRA);

	internal static readonly TextureFormat ColorHalf = new(XSurfaceFormat.Bgra4444, CompressionFormat.BGRA);
	internal static readonly TextureFormat ColorHalfPunchthroughAlpha = new(XSurfaceFormat.Bgra5551, CompressionFormat.BGRA);
	internal static readonly TextureFormat ColorHalfNoAlpha = new(XSurfaceFormat.Bgr565, CompressionFormat.BGRA);

	internal static readonly TextureFormat AlphaOnly = new(XSurfaceFormat.Alpha8, CompressionFormat.BGRA);

	internal static readonly TextureFormat BC3 = new(XSurfaceFormat.Dxt5, CompressionFormat.BC3);
	internal static readonly TextureFormat BC3S = new(XSurfaceFormat.Dxt5SRgb, CompressionFormat.BC3);
	internal static readonly TextureFormat BC2 = new(XSurfaceFormat.Dxt3, CompressionFormat.BC2);
	internal static readonly TextureFormat BC2S = new(XSurfaceFormat.Dxt3SRgb, CompressionFormat.BC2);
	internal static readonly TextureFormat BC1a = new(XSurfaceFormat.Dxt1a, CompressionFormat.BC1a);
	internal static readonly TextureFormat BC1 = new(XSurfaceFormat.Dxt1, CompressionFormat.BC1);
	internal static readonly TextureFormat BC1S = new(XSurfaceFormat.Dxt1SRgb, CompressionFormat.BC1);

	internal static readonly TextureFormat WithAlpha =							BC3.SupportedOr ?? BC2.SupportedOr ?? Color.SupportedOr ?? BC1a.SupportedOr ?? BC1;
	internal static readonly TextureFormat WithHardAlpha =					BC2.SupportedOr ?? WithAlpha;
	internal static readonly TextureFormat WithPunchthroughAlpha =	BC1a.SupportedOr ?? WithHardAlpha;
	internal static readonly TextureFormat WithNoAlpha =						BC1.SupportedOr ?? WithPunchthroughAlpha;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static TextureFormat? Get(CompressionFormat format) {
		var fields = typeof(TextureFormat).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
		foreach (var field in fields) {
			if (field.FieldType != typeof(TextureFormat))
				continue;
			var formatField = (TextureFormat)field.GetValue(null)!;
			if (formatField == format)
				return formatField;
		}
		return null;
	}

	public static bool operator ==(TextureFormat left, TextureFormat right) => left.SurfaceFormat == right.SurfaceFormat && left.CompressionFormat == right.CompressionFormat;

	public static bool operator !=(TextureFormat left, TextureFormat right) => left.SurfaceFormat != right.SurfaceFormat || left.CompressionFormat != right.CompressionFormat;

	public override bool Equals(object? obj) {
		if (obj is TextureFormat format) {
			return this == format;
		}
		return false;
	}

	public override int GetHashCode() => HashUtility.Combine32(SurfaceFormat, CompressionFormat);

	public bool Equals(TextureFormat other)
	{
		return SurfaceFormat == other.SurfaceFormat && CompressionFormat == other.CompressionFormat;
	}
}
