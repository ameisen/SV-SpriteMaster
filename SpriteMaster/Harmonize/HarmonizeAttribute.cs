using Harmony;
using System;
using System.Linq;
using System.Reflection;
using static SpriteMaster.Harmonize.Harmonize;
using static SpriteMaster.Runtime;

namespace SpriteMaster.Harmonize {
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	internal sealed class HarmonizeAttribute : Attribute {
		public readonly Type Type;
		public readonly string Method;
		public readonly int Priority;
		public readonly AffixType Affix;
		public readonly GenericType Generic;
		public readonly bool Instance;
		public readonly PlatformType ForPlatform;

		internal static bool CheckPlatform(PlatformType platform) {
			return Is(platform);
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

		public HarmonizeAttribute(Type type, string method, AffixType affix = AffixType.Prefix, PriorityLevel priority = PriorityLevel.Average, GenericType generic = GenericType.None, bool instance = true, PlatformType platform = PlatformType.Any) {
			Type = type;
			Method = method;
			Priority = (int)priority;
			Affix = affix;
			Generic = generic;
			Instance = instance;
			ForPlatform = platform;
		}

		public HarmonizeAttribute (string assembly, string type, string method, AffixType affix = AffixType.Prefix, PriorityLevel priority = PriorityLevel.Average, GenericType generic = GenericType.None, bool instance = true, PlatformType platform = PlatformType.Any) :
			this(
				CheckPlatform(platform) ? GetAssembly(assembly).GetType(type, true) : null,
				method,
				affix,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonizeAttribute (Type parent, string type, string method, AffixType affix = AffixType.Prefix, PriorityLevel priority = PriorityLevel.Average, GenericType generic = GenericType.None, bool instance = true, PlatformType platform = PlatformType.Any) :
			this(
				CheckPlatform(platform) ? parent.Assembly.GetType(type, true) : null,
				method,
				affix,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonizeAttribute (Type parent, string[] type, string method, AffixType affix = AffixType.Prefix, PriorityLevel priority = PriorityLevel.Average, GenericType generic = GenericType.None, bool instance = true, PlatformType platform = PlatformType.Any) :
			this(
				CheckPlatform(platform) ? ResolveType(parent.Assembly, type) : null,
				method,
				affix,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonizeAttribute (string assembly, string[] type, string method, AffixType fixation = AffixType.Prefix, PriorityLevel priority = PriorityLevel.Average, GenericType generic = GenericType.None, bool instance = true, PlatformType platform = PlatformType.Any) :
			this(
				CheckPlatform(platform) ? ResolveType(GetAssembly(assembly), type) : null,
				method,
				fixation,
				priority,
				generic,
				instance,
				platform
			) { }

		public HarmonizeAttribute (string method, AffixType fixation = AffixType.Prefix, PriorityLevel priority = PriorityLevel.Average, GenericType generic = GenericType.None, bool instance = true, PlatformType platform = PlatformType.Any) :
			this(null, method, fixation, priority, generic, instance, platform) { }
	}
}
