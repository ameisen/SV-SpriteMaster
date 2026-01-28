using System;
using System.Collections.Generic;

namespace SpriteMaster.Types.Exceptions;

internal class UnknownArgumentException : ArgumentOutOfRangeException, IUnknownValueException {
	private static string GetDefaultMessage(object? value) => $"Unknown Argument Value: '{value}'";
	internal readonly object? Value;
	public virtual IEnumerable<object>? LegalValues => null;

	string? IUnknownValueException.Name => ParamName;
	object? IUnknownValueException.Value => Value;
	
	internal UnknownArgumentException(string paramName, object? value) : this(paramName, value, GetDefaultMessage(value)) {
	}

	internal UnknownArgumentException(string paramName, object? value, string message) : base(paramName, value, message) {
		Value = value;
	}
}

internal class UnknownArgumentException<TArgument> : UnknownArgumentException, IUnknownValueException<TArgument> {
	private static string GetDefaultMessage(in TArgument? value) => $"Unknown Argument Value: '{value}'";
	internal new readonly TArgument? Value;

	TArgument? IUnknownValueException<TArgument>.Value => Value;
	public new virtual IEnumerable<TArgument>? LegalValues => null;

	internal UnknownArgumentException(string paramName, in TArgument? value) : this(paramName, value, GetDefaultMessage(value)) {
	}

	internal UnknownArgumentException(string paramName, in TArgument? value, string message) : base(paramName, value, message) {
		Value = value;
	}
}
