using System.Globalization;

namespace SpriteMaster.Tools.Preview.Preview.Controls;

internal sealed class BooleanCheckBox : CheckBox {
	internal class EventsClass {
		internal Action<bool>? OnChanged { get; set; }
	}

	internal new EventsClass Events { get; init; } = new();

	internal bool Value {
		get => Checked;
		set => Checked = value;
	}

	internal BooleanCheckBox() {
	}

	protected override void OnCheckedChanged(EventArgs args) {
		base.OnCheckedChanged(args);

		Events.OnChanged?.Invoke(Checked);
	}

	public override string ToString() => Checked.ToString(CultureInfo.CurrentCulture);
}