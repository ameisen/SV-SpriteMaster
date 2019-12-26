using Harmony;
using System;
using static SpriteMaster.HarmonyExt.HarmonyExt;

namespace SpriteMaster.HarmonyExt {
	class HarmonyPatch : HarmonyAttribute {
		public enum Fixation {
			Prefix,
			Postfix,
			Transpile
		}

		public enum Generic {
			None,
			Struct,
			Class
		}

		public readonly Type Type;
		public readonly string Method;
		public readonly int PatchPriority;
		public readonly Fixation PatchFixation;
		public readonly Generic GenericType;

		public HarmonyPatch(Type type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None) {
			Type = type;
			Method = method;
			PatchPriority = (int)priority;
			PatchFixation = fixation;
			GenericType = generic;
		}

		public HarmonyPatch (string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None) :
			this(null, method, fixation, priority, generic) { }
	}
}
