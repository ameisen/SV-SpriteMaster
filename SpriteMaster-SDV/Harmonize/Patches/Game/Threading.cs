using HarmonyLib;
using SpriteMaster.Extensions;
using SpriteMaster.Extensions.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches.Game;

internal static class Threading {
	private static readonly Func<List<Action>> ThreadingActionsGet = 
		typeof(XNA.Threading).
		GetFieldGetter<List<Action>>("_queuedActions") ??
			throw new NullReferenceException(nameof(ThreadingActionsGet));
	
	[Harmonize(
		typeof(XNA.Threading),
		"Run",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	public static bool Run() {
		if (!SMConfig.IsUnconditionallyEnabled || !SMConfig.Extras.OptimizeEngineTaskRunner) {
			return true;
		}

		var actions = ThreadingActionsGet();
		var localActions = actions.ExchangeClearLocked();

		foreach (var action in localActions) {
			action();
		}

		return false;
	}

	[HarmonizeTranspile(
		typeof(XNA.Threading),
		"EnsureUIThread",
		argumentTypes: new Type[] {},
		platform: Platform.MonoGame,
		instance: false
	)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<CodeInstruction> EnsureUIThreadTranspiler(
		IEnumerable<CodeInstruction> instructions,
		ILGenerator generator
	) {
		var isMainThreadField = typeof(ThreadingExt).GetField(nameof(ThreadingExt.IsMainThread), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ??
			throw new NullReferenceException($"Could not access field '{nameof(ThreadingExt.IsMainThread)}'");

		var throwLabel = generator.DefineLabel();
		yield return new(OpCodes.Ldsfld, isMainThreadField);
		yield return new(OpCodes.Brfalse_S, throwLabel);
		yield return new(OpCodes.Ret);

		yield return new(OpCodes.Ldstr, "Operation not called on UI thread.") {labels = new() {throwLabel}};
		yield return new(OpCodes.Call, new Action<string>(ThrowHelper.ThrowInvalidOperationException).Method);
		yield return new(OpCodes.Ret);
	}
}
