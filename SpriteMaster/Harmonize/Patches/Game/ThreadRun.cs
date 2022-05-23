using SpriteMaster.Configuration;
using SpriteMaster.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Harmonize.Patches.Game;

internal static class ThreadRun {
	private static readonly Func<List<Action>> ThreadingActionsGet = typeof(XColor).Assembly.
	GetType("Microsoft.Xna.Framework.Threading")?.
	GetFieldGetter<List<Action>>("actions") ??
	throw new NullReferenceException("ThreadingActionsGet");

	[Harmonize(
		typeof(XColor),
		"Microsoft.Xna.Framework.Threading",
		"Run",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	public static bool Run() {
		if (!Config.IsUnconditionallyEnabled || !Config.Extras.OptimizeEngineTaskRunner) {
			return true;
		}

		var actions = ThreadingActionsGet();
		Action[] localList;
		lock (actions) {
			localList = actions.ToArray();
			actions.Clear();
		}

		foreach (var action in localList) {
			action();
		}

		return false;
	}
}
