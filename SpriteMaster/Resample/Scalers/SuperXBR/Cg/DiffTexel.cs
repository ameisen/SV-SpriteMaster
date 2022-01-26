namespace SpriteMaster.Resample.Scalers.SuperXBR.Cg;

ref struct DiffTexel {
	internal readonly float YUV;
	internal readonly float Alpha;

	internal DiffTexel(float yuv, float alpha) {
		YUV = yuv;
		Alpha = alpha;
	}
}
