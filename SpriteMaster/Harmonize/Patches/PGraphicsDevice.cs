using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions.Reflection;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
internal static class PGraphicsDevice {
	#region Present

	//[Harmonize("Present", fixation: Harmonize.Fixation.Postfix, priority: PriorityLevel.Last, critical: false)]
	//internal static void PresentPost(GraphicsDevice __instance, XRectangle? sourceRectangle, XRectangle? destinationRectangle, IntPtr overrideWindowHandle) => DrawState.OnPresentPost();

	[Harmonize("Present", fixation: Fixation.Prefix, priority: PriorityLevel.Last)]
	public static void PresentPre(GraphicsDevice __instance) {
		DrawState.OnPresent();
	}

	[Harmonize("Present", fixation: Fixation.Postfix, priority: PriorityLevel.Last)]
	public static void PresentPost(GraphicsDevice __instance) {
		DrawState.OnPresentPost();
	}

	#endregion

	[Harmonize(typeof(Microsoft.Xna.Framework.Game), "BeginDraw", fixation: Fixation.Prefix, priority: PriorityLevel.First)]
	public static void OnBeginDraw(Microsoft.Xna.Framework.Game __instance) {
		DrawState.OnBeginDraw();
	}

	#region Reset

	[Harmonize("Reset", fixation: Fixation.Postfix, priority: PriorityLevel.Last)]
	public static void OnResetPost(GraphicsDevice __instance) {
		DrawState.OnPresentPost();
	}

	#endregion

	[Harmonize("SetVertexAttributeArray", fixation: Fixation.Prefix, priority: PriorityLevel.Last)]
	public static bool OnSetVertexAttributeArray(GraphicsDevice __instance, bool[] attrs) {
		return !GL.GraphicsDeviceExt.SetVertexAttributeArray(
			__instance,
			attrs
		);
	}

	#region OnPlatformDrawUserIndexedPrimitives

	private static class VertexDeclarationClass<T> where T : struct {
		internal static readonly VertexDeclaration Value;

		static VertexDeclarationClass() {
			if (
				ReflectionExt.GetTypeExt("Microsoft.Xna.Framework.Graphics.VertexDeclarationCache")
					?.MakeGenericType(new[] {typeof(T)})
					?.GetProperty("VertexDeclaration", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
					?.GetValue(null) is VertexDeclaration declaration
			) {
				Value = declaration;
			}
			else {
				Value = VertexDeclaration.FromType(typeof(T));
			}
		}
	}

	[Harmonize(
		"DrawUserIndexedPrimitives",
		Fixation.Prefix,
		PriorityLevel.Last,
		generic: Generic.Struct
	)]
	public static unsafe bool OnDrawUserIndexedPrimitives<T>(
		GraphicsDevice __instance,
		PrimitiveType primitiveType,
		T[] vertexData,
		int vertexOffset,
		int numVertices,
		short[] indexData,
		int indexOffset,
		int primitiveCount,
		VertexDeclaration vertexDeclaration
	) where T : unmanaged {
		return !GL.GraphicsDeviceExt.DrawUserIndexedPrimitives(
			__instance,
			primitiveType,
			vertexData,
			vertexOffset,
			numVertices,
			indexData,
			indexOffset,
			primitiveCount,
			vertexDeclaration
		);
	}

	[Harmonize(
		"DrawUserIndexedPrimitives",
		Fixation.Prefix,
		PriorityLevel.Last,
		generic: Generic.Struct,
		genericConstraints: new[] { typeof(IVertexType) }
	)]
	public static unsafe bool OnDrawUserIndexedPrimitives<T>(
		GraphicsDevice __instance,
		PrimitiveType primitiveType,
		T[] vertexData,
		int vertexOffset,
		int numVertices,
		short[] indexData,
		int indexOffset,
		int primitiveCount
	) where T : unmanaged {
		return !GL.GraphicsDeviceExt.DrawUserIndexedPrimitives(
			__instance,
			primitiveType,
			vertexData,
			vertexOffset,
			numVertices,
			indexData,
			indexOffset,
			primitiveCount,
			VertexDeclarationClass<T>.Value
		);
	}

	[Harmonize(
		"DrawUserIndexedPrimitives",
		Fixation.Prefix,
		PriorityLevel.Last,
		generic: Generic.Struct
	)]
	public static unsafe bool OnDrawUserIndexedPrimitives<T>(
		GraphicsDevice __instance,
		PrimitiveType primitiveType,
		T[] vertexData,
		int vertexOffset,
		int numVertices,
		int[] indexData,
		int indexOffset,
		int primitiveCount,
		VertexDeclaration vertexDeclaration
	) where T : unmanaged {
		return !GL.GraphicsDeviceExt.DrawUserIndexedPrimitives(
			__instance,
			primitiveType,
			vertexData,
			vertexOffset,
			numVertices,
			indexData,
			indexOffset,
			primitiveCount,
			vertexDeclaration
		);
	}

	[Harmonize(
		"DrawUserIndexedPrimitives",
		Fixation.Prefix,
		PriorityLevel.Last,
		generic: Generic.Struct,
		genericConstraints: new[] { typeof(IVertexType) }
	)]
	public static unsafe bool OnDrawUserIndexedPrimitives<T>(
		GraphicsDevice __instance,
		PrimitiveType primitiveType,
		T[] vertexData,
		int vertexOffset,
		int numVertices,
		int[] indexData,
		int indexOffset,
		int primitiveCount
	) where T : unmanaged {
		return !GL.GraphicsDeviceExt.DrawUserIndexedPrimitives(
			__instance,
			primitiveType,
			vertexData,
			vertexOffset,
			numVertices,
			indexData,
			indexOffset,
			primitiveCount,
			VertexDeclarationClass<T>.Value
		);
	}

	#endregion
}
