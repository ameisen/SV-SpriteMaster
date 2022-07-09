﻿using System.Globalization;

namespace SpriteMaster.Tools.Preview.Preview.Controls;

internal sealed class ScaleControl : NumericUpDown {
	internal class EventsClass {
		internal Action<uint>? OnChanged { get; set; }
	}

	internal new EventsClass Events { get; init; } = new();
	internal uint? MinValueInternal;
	internal uint? MaxValueInternal;

	internal uint? MinValue {
		get => MinValueInternal;
		set {
			Minimum = value ?? 0;
			MinValueInternal = value;
		}
	}
	internal uint? MaxValue {
		get => MaxValueInternal;
		set {
			Maximum = value ?? 100;
			MaxValueInternal = value;
		}
	}

	internal new uint Value {
		get => (uint)base.Value;
		set => base.Value = value;
	}

	internal ScaleControl() {
	}

	protected override void OnChanged(object? source, EventArgs args) {
		base.OnChanged(source, args);

		Events.OnChanged?.Invoke(Value);
	}

	public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);
}
