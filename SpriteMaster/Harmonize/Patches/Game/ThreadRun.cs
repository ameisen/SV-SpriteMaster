using SpriteMaster.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Harmonize.Patches.Game;

static class ThreadRun {
	private static readonly Func<List<Action>> ThreadingActionsGet = typeof(XNA.Color).Assembly.
	GetType("Microsoft.Xna.Framework.Threading")?.
	GetFieldGetter<List<Action>>("actions") ??
	throw new NullReferenceException("ThreadingActionsGet");

	[Harmonize(
		typeof(XNA.Color),
		"Microsoft.Xna.Framework.Threading",
		"Run",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool Run() {
		if (!Config.Enabled || !Config.Extras.OptimizeEngineTaskRunner) {
			return true;
		}

		var actions = ThreadingActionsGet();
		List<Action> localList;
		lock (actions) {
			localList = new(actions);
			actions.Clear();
		}

		foreach (var action in localList) {
			action();
		}

		return false;
	}
}
