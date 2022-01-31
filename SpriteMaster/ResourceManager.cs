using SpriteMaster.Metadata;
using SpriteMaster.Types;

namespace SpriteMaster;

static class ResourceManager {
	internal static readonly ConcurrentConsumer<string> ReleasedTextureMetas =
		new("ReleasedTextureMetas", Texture2DMeta.Cleanup);
	internal static readonly ConcurrentConsumer<ManagedSpriteInstance.CleanupData> ReleasedSpriteInstances =
		new("ReleasedSpriteInstances", ManagedSpriteInstance.Cleanup);
}
