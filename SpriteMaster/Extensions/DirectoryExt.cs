﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SpriteMaster.Extensions;

using ManagementObject = IDisposable;

static class DirectoryExt {
	private static MethodInfo? ManagementInvokeMethod = null;
	private static Type? ManagementObjectType = null;

	static DirectoryExt() {
		try {
			if (Runtime.IsWindows) {
				var managementAssembly = Assembly.Load("System.Management");
				ManagementObjectType = managementAssembly?.GetType("System.Management.ManagementObject");

				if (ManagementObjectType is null) {
					return;
				}

				ManagementInvokeMethod = ManagementObjectType.GetMethod("InvokeMethod", new[] { typeof(string), typeof(object[]) });
				if (ManagementInvokeMethod is null) {
					return;
				}
			}
		}
		catch { }
	}

	internal static bool CompressDirectory(string path, bool force = false) {
		if (Runtime.IsWindows) {
			return CompressDirectoryWindows(path, force);
		}

		return false;
	}


	private static readonly object[] CompressArgs = new object[] { "Compress", Array.Empty<object>() };
	private static bool CompressDirectoryWindows(string path, bool force) {
		if (ManagementInvokeMethod is null || ManagementObjectType is null) {
			return false;
		}

		DirectoryInfo? info = null;
		try {
			info = new DirectoryInfo(path);
			if (!force && info.Attributes.HasFlag(FileAttributes.Compressed)) {
				return true;
			}

			var objectPath = $"Win32_Directory.Name='{info.FullName.Replace("\\", @"\\").TrimEnd('\\')}'";

			// I am switching this to use reflection does it doesn't try to search for these assemblies on Unix.

			using var obj = (ManagementObject?)Activator.CreateInstance(ManagementObjectType!, new object[] { objectPath });
			if (obj is null) {
				return false;
			}
			_ = ManagementInvokeMethod.Invoke(obj, CompressArgs);
		}
		catch {
			return false;
		}
		try {
			// Attempt to use LZX compression, if possible
			var processInfo = new ProcessStartInfo("compact.exe") {
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = true
			};
			processInfo.ArgumentList.AddRange("/C", "/I", "/F", $"/S:{path}", "/EXE:LZX");

			var process = Process.Start(processInfo);
			if (process is not null) {
				try {
					ThreadExt.Run(() => {
						try {
							process.WaitForExit();
						}
						finally {
							process.Dispose();
						}
					}, name: $"LZX Compact '{path}'");
				}
				catch {
					process.Dispose();
				}
			}
		}
		catch {
			return false;
		}
		try {
			if (info is not null) {
				info.Refresh();
				return info.Attributes.HasFlag(FileAttributes.Compressed);
			}
			return false;
		}
		catch {
			return false;
		}
	}
}