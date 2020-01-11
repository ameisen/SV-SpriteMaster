using Harmony;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static SpriteMaster.HarmonyExt.HarmonyExt;

namespace SpriteMaster.HarmonyExt {
	class HarmonyPatchAttribute : HarmonyAttribute {
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

		public enum Platform {
			All = 0,
			Windows = 1,
			Linux = 2,
			Macintosh = 3,
			Unix = 4
		}

		public readonly Type Type;
		public readonly string Method;
		public readonly int PatchPriority;
		public readonly Fixation PatchFixation;
		public readonly Generic GenericType;
		public readonly bool Instance;
		public readonly Platform ForPlatform;

		internal static bool CheckPlatform(Platform platform) {
			return platform switch
			{
				Platform.All => true,
				Platform.Windows => Runtime.IsWindows,
				Platform.Linux => Runtime.IsLinux,
				Platform.Macintosh => Runtime.IsMacintosh,
				Platform.Unix => Runtime.IsUnix,
				_ => throw new ArgumentOutOfRangeException(nameof(ForPlatform)),
			};
		}

		internal bool CheckPlatform() {
			return CheckPlatform(ForPlatform);
		}

		private static Assembly GetAssembly(string name) {
			return AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == name);
		}

		private static Type ResolveType(Assembly assembly, Type parent, string[] type, int offset = 0) {
			var foundType = parent.GetNestedType(type[offset], BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			offset += 1;
			if (offset >= type.Length)
				return foundType;
			else
				return ResolveType(assembly, foundType, type, offset);
		}

		private static Type ResolveType(Assembly assembly, string[] type, int offset = 0) {
			return ResolveType(assembly, assembly.GetType(type[0], true), type, offset + 1);
		}

		public HarmonyPatchAttribute(Type type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true, Platform platform = Platform.All) {
			Type = type;
			Method = method;
			PatchPriority = (int)priority;
			PatchFixation = fixation;
			GenericType = generic;
			Instance = instance;
			ForPlatform = platform;
		}

		public HarmonyPatchAttribute (string assembly, string type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true, Platform platform = Platform.All) :
			this(
				CheckPlatform(platform) ? GetAssembly(assembly).GetType(type, true) : null,
				method,
				fixation,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonyPatchAttribute (Type parent, string type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true, Platform platform = Platform.All) :
			this(
				CheckPlatform(platform) ? parent.Assembly.GetType(type, true) : null,
				method,
				fixation,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonyPatchAttribute (Type parent, string[] type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true, Platform platform = Platform.All) :
			this(
				CheckPlatform(platform) ? ResolveType(parent.Assembly, type) : null,
				method,
				fixation,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonyPatchAttribute (string assembly, string[] type, string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true, Platform platform = Platform.All) :
			this(
				CheckPlatform(platform) ? ResolveType(GetAssembly(assembly), type) : null,
				method,
				fixation,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonyPatchAttribute (string method, Fixation fixation = Fixation.Prefix, PriorityLevel priority = PriorityLevel.Average, Generic generic = Generic.None, bool instance = true, Platform platform = Platform.All) :
			this(null, method, fixation, priority, generic, instance, platform) { }
	}
}
