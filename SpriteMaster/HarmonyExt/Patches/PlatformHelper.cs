using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace SpriteMaster.HarmonyExt.Patches {
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
	internal static class PlatformHelper {
		[HarmonyPatch(
			typeof(TeximpNet.Unmanaged.NvTextureToolsLibrary),
			"TeximpNet.Unmanaged.PlatformHelper",
			"GetAppBaseDirectory",
			HarmonyPatch.Fixation.Prefix,
			HarmonyExt.PriorityLevel.First
		)]
		internal static bool GetAppBaseDirectory(ref string __result) {
			__result = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			return false;
		}
	}
}
