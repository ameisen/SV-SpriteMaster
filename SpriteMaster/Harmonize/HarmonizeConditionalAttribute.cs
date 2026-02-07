using StardewModdingAPI;
using System;

namespace SpriteMaster.Harmonize;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal abstract class HarmonizeConditionalAttribute2 : Attribute {
	internal abstract bool Condition { get; }
}

internal enum Comparator {
	Equal,
	NotEqual,
	GreaterThan,
	GreaterThanOrEqual,
	LessThan,
	LessThanOrEqual
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal abstract class HarmonizeVersionConditionalAttribute2 : HarmonizeConditionalAttribute2 {
	internal readonly Comparator Comparator;
	internal readonly StardewModdingAPI.ISemanticVersion Comparand;

	internal HarmonizeVersionConditionalAttribute2(Comparator comparator, StardewModdingAPI.ISemanticVersion comparand) {
		Comparator = comparator;
		Comparand = comparand;
	}

	internal HarmonizeVersionConditionalAttribute2(Comparator comparator, string comparand) {
		Comparator = comparator;
		Comparand = new SemanticVersion(comparand);
	}

	protected bool TestCondition(ISemanticVersion reference) {
		var result = reference.CompareTo(Comparand);

		switch (Comparator) {
			case Comparator.Equal:
				return result == 0;
			case Comparator.NotEqual:
				return result != 0;
			case Comparator.GreaterThan:
				return result > 0;
			case Comparator.GreaterThanOrEqual:
				return result >= 0;
			case Comparator.LessThan:
				return result < 0;
			case Comparator.LessThanOrEqual:
				return result <= 0;
			default:
				throw new InvalidOperationException($"Unknown comparator: '{Comparator}'");
		}
	}
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class HarmonizeSmapiVersionConditionalAttribute : HarmonizeVersionConditionalAttribute2 {
	internal override bool Condition => TestCondition(StardewModdingAPI.Constants.ApiVersion);

	internal HarmonizeSmapiVersionConditionalAttribute(Comparator comparator, StardewModdingAPI.ISemanticVersion comparand) :
		base(comparator, comparand) {
	}

	internal HarmonizeSmapiVersionConditionalAttribute(Comparator comparator, string comparand) :
		base(comparator, comparand) {
	}
}
