using System.IO;
using System.Reflection;

namespace SpriteMaster.HarmonyExt.Patches {
	public static class PlatformHelper {
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
