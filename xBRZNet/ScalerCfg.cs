namespace xBRZNet
{
	public class ScalerConfiguration
	{
		// These are the default values:
		public double LuminanceWeight { get; set; } = 1;
		public double EqualColorTolerance { get; set; } = 30;
		public double DominantDirectionThreshold { get; set; } = 3.6;
		public double SteepDirectionThreshold { get; set; } = 2.2;

		public bool WrappedX { get; set; } = false;
		public bool WrappedY { get; set; } = false;
	}
}
