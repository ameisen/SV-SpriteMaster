using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

#nullable enable

namespace SpriteMaster;

static class SystemInfo {
	internal static void Dump(GraphicsDeviceManager gdm, GraphicsDevice device) {
		Debug.MessageLn("System Information:");

		try {
			Debug.MessageLn($"\tArchitecture: {(Environment.Is64BitProcess ? "x64" : "x86")}");
			Debug.MessageLn($"\tNumber of Cores: {Environment.ProcessorCount}");
		}
		catch { }

		try {
			if (!(device?.IsDisposed).GetValueOrDefault(false)) {
				var adapter = device?.Adapter;
				if (adapter != null) {
					Debug.MessageLn($"\tGraphics Adapter: {adapter}");
					Debug.MessageLn($"\tGraphics Adapter Description: {adapter.Description}");
				}
			}
		}
		catch { }
	}
}
