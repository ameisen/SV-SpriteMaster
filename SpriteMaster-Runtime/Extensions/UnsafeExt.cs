using System.Runtime.CompilerServices;
using static SpriteMaster.Runtime;

namespace SpriteMaster.Extensions;

internal static class UnsafeExt {
	[MethodImpl(MethodImpl.Inline)]
	internal static unsafe int ByteOffsetPinned<OriginType, TargetType>(scoped ref OriginType origin, scoped ref TargetType target)
		where OriginType : struct
		where TargetType : struct {
		return (int)((nint)Unsafe.AsPointer(ref target) - (nint)Unsafe.AsPointer(ref origin));
	}

	[MethodImpl(MethodImpl.Inline)]
	internal static unsafe int ByteOffset<OriginType, TargetType>(scoped ref OriginType origin, scoped ref TargetType target)
		where OriginType : unmanaged
		where TargetType : unmanaged {
		fixed (OriginType* originPtr = &origin) {
			fixed (TargetType* targetPtr = &target) {
				return (int)((nint)targetPtr - (nint)originPtr);
			}
		}
	}
}
