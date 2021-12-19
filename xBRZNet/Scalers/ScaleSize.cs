using System.Linq;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Scalers {
	internal static class ScaleSize {
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static IScaler ToIScaler (this uint scaleSize, in Config config) {
			// MJY: Need value checks to assure scaleSize is between 2-5 inclusive.
			switch (scaleSize) {
				case 2:
					return new Scaler2X(config);
				case 3:
					return new Scaler3X(config);
				case 4:
					return new Scaler4X(config);
				case 5:
					return new Scaler5X(config);
				case 6:
					return new Scaler6X(config);
				default:
					return null;
			}
		}
	}
}
