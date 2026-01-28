#if !SHIPPING
namespace SpriteMaster.Resample.Scalers.SuperXBR.Cg;

partial struct Float3 {
	internal readonly Float4 XYZ0 => new(Value, 0.0f);
	internal readonly Float4 RGB0 => new(Value, 0.0f);
	internal readonly Float4 XYZ1 => new(Value, 1.0f);
	internal readonly Float4 RGB1 => new(Value, 1.0f);
}
#endif
