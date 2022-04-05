using System;

namespace SpriteMaster.Configuration;

static class Attributes {
	internal abstract class ConfigAttribute : Attribute { };

	[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
	internal sealed class CommentAttribute : ConfigAttribute {
		internal readonly string Message;

		internal CommentAttribute(string message) => Message = message;
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	internal sealed class IgnoreAttribute : ConfigAttribute { }

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	internal sealed class RetainAttribute : ConfigAttribute { }

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	internal sealed class OldNameAttribute : ConfigAttribute {
		internal readonly string Name;

		internal OldNameAttribute(string name) => Name = name;
	}

	[AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
	internal sealed class Options : ConfigAttribute {
		[Flags]
		internal enum Flag {
			None = 0,
			FlushTextureCache = 1 << 0,
			FlushSuspendedSpriteCache = 1 << 1,
			FlushFileCache = 1 << 2,
			FlushResidentCache = 1 << 3,
			FlushMetaData = 1 << 4,
			FlushAllInternalCaches = FlushSuspendedSpriteCache | FlushFileCache | FlushResidentCache | FlushMetaData,
			FlushAllCaches = FlushTextureCache | FlushAllInternalCaches,
			GarbageCollect = 1 << 5,
			ResetDisplay = 1 << 6,
		}

		internal readonly Flag Flags = Flag.None;

		internal Options(Flag flags) => Flags = flags;
	}
}
