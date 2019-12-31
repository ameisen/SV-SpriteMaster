using Harmony;
using System;
using System.Linq;
using System.Reflection;
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
		public readonly bool Instance;

		private static Assembly GetAssembly(string name) {
			return AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == name);
		}

		public HarmonyPatch(Type type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true) {
			Type = type;
			Method = method;
			PatchPriority = (int)priority;
			PatchFixation = fixation;
			GenericType = generic;
			Instance = instance;
		}

		public HarmonyPatch (string assembly, string type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true) :
			this(
				GetAssembly(assembly).GetType(type, true),
				method,
				fixation,
				priority,
				generic,
				instance
			) { }

		public HarmonyPatch (Type parent, string type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true) :
			this(
				parent.Assembly.GetType(type, true),
				method,
				fixation,
				priority,
				generic,
				instance
			) { }

		public HarmonyPatch (string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true) :
			this(null, method, fixation, priority, generic, instance) { }
	}
}
