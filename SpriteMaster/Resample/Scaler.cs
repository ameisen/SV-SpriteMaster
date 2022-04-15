namespace SpriteMaster.Resample;

enum Scaler : int {
	None = -1,
	xBRZ = 0,
#if !SHIPPING
	SuperXBR,
#endif
	EPX,
	ScaleX = EPX,
#if !SHIPPING
	Bilinear
#endif
}
