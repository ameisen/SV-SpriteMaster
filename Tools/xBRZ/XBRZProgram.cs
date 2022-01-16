using xBRZ;

namespace SpriteMaster.Tools;

public static class XBRZProgram {
	[STAThread]
	public static int Main(string[] args) {
		if (args.Contains("--ui") || args.Contains("--preview")) {
			return PreviewProgram.SubMain(args);
		}
		else {
			return ConverterProgram.SubMain(args);
		}

		return 0;
	}
}
