using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace SpriteMaster;

[Pure]
internal static class ThrowHelper {
	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static T ThrowNullReferenceException<T>(string name) =>
		throw new NullReferenceException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ThrowNullReferenceException(string name) =>
		throw new NullReferenceException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static T ThrowArgumentNullException<T>(string name) =>
		throw new ArgumentNullException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ThrowArgumentNullException(string name) =>
		throw new ArgumentNullException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static T ThrowInvalidOperationException<T>(string name) =>
		throw new InvalidOperationException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ThrowInvalidOperationException(string name) =>
		throw new InvalidOperationException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static T ThrowObjectDisposedException<T>(string name) =>
		throw new ObjectDisposedException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ThrowObjectDisposedException(string name) =>
		throw new ObjectDisposedException(name);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static T ThrowArgumentException<T>(string message, string paramName) =>
		throw new ArgumentException(message, paramName);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ThrowArgumentException(string message, string paramName) =>
		throw new ArgumentException(message, paramName);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static T ThrowNotImplementedException<T>(string message) =>
		throw new NotImplementedException(message);

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void ThrowNotImplementedException(string message) =>
		throw new NotImplementedException(message);
}
