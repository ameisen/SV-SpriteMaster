using JetBrains.Annotations;

#if SM_LIBRARY
namespace SpriteMaster;
#else
namespace SpriteMaster.Resample;
#endif

#if SM_LIBRARY
[PublicAPI]
public
#else
internal
#endif
enum Scaler : int {
	None = -1,
	xBRZ = 0,
#if !SHIPPING
	SuperXBR,
#endif
	EPX,
	ScaleX = EPX,
	EPXLegacy,
	xBREPX
}
