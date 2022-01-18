using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Scalers.SuperXBR;

sealed class Config : IEquatable<Config> {
	internal const int MaxScale = 8;

	internal readonly Vector2B Wrapped;
	internal readonly bool HasAlpha;

	// default, minimum, maximum, optional step

	internal readonly float EdgeStrength = 2.0f; 
	internal readonly float Weight = 1.0f;
	internal readonly float EdgeShape = 0.0f;
	internal readonly float TextureShape = 0.0f;
	internal readonly float AntiRinging = 1.0f;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Config(
		Vector2B wrapped,
		bool hasAlpha = true
	) {
		Wrapped = wrapped;
		HasAlpha = hasAlpha;
	}

	public bool Equals(Config? other) {
		try {
			foreach (var field in typeof(Config).GetFields()) {
				var leftField = field.GetValue(this);
				var rightField = field.GetValue(other);
				// TODO possibly fall back on IComparable
				if (leftField is null) {
					return rightField is null;
				}
				if (!leftField.Equals(rightField)) {
					return false;
				}
			}
			return true;
		}
		catch {
			return false;
		}
	}
}
