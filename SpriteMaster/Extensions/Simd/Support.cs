namespace SpriteMaster.Extensions.Simd;

internal static class Support {
	internal static readonly bool Ssse3 = System.Runtime.Intrinsics.X86.Ssse3.IsSupported && true;
	internal static readonly bool Sse41 = System.Runtime.Intrinsics.X86.Sse41.IsSupported && true;
	internal static readonly bool Bmi2 = System.Runtime.Intrinsics.X86.Bmi2.IsSupported && true;
	internal static readonly bool Avx2 = System.Runtime.Intrinsics.X86.Avx2.IsSupported && SystemInfo.Instructions.Avx2 && true;
	internal const bool Avx512 = false;
}
