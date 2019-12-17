using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;
using System.Data.HashFunction.xxHash;
using System.Drawing;
using System.IO;

namespace SpriteMaster.Extensions {
	using XRectangle = Microsoft.Xna.Framework.Rectangle;
	internal static class Hashing {
		// FNV-1a hash.
		internal static ulong HashFNV1 (this byte[] data) {
			const ulong prime = 0x100000001b3;
			ulong hash = 0xcbf29ce484222325;
			foreach (byte octet in data) {
				hash ^= octet;
				hash *= prime;
			}

			return hash;
		}

		/*
		internal static unsafe ulong HashFNV1<T>(this T[] data) where T : unmanaged
		{
			using (var handle = data.AsMemory().Pin())
			{
				var spannedData = new Span<byte>(handle.Pointer, data.Length * sizeof(T));
				return spannedData.ToArray().HashFNV1();
			}
		}
		*/

		private static xxHashConfig GetHashConfig () {
			var config = new xxHashConfig();
			config.HashSizeInBits = 64;
			return config;
		}
		private static readonly IxxHash HasherXX = xxHashFactory.Instance.Create(GetHashConfig());

		internal static ulong HashXX (this byte[] data) {
			var hashData = HasherXX.ComputeHash(data).Hash;
			return BitConverter.ToUInt64(hashData, 0);
		}

		internal static ulong HashXX (this byte[] data, int start, int length) {
			var hashData = HasherXX.ComputeHash(new MemoryStream(data, start, length)).Hash;
			return BitConverter.ToUInt64(hashData, 0);
		}

		/*
		internal static unsafe ulong HashXX<T>(this T[] data) where T : unmanaged
		{
			using (var handle = data.AsMemory().Pin())
			{
				var spannedData = new Span<byte>(handle.Pointer, data.Length * sizeof(T));
				return spannedData.ToArray().HashXX();
			}
		}
		*/

		internal static ulong Hash (this byte[] data) {
			return data.HashXX();
			//return data.HashFNV1();
		}

		internal static ulong Hash (this byte[] data, int start, int length) {
			return data.HashXX(start, length);
			//return data.HashFNV1();
		}

		/*
		internal static unsafe ulong Hash<T>(this T[] data) where T : unmanaged
		{
			return data.HashXX();
		}
		*/

		internal static ulong Hash (this Texture2D texture) {
			// TODO : make sure that the texture's stride is actually 4B * width
			byte[] data = new byte[texture.Width * texture.Height * sizeof(int)];
			texture.GetData(data);
			return data.Hash();
		}

		internal static ulong Hash (this in Rectangle rectangle) {
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static ulong Hash (this in XRectangle rectangle) {
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static ulong Hash (this in Bounds rectangle) {
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}
	}
}
