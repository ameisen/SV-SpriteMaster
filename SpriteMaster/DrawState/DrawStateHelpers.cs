using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Reflection;

namespace SpriteMaster;

internal static partial class DrawState {

	private static readonly Func<SamplerState, SamplerState> SamplerStateClone =
		typeof(SamplerState).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic)?.CreateDelegate<Func<SamplerState, SamplerState>>() ?? throw new NullReferenceException(nameof(SamplerStateClone));

#if DEBUG
	private static string Dump(this BlendState blendState) {
		// TODO : use delegates instead of reflection here
		try {
			var sb = new StringBuilder();

			var properties = typeof(BlendState).GetProperties();
			int maxNameLen = int.MinValue;
			foreach (var property in properties) {
				if (property.Name.Length > maxNameLen) {
					maxNameLen = property.Name.Length;
				}
			}

			++maxNameLen;

			foreach (var property in properties) {
				try {
					var propertyValue = property.GetValue(blendState);
					sb.AppendLine($"{property.Name.PadLeft(maxNameLen)}: {(propertyValue is null ? "[null]" : propertyValue.ToString())}");
				}
				catch { }
			}

			return sb.ToString();
		}
		catch (Exception ex) {
			Debug.Warning(ex);
			return blendState.ToString();
		}
	}
#endif

	[Conditional("DEBUG")]
	private static void CheckStates() {
		/*
#if DEBUG
		// Warn if we see some blend and sampler states that we don't presently handle
		if (CurrentSamplerState.AddressU is (TextureAddressMode.Border or TextureAddressMode.Mirror or TextureAddressMode.Wrap) && AlreadyPrintedSetSampler.Add(CurrentSamplerState)) {
			Debug.Trace($"SamplerState.AddressU: Unhandled Sampler State: {CurrentSamplerState.AddressU}");
		}
		if (CurrentSamplerState.AddressV is (TextureAddressMode.Border or TextureAddressMode.Mirror or TextureAddressMode.Wrap) && AlreadyPrintedSetSampler.Add(CurrentSamplerState)) {
			Debug.Trace($"SamplerState.AddressV: Unhandled Sampler State: {CurrentSamplerState.AddressV}");
		}
		if (CurrentBlendState != BlendState.AlphaBlend && AlreadyPrintedSetBlend.Add(CurrentBlendState)) {
			Debug.Trace($"BlendState: Unhandled Blend State: {CurrentBlendState.Dump()}");
		}
#endif
		*/
	}
}
