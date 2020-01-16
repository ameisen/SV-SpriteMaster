using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using StardewValley;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	internal static class DrawState {
		private static readonly SamplerState DefaultSamplerState = SamplerState.LinearClamp;
		private static bool FetchedThisFrame = false;
		private static long RemainingTexelFetchBudget = Config.AsyncScaling.ScalingBudgetPerFrameTexels;
		private static bool PushedUpdateThisFrame = false;
		public static Volatile<ulong> CurrentFrame = 0;
		public static TextureAddressMode CurrentAddressModeU = DefaultSamplerState.AddressU;
		public static TextureAddressMode CurrentAddressModeV = DefaultSamplerState.AddressV;
		public static Blend CurrentBlendSourceMode = BlendState.AlphaBlend.AlphaSourceBlend;
		public static volatile bool TriggerGC = false;
		public static SpriteSortMode CurrentSortMode = SpriteSortMode.Deferred;
		public static TimeSpan FrameRate = new TimeSpan(166_667); // default 60hz
		public static bool ForceSynchronous = false;

		private static DateTime FrameStartTime = DateTime.Now;

		internal static GraphicsDevice Device {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				return Game1.graphics.GraphicsDevice;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void SetCurrentAddressMode (SamplerState samplerState) {
			CurrentAddressModeU = samplerState.AddressU;
			CurrentAddressModeV = samplerState.AddressV;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool GetUpdateToken (int texels) {
			if (FetchedThisFrame && texels > RemainingTexelFetchBudget) {
				return false;
			}

			FetchedThisFrame = true;
			RemainingTexelFetchBudget -= texels;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void OnPresent () {
			if (TriggerGC) {
				ScaledTexture.PurgeTextures((Config.RequiredFreeMemory * Config.RequiredFreeMemoryHysterisis).NearestLong() * 1024 * 1024);
				//Garbage.Collect();
				Garbage.Collect(compact: true, blocking: true, background: false);

				TriggerGC = false;
			}

			var duration = DateTime.Now - FrameStartTime;
			FrameStartTime = DateTime.Now;

			if (Config.AsyncScaling.CanFetchAndLoadSameFrame || !PushedUpdateThisFrame) {
				var remaining = FrameRate - duration;

				// Unfortunately, the game appears to call 'present' as late as possible (which also causes vblank to miss, bad!
				// So we have to lie about the remaining time.
				remaining = new TimeSpan(FrameRate.Ticks / 2);

				SynchronizedTasks.ProcessPendingActions(remaining);
			}

			RemainingTexelFetchBudget = Config.AsyncScaling.ScalingBudgetPerFrameTexels;
			FetchedThisFrame = false;
			PushedUpdateThisFrame = false;
			++CurrentFrame;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void OnBegin (
			SpriteBatch @this,
			SpriteSortMode sortMode,
			BlendState blendState,
			SamplerState samplerState,
			DepthStencilState depthStencilState,
			RasterizerState rasterizerState,
			Effect effect,
			Matrix transformMatrix
		) {
			CurrentSortMode = sortMode;
			SetCurrentAddressMode(samplerState ?? SamplerState.PointClamp);
			CurrentBlendSourceMode = (blendState ?? BlendState.AlphaBlend).AlphaSourceBlend;

			var device = @this.GraphicsDevice;
			var renderTargets = device.GetRenderTargets();
			var renderTarget = renderTargets.Any() ? renderTargets[0].RenderTarget : null;

			//if (renderTarget is RenderTarget2D target && target.RenderTargetUsage != RenderTargetUsage.DiscardContents) {
			//	Debug.WarningLn("Non-Discarding RTT");
			//}

			ForceSynchronous = renderTarget switch {
				null => false,
				_ => true
			};
		}
	}
}
