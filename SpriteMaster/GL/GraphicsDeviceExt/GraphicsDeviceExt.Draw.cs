using Microsoft.Toolkit.HighPerformance;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.OpenGL;
using SpriteMaster.Extensions;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Microsoft.Xna.Framework.Graphics.VertexDeclaration.VertexDeclarationAttributeInfo;

namespace SpriteMaster.GL;

internal static partial class GraphicsDeviceExt {
	static GraphicsDeviceExt() {
		SMConfig.ConfigChanged += OnConfigChanged;
		OnConfigChanged();
	}

	private static class Enabled {
		internal static bool DrawUserIndexedPrimitivesInternal = true;
		internal static bool DrawUserIndexedPrimitives = DrawUserIndexedPrimitivesInternal;
	}

	internal static unsafe class SpriteBatcherValues {
		internal const int MaxBatchSize = SpriteBatcher.MaxBatchSize;
		internal const int MaxIndicesCount = MaxBatchSize * 6;
		internal static readonly short[] Indices16 = GC.AllocateUninitializedArray<short>(MaxIndicesCount);

#if false
		internal static readonly Lazy<GLExt.ObjectId> IndexBuffer16 = new(() => GetIndexBuffer(Indices16), mode: LazyThreadSafetyMode.None);

		// Creates an OpenGL index buffer object given the provided data.
		private static GLExt.ObjectId GetIndexBuffer<T>(T[] data) where T : unmanaged {
			try {
				MonoGame.OpenGL.GL.GenBuffers(1, out int indexBuffer);
				GraphicsExtensions.CheckGLError();
				MonoGame.OpenGL.GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
				GraphicsExtensions.CheckGLError();
				fixed (T* ptr = data) {
					MonoGame.OpenGL.GL.BufferData(
						BufferTarget.ElementArrayBuffer,
						(nint)data.Length * sizeof(T),
						(nint)ptr,
						BufferUsageHint.StaticDraw
					);
				}

				GraphicsExtensions.CheckGLError();

				return (GLExt.ObjectId)indexBuffer;
			}
			catch (Exception) {
				return GLExt.ObjectId.None;
			}
		}
#endif

		// Returns the greatest array index possible given the number of elements.
		// We know that the 4th offset of each element (which is two triangles, 6 indices total) is the greatest,
		// so we can rapidly calculate a value based upon that assumption.
		internal static uint GetMaxArrayIndex(uint numElements, uint offset) {
			if (numElements == 0u) {
				return 0u;
			}

			if (offset != 0) {
				numElements += offset / 6;
				offset %= 6;
				if (offset > 4) {
					++numElements;
				}
			}

			return ((numElements - 1u) * 4u) + 3u;
		}

