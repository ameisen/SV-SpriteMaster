using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types.Reflection;

[StructLayout(LayoutKind.Auto)]
internal readonly struct VariableStaticAccessor<TResult> {
	internal readonly VariableInfo Info;
	private readonly Func<TResult> Getter;
	private readonly Action<TResult> Setter;
	internal readonly bool HasGetter;
	internal readonly bool HasSetter;

	[DoesNotReturn]
	private static TResult InvalidGetter(VariableInfo info) =>
		ThrowHelper.ThrowInvalidOperationException<TResult>($"Variable '{info}' does not have a valid getter");

	[DoesNotReturn]
	private static void InvalidSetter(VariableInfo info) =>
		ThrowHelper.ThrowInvalidOperationException<TResult>($"Variable '{info}' does not have a valid setter");

	internal readonly TResult Get() =>
		Getter();

	internal readonly void Set(TResult value) =>
		Setter(value);

	internal readonly TResult Value {
		get => Getter();
		set => Setter(value);
	}

	internal VariableStaticAccessor(VariableInfo info, Func<TResult>? getter, Action<TResult>? setter) {
		Info = info;
		HasGetter = getter is not null;
		Getter = getter ?? (() => InvalidGetter(info));
		HasSetter = setter is not null;
		Setter = setter ?? (_ => InvalidSetter(info));
	}
}
