using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types.Exceptions;

internal sealed class ReversePatchException : InvalidOperationException {
	internal ReversePatchException(string message, string member) : base($"Reverse Patch '{member}' : {message}") {
	}

	internal ReversePatchException([CallerMemberName] string member = null!) : base($"Reverse Patch '{member}'") {
	}
}
