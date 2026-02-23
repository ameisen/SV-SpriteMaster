namespace SpriteMaster;

internal static partial class Debug {
	private static void WriteToLogImpl(string str, LogLevel level) {
		Console.WriteLine(str);
	}
}
