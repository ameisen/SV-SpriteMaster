using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types.Reflection;

[StructLayout(LayoutKind.Auto)]
internal readonly struct VariableAccessor<TObject, TResult> {
	internal readonly VariableInfo Info;
	private readonly Func<TObject, TResult> Getter;
	private readonly Action<TObject, TResult> Setter;
	internal readonly bool HasGetter;
	internal readonly bool HasSetter;

	[DoesNotReturn]
	private static TResult InvalidGetter(VariableInfo info) =>
		ThrowHelper.ThrowInvalidOperationException<TResult>($"Variable '{info}' does not have a valid getter");

	[DoesNotReturn]
	private static void InvalidSetter(VariableInfo info) =>
		ThrowHelper.ThrowInvalidOperationException($"Variable '{info}' does not have a valid setter");

	internal readonly TResult Get(TObject obj) =>
		Getter(obj);

	internal readonly void Set(TObject obj, TResult value) =>
		Setter(obj, value);

	internal readonly VariableStaticAccessor<TResult> Bind(TObject target) => new(
		Info,
		Getter.Method.CreateDelegate<Func<TResult>>(target),
		Setter.Method.CreateDelegate<Action<TResult>>(target)
	);

	internal VariableAccessor(VariableInfo info, Func<TObject, TResult>? getter, Action<TObject, TResult>? setter) {
		Info = info;
		HasGetter = getter is not null;
		Getter = getter ?? (_ => InvalidGetter(info));
		HasSetter = setter is not null;
		Setter = setter ?? ((_, _) => InvalidSetter(info));
	}
}