		static SpriteBatcherValues() {
			fixed (short* ptr16 = Indices16) {
				for (uint i = 0, indexOffset = 0; i < MaxBatchSize; ++i, indexOffset += 6u) {
					uint index0 = i * 4u;
					uint index1 = index0 + 1u;
					uint index2 = index0 + 2u;
					uint index3 = index0 + 1u;
					uint index4 = index0 + 3u;
					uint index5 = index0 + 2u;

					ptr16[indexOffset + 0u] = (short)(ushort)index0;
					ptr16[indexOffset + 1u] = (short)(ushort)index1;
					ptr16[indexOffset + 2u] = (short)(ushort)index2;
					ptr16[indexOffset + 3u] = (short)(ushort)index3;
					ptr16[indexOffset + 4u] = (short)(ushort)index4;
					ptr16[indexOffset + 5u] = (short)(ushort)index5;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static GLPrimitiveType GetGl(this PrimitiveType primitiveType) =>
		primitiveType switch {
			PrimitiveType.TriangleList => GLPrimitiveType.Triangles,
			PrimitiveType.TriangleStrip => GLPrimitiveType.TriangleStrip,
			PrimitiveType.LineList => GLPrimitiveType.Lines,
			PrimitiveType.LineStrip => GLPrimitiveType.LineStrip,
			_ => ThrowHelper.ThrowArgumentException<GLPrimitiveType>(nameof(primitiveType))
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint GetElementCountArray(this PrimitiveType primitiveType, int primitiveCount) =>
		primitiveType switch {
			PrimitiveType.TriangleList => (uint)primitiveCount * 3u,
			PrimitiveType.TriangleStrip => (uint)primitiveCount + 2u,
			PrimitiveType.LineList => (uint)primitiveCount * 2u,
			PrimitiveType.LineStrip => (uint)primitiveCount + 1,
			_ => ThrowHelper.ThrowNotSupportedException<uint>(primitiveType.ToString())
		};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static GLExt.ValueType GetExpensiveIndexType(GLExt.ValueType value) {
#if SHIPPING
		Debug.WarningOnce($"Expensive Index Type queried: {value}");
#endif
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe GLExt.ValueType GetIndexType<TIndex>() where TIndex : unmanaged =>
		sizeof(TIndex) switch {
			1 => GetExpensiveIndexType(GLExt.ValueType.UnsignedByte),
			2 => GLExt.ValueType.UnsignedShort,
			4 => GLExt.ValueType.UnsignedInt,
			_ => ThrowHelper.ThrowArgumentException<GLExt.ValueType>(nameof(TIndex))
		};

	private static uint EnabledAttributeBitmask = GetEnabledAttributeBitmask();

	// Gets a bitmask from the current MonoGame enabledVertexAttributes list.
	private static uint GetEnabledAttributeBitmask() {
		uint bitmask = 0u;
		foreach (var attribute in GraphicsDevice._enabledVertexAttributes) {
			bitmask |= 1u << attribute;
		}

		return bitmask;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool SetVertexAttributeArray(GraphicsDevice @this, bool[] attributes) {
		
		// Iterate over each attribute, setting the bitmask's bits appropriately for each offset in the array.
		uint attributeBit = 1u;
		uint bitmask = 0U;
		foreach (var flagValue in attributes) {
			int flag = flagValue.ReinterpretAs<byte>();

			bitmask |= (uint)-flag & attributeBit;

			attributeBit <<= 1;
		}

		// If the bitmask is different, then attribute enablement has changed - take the slow path.
		// This is incredibly rare, surprisingly.
		if (bitmask != EnabledAttributeBitmask) {
			return SetVertexAttributeArraySlowPath(attributes);
		}

		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool SetVertexAttributeBitmask(GraphicsDevice @this, uint bitmask, bool[] attributes) {
		// If the bitmask is different, then attribute enablement has changed - take the slow path.
		// This is incredibly rare, surprisingly.
		if (bitmask != EnabledAttributeBitmask) {
			return SetVertexAttributeArraySlowPath(attributes);
		}

		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool SetVertexAttributeArraySlowPath(bool[] attributes) {
		uint attributeBit = 1u;
		uint bitmask = EnabledAttributeBitmask;

		// Iterate over each attribute, compare against the current bitmask, and enable/disable the Vertex Attribute Array as appropriate.
		for (int attribute = 0; attribute < attributes.Length; ++attribute, attributeBit <<= 1) {
			bool flagValue = attributes[attribute];
			int flag = flagValue.ReinterpretAs<byte>();

			uint currentValue = bitmask & attributeBit;
			if (flagValue) {
				if (currentValue == 0u) {
					bitmask |= attributeBit;
					MonoGame.OpenGL.GL.EnableVertexAttribArray(attribute);
				}
			}
			else {
				if (currentValue != 0u) {
					bitmask &= ~attributeBit;
					MonoGame.OpenGL.GL.DisableVertexAttribArray(attribute);
				}
			}

			bitmask = (bitmask & ~attributeBit) | ((uint)-flag & attributeBit);
		}

		EnabledAttributeBitmask = bitmask;

		// Update the MonoGame enabledVertexAttributes list just in-case something needs it.
		var list = GraphicsDevice._enabledVertexAttributes;
		list.Clear();

		// Iterates over the bitmask and adds each offset/index into the list.
		int index = 0;
		while (BitOperations.TrailingZeroCount(bitmask) is var shift && shift < 32) {
			index += shift;
			bitmask >>= shift + 1;
			list.Add(index);
			++index;
		}

		return true;
	}

	private static int LastProgramHash = int.MinValue;
	private static VertexDeclaration.VertexDeclarationAttributeInfo? LastAttributeInfo = null;

	// Their implementation of this already caches using a `Dictionary`, but we can avoid even the overhead of the dictionary lookup
	// in 99% of cases by just using a simple inline cache like this. This hits almost every time.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static VertexDeclaration.VertexDeclarationAttributeInfo GetAttributeInfo(VertexDeclaration @this, Shader shader, int programHash) {
		if (LastProgramHash != programHash || LastAttributeInfo is not {} attributeInfo) {
			LastAttributeInfo = attributeInfo = @this.GetAttributeInfo(shader, programHash);
			LastProgramHash = programHash;
		}

		return attributeInfo;
	}

	private static readonly bool SupportsInstancing = DrawState.Device.GraphicsCapabilities.SupportsInstancing;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void VertexDeclarationApply(VertexDeclaration @this, Shader shader, nint offset, int programHash) {
		VertexDeclaration.VertexDeclarationAttributeInfo attributeInfo = GetAttributeInfo(@this, shader, programHash);

		uint vertexStride = (uint)@this.VertexStride;
		uint bitmask = 0u;

		// It's faster to iterate over a list-span than a list itself. Significantly so (about 5x).
		foreach (var element in attributeInfo.Elements.AsSpan()) {
			// Call our function pointer version - this is called a lot, so we want reduced overhead.
			VertexAttribPointerInternal(
				(uint)element.AttributeLocation,
				element.NumberOfElements,
				(GLExt.ValueType)element.VertexAttribPointerType,
				element.Normalized,
				vertexStride,
				offset + element.Offset
			);

			bitmask |= 1u << element.AttributeLocation;

			// This allows us to avoid a more complex boolean check every iteration of the loop
			if (SupportsInstancing) {
				MonoGame.OpenGL.GL.VertexAttribDivisor(element.AttributeLocation, 0);
			}
		}
		SetVertexAttributeBitmask(@this.GraphicsDevice, bitmask, attributeInfo.EnabledAttributes);
		GraphicsDevice._attribsDirty = true;
	}

	#region BindBufferOverride

	private static readonly Dictionary<BufferTarget, GLExt.ObjectId> OtherBufferBindings = new();
	private static readonly GLExt.ObjectId[] PrimaryBufferBindingsArray = GC.AllocateArray<GLExt.ObjectId>(2, pinned: true);
	private static readonly unsafe GLExt.ObjectId* PrimaryBufferBindings = PrimaryBufferBindingsArray.GetPointerFromPinned();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void BindBufferOverride(BufferTarget target, int obj) =>
		BindBufferInternal(target, (GLExt.ObjectId)obj);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void BindBufferInternal(BufferTarget target, GLExt.ObjectId obj) {
		int offset = (int)target - (int)BufferTarget.ArrayBuffer;

		if (offset <= 1) {
			if (PrimaryBufferBindings[offset] != obj) {
				PrimaryBufferBindings[offset] = obj;
				GLExt.BindBuffer(target, obj);
			}
		}
		else {
			BindBufferInternalSlowPath(target, obj);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static unsafe void BindBufferInternalSlowPath(BufferTarget target, GLExt.ObjectId obj) {
		if (!OtherBufferBindings.TryGetValue(target, out var boundObj) || boundObj != obj) {
			OtherBufferBindings[target] = obj;
			GLExt.BindBuffer(target, obj);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe bool BindBufferInternalResult(BufferTarget target, GLExt.ObjectId obj) {
		int offset = (int)target - (int)BufferTarget.ArrayBuffer;

		if (PrimaryBufferBindings[offset] != obj) {
			PrimaryBufferBindings[offset] = obj;
			GLExt.BindBuffer(target, obj);
			return true;
		}

		return false;
	}

	#endregion

	#region VertexAttributeDivisor

	private static readonly Lazy<int> MaxVertexAttributes = new(
		() => {
			MonoGame.OpenGL.GL.GetInteger((int)MonoGame.OpenGL.GetPName.MaxVertexAttribs, out int value);
			return value;
		}
	);

	private static readonly uint[] VertexAttribDivisorsArray = GC.AllocateArray<uint>(MaxVertexAttributes.Value, pinned: true);
	private static readonly unsafe uint* VertexAttribDivisors = VertexAttribDivisorsArray.GetPointerFromPinned();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void VertexAttribDivisorOverride(int index, int divisor) {
		uint uDivisor = (uint)divisor;
		
		if (VertexAttribDivisors[index] != uDivisor) {
			VertexAttribDivisors[index] = uDivisor;

			GLExt.VertexAttribDivisor((uint)index, uDivisor);
		}
	}

	#endregion

	#region VertexAttributePointer

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void VertexAttribPointerOverride(
		int location,
		int elementCount,
		VertexAttribPointerType type,
		bool normalize,
		int stride,
		IntPtr data
	) {
		VertexAttribPointerInternal(
			(uint)location,
			elementCount,
			(GLExt.ValueType)type,
			normalize,
			(uint)stride,
			data
		);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private readonly record struct AttributePointer(
		nint Data,							// 8
		int ElementCount,				// 12
		GLExt.ValueType Type,		// 16
		uint Stride,						// 20
		bool Normalize					// 21 (24)
	) {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe bool FastEquals(in AttributePointer other) {
			ulong* thisPtr = (ulong*)Unsafe.AsPointer(ref Unsafe.AsRef(this));
			ulong* otherPtr = (ulong*)Unsafe.AsPointer(ref Unsafe.AsRef(other));

			return
				thisPtr[0] == otherPtr[0] &&
				thisPtr[1] == otherPtr[1] &&
				thisPtr[2] == otherPtr[2];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal unsafe bool FastEqualsBr(in AttributePointer other) {
			ulong* thisPtr = (ulong*)Unsafe.AsPointer(ref Unsafe.AsRef(this));
			ulong* otherPtr = (ulong*)Unsafe.AsPointer(ref Unsafe.AsRef(other));

			return
				(thisPtr[0] == otherPtr[0]) &
				(thisPtr[1] == otherPtr[1]) &
				(thisPtr[2] == otherPtr[2]);
		}
	};

	private static readonly AttributePointer[] VertexAttributePointersArray = GC.AllocateArray<AttributePointer>(MaxVertexAttributes.Value, pinned: true);
	private static readonly unsafe AttributePointer* VertexAttributePointers = VertexAttributePointersArray.GetPointerFromPinned();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void VertexAttribPointerInternal(
		uint location,
		int elementCount,
		GLExt.ValueType type,
		bool normalize,
		uint stride,
		nint data
	) {
		var currentAttributePointer = new AttributePointer(data, elementCount, type, stride, normalize);
		var attributePointer = VertexAttributePointers + location;
		if (currentAttributePointer.FastEquals(*attributePointer)) {
			return;
		}

		*attributePointer = currentAttributePointer;

		GLExt.VertexAttribPointer(
			location,
			elementCount,
			type,
			normalize,
			stride,
			data
		);
	}

	#endregion

	internal static unsafe bool DrawUserIndexedPrimitives<TVertex, TIndex>(
		GraphicsDevice @this,
		PrimitiveType primitiveType,
		TVertex[] vertexData,
		int vertexOffset,
		int numVertices,
		TIndex[] indexData,
		int indexOffset,
		int primitiveCount,
		VertexDeclaration vertexDeclaration
	) where TVertex : unmanaged where TIndex : unmanaged {
		++DrawState.Statistics.DrawCalls;

		if (!Enabled.DrawUserIndexedPrimitives) {
			return false;
		}

		try {
			// TODO : This can be optimized as well - lots of interdependant booleans.
			@this.ApplyState(true);

			// Unbind current VBOs.
			BindBufferInternal(BufferTarget.ArrayBuffer, GLExt.ObjectId.None);
			GraphicsExtensions.CheckGLError();
			if (BindBufferInternalResult(BufferTarget.ElementArrayBuffer, GLExt.ObjectId.None)) {
				@this._indexBufferDirty = true;
			}
			GraphicsExtensions.CheckGLError();

			var type = primitiveType.GetGl();
			var count = primitiveType.GetElementCountArray(primitiveCount);

			// Perform as much work outside of 'fixed' as possible so as to limit the time the GC might have to stall if it is triggered.
			nint vertexPointerOffset = vertexDeclaration.VertexStride * vertexOffset;

			// Pin the buffers.
			fixed (TVertex* vbPtr = vertexData) {
				nint vertexPointer = (nint)vbPtr;
				vertexPointer += vertexPointerOffset;

				// Setup the vertex declaration to point at the VB data.
				vertexDeclaration.GraphicsDevice = @this;
				// Use our optimized version.
				VertexDeclarationApply(
					vertexDeclaration,
					shader: @this._vertexShader,
					offset: vertexPointer,
					programHash: @this.ShaderProgramHash
				);

				fixed (TIndex* ibPtr = indexData) {
					var offsetIndexPtr = (nint)(ibPtr + indexOffset);

					//Draw

					// If we are drawing from the pre-cached spritebatcher indices, we can use `glDrawRangeElements` instead.
					if (sizeof(TIndex) == 2 && ReferenceEquals(indexData, SpriteBatcherValues.Indices16)) {
						GLExt.DrawRangeElements(
							GLPrimitiveType.Triangles,
							0,
							SpriteBatcherValues.GetMaxArrayIndex((uint)primitiveCount, (uint)indexOffset),
							count,
							GLExt.ValueType.UnsignedShort,
							offsetIndexPtr
						);
					}
					else {
						GLExt.DrawElements(
							type,
							count,
							GetIndexType<TIndex>(),
							offsetIndexPtr
						);
					}
					GraphicsExtensions.CheckGLError();
				}
			}
		}
		catch (Exception ex) when (ex is MemberAccessException or MonoGameGLException) {
			Debug.Error($"Disabling OpenGL Optimization due to exception", ex);
			Enabled.DrawUserIndexedPrimitivesInternal = false;
			return false;
		}

		return true;
	}

	private static readonly MonoGame.OpenGL.GL.BindBufferDelegate OriginalBindBuffer =
		MonoGame.OpenGL.GL.BindBuffer;

	private static readonly MonoGame.OpenGL.GL.VertexAttribDivisorDelegate OriginalVertexAttribDivisor =
		MonoGame.OpenGL.GL.VertexAttribDivisor;

	private static readonly MonoGame.OpenGL.GL.VertexAttribPointerDelegate OriginalVertexAttribPointer =
		MonoGame.OpenGL.GL.VertexAttribPointer;

	// When the config changes, update the enablement booleans.
	private static void OnConfigChanged() {
		Enabled.DrawUserIndexedPrimitives =
			Enabled.DrawUserIndexedPrimitivesInternal &&
			SMConfig.Extras.OpenGL.Enabled &&
			SMConfig.Extras.OpenGL.OptimizeDrawUserIndexedPrimitives;

		MonoGame.OpenGL.GL.BindBuffer = BindBufferOverride;
		MonoGame.OpenGL.GL.VertexAttribDivisor = VertexAttribDivisorOverride;
		MonoGame.OpenGL.GL.VertexAttribPointer = VertexAttribPointerOverride;
	}
}
