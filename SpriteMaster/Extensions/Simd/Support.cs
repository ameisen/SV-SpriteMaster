namespace SpriteMaster.Extensions.Simd;

internal static class Support {
	internal const bool Enabled = true;
	internal static readonly bool Ssse3 = Enabled && System.Runtime.Intrinsics.X86.Ssse3.IsSupported && true;
	internal static readonly bool Sse41 = Enabled && System.Runtime.Intrinsics.X86.Sse41.IsSupported && true;
	internal static readonly bool Bmi2 = Enabled && System.Runtime.Intrinsics.X86.Bmi2.IsSupported && true;
	internal static readonly bool Avx2 = Enabled && System.Runtime.Intrinsics.X86.Avx2.IsSupported && SystemInfo.Instructions.Avx2 && false;
	internal const bool Avx512 = Enabled && false;
}
