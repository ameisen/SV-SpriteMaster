//#define GL_DEBUG

using LinqFasterer;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.OpenGL;
using SpriteMaster.Extensions;
using SpriteMaster.Extensions.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

// Defined with a 32-bit depth
using GLEnum = System.UInt32;
using IntPtr = System.IntPtr;

namespace SpriteMaster.GL;

internal static unsafe class GLExt {
	// ReSharper disable UnusedMember.Global

	internal enum ObjectId : uint {
		None = 0
	};

	internal enum ErrorCode : GLEnum {
		NoError = 0x0000,
		InvalidEnum = 0x0500,
		InvalidValue = 0x0501,
		InvalidOperation = 0x0502,
		StackOverflow = 0x0503,
		StackUnderflow = 0x0504,
		OutOfMemory = 0x0505,
		InvalidFramebufferOperation = 0x0506,
		ContextLost = 0x0507,
		TableTooLarge = 0x8031
	}

	internal enum SizedInternalFormat : GLEnum {
		R8 = 0x8229,
		R8SNorm = 0x8F94,
		RG8 = 0x822B,
		RG8SNorm = 0x8F95,
		RGB8 = 0x8051,
		RGB8SNorm = 0x8F96,
		RGBA8 = 0x8058,
		RGBA8SNorm = 0x8F97,
		SRGB8 = 0x8C41,
		SRGB8A8 = 0x8C43
	}
	// ReSharper restore UnusedMember.Global

	[DebuggerHidden, DoesNotReturn]
	private static void HandleError(ErrorCode error, string expression) {
		using var errorList = ObjectPoolExt<List<string>>.Take(list => list.Clear());
		if (error is not ErrorCode.NoError) {
			errorList.Value.Add(error.ToString());
		}

		if (Interlocked.Exchange(ref LastCallbackException, null) is { } callbackException) {
			errorList.Value.Add(callbackException.Message);
		}

		while ((error = (ErrorCode)MonoGame.OpenGL.GL.GetError()) != ErrorCode.NoError) {
			errorList.Value.Add(error.ToString());
		}

		string errorMessage = $"GL.GetError() returned '{string.Join(", ", errorList.Value)}': {expression}";
		System.Diagnostics.Debug.WriteLine(errorMessage);
		Debug.Break();
		throw new MonoGameGLException(errorMessage);
	}

	[Conditional("GL_DEBUG"), Conditional("CONTRACTS_FULL"), Conditional("DEBUG")]
	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void CheckError(string? expression = null, [CallerMemberName] string member = "") {
		AlwaysCheckError(expression, member);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysCheckError(string? expression = null, [CallerMemberName] string member = "") {
		if (LastCallbackException is not null) {
			HandleError(ErrorCode.NoError, expression ?? member);
		}

		if ((ErrorCode)MonoGame.OpenGL.GL.GetError() is var error and not ErrorCode.NoError) {
			HandleError(error, expression ?? member);
		}
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SwallowOrReportErrors() {
#if GL_DEBUG || CONTRACTS_FULL || DEBUG
		CheckError();
#else
		AlwaysSwallowErrors();
#endif
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysSwallowErrors() {
		LastCallbackException = null;

		while ((ErrorCode)MonoGame.OpenGL.GL.GetError() != ErrorCode.NoError) {
			// Do Nothing
		}
	}

	[Conditional("GL_DEBUG"), Conditional("CONTRACTS_FULL"), Conditional("DEBUG")]
	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void SwallowErrors() {
		LastCallbackException = null;

		while ((ErrorCode)MonoGame.OpenGL.GL.GetError() != ErrorCode.NoError) {
			// Do Nothing
		}
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Checked(Action action, [CallerArgumentExpression("action")] string expression = "") {
		action();
		CheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Checked<T0>(Action<T0> action, T0 param0, [CallerArgumentExpression("action")] string expression = "") {
		action(param0);
		CheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Checked<T0, T1>(Action<T0, T1> action, T0 param0, T1 param1, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1);
		CheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Checked<T0, T1, T2>(Action<T0, T1, T2> action, T0 param0, T1 param1, T2 param2, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1, param2);
		CheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Checked<T0, T1, T2, T3>(Action<T0, T1, T2, T3> action, T0 param0, T1 param1, T2 param2, T3 param3, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1, param2, param3);
		CheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Checked<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> action, T0 param0, T1 param1, T2 param2, T3 param3, T4 param4, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1, param2, param3, param4);
		CheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysChecked(Action action, [CallerArgumentExpression("action")] string expression = "") {
		action();
		AlwaysCheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysChecked<T0>(Action<T0> action, T0 param0, [CallerArgumentExpression("action")] string expression = "") {
		action(param0);
		AlwaysCheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysChecked<T0, T1>(Action<T0, T1> action, T0 param0, T1 param1, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1);
		AlwaysCheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysChecked<T0, T1, T2>(Action<T0, T1, T2> action, T0 param0, T1 param1, T2 param2, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1, param2);
		AlwaysCheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysChecked<T0, T1, T2, T3>(Action<T0, T1, T2, T3> action, T0 param0, T1 param1, T2 param2, T3 param3, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1, param2, param3);
		AlwaysCheckError(expression);
	}

	[DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void AlwaysChecked<T0, T1, T2, T3, T4>(Action<T0, T1, T2, T3, T4> action, T0 param0, T1 param1, T2 param2, T3 param3, T4 param4, [CallerArgumentExpression("action")] string expression = "") {
		action(param0, param1, param2, param3, param4);
		AlwaysCheckError(expression);
	}

	internal readonly record struct FunctionFeature(string? Feature, string Name) {
		// ReSharper disable once InconsistentNaming
		internal FunctionFeature(string Name) : this(Feature: null, Name: Name) {
		}
	}

	// ReSharper disable MemberHidesStaticFromOuterClass
	internal static class Delegates {
		private static readonly MethodInfo? LoadFunctionGeneric =
			typeof(MonoGame.OpenGL.GL).GetStaticMethod("LoadFunction");

		internal static class Generic<T> where T : class? {

			private delegate T? LoadFunctionDelegate(string function, bool throwIfNotFound = false);

			private static readonly LoadFunctionDelegate LoadFunctionDelegator =
				LoadFunctionGeneric?.MakeGenericMethod(typeof(T))
					.CreateDelegate<LoadFunctionDelegate>() ??
					((_, _) => null);

			internal static T? LoadFunction(string function) {
				return LoadFunctionDelegator(function, false);
			}

			internal static T LoadFunctionRequired(string function) {
				try {
					return LoadFunctionDelegator(function, true)!;
				}
				catch (Exception ex) {
					throw new DelegateException(function, ex);
				}
			}
		}

		internal static nint? LoadActionPtr(string function) {
			if (Sdl.GL.GetProcAddress(function) is var address && address == IntPtr.Zero) {
				return null;
			}

			return address;
		}
		internal static nint? LoadActionPtr(in FunctionFeature function) {
			if (function.Feature is {} feature && !MonoGame.OpenGL.GL.Extensions.Contains(feature)) {
				return null;
			}

			if (Sdl.GL.GetProcAddress(function.Name) is var address && address == IntPtr.Zero) {
				return null;
			}

			return address;
		}

		internal static nint LoadActionPtrRequired(string function) {
			if (LoadActionPtr(function) is not {} address) {
				throw new DelegateException(function);
			}

			return address;
		}

		internal static nint LoadActionPtrRequired(in FunctionFeature function) {
			if (LoadActionPtr(in function) is not { } address) {
				throw new DelegateException(function.Name);
			}

			return address;
		}

		internal static nint LoadActionPtrRequired(params FunctionFeature[] functions) {
			foreach (var function in functions) {
				if (LoadActionPtr(function) is { } address) {
					return address;
				}
			}

			throw new DelegateException(functions.SelectF(function => function.Name).FirstOrDefaultF() ?? "<Unknown>");
		}

		internal static nint LoadActionPtrRequired(string feature, params string[] functions) {
			foreach (var function in functions) {
				if (LoadActionPtr(function) is { } address) {
					return address;
				}
			}

			throw new DelegateException(functions.FirstOrDefaultF() ?? "<Unknown>");
		}

		/*
		internal readonly struct ActionPtr<TArg0> {
			private readonly delegate* unmanaged[Stdcall]<TArg0, void> Pointer;

			internal ActionPtr(nint pointer) {
				Pointer = (delegate* unmanaged[Stdcall]<TArg0, void>)pointer;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal readonly void Invoke(TArg0 arg0) =>
				Pointer(arg0);
		}
		*/

		internal static delegate* unmanaged[Stdcall]<TArg0, void> LoadActionPtrRequired<TArg0>(string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, void>)LoadActionPtrRequired(function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, void> LoadActionPtrRequired<TArg0, TArg1>(string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, void>)LoadActionPtrRequired(function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void> LoadActionPtrRequired<TArg0, TArg1, TArg2>(string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void>)LoadActionPtrRequired(function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3>(string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void>)LoadActionPtrRequired(function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4>(string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void>)LoadActionPtrRequired(function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void>)LoadActionPtrRequired(function);

		internal static delegate* unmanaged[Stdcall]<TArg0, void> LoadActionPtrRequired<TArg0>(string feature, string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, void>)LoadActionPtrRequired(feature, function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, void> LoadActionPtrRequired<TArg0, TArg1>(string feature, string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, void>)LoadActionPtrRequired(feature, function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void> LoadActionPtrRequired<TArg0, TArg1, TArg2>(string feature, string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void>)LoadActionPtrRequired(feature, function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3>(string feature, string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void>)LoadActionPtrRequired(feature, function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4>(string feature, string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void>)LoadActionPtrRequired(feature, function);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(string feature, string function) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void>)LoadActionPtrRequired(feature, function);

		internal static delegate* unmanaged[Stdcall]<TArg0, void> LoadActionPtrRequired<TArg0>(params FunctionFeature[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, void>)LoadActionPtrRequired(functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, void> LoadActionPtrRequired<TArg0, TArg1>(params FunctionFeature[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, void>)LoadActionPtrRequired(functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void> LoadActionPtrRequired<TArg0, TArg1, TArg2>(params FunctionFeature[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void>)LoadActionPtrRequired(functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3>(params FunctionFeature[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void>)LoadActionPtrRequired(functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4>(params FunctionFeature[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void>)LoadActionPtrRequired(functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(params FunctionFeature[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void>)LoadActionPtrRequired(functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, void> LoadActionPtrRequired<TArg0>(string feature, params string[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, void>)LoadActionPtrRequired(feature, functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, void> LoadActionPtrRequired<TArg0, TArg1>(string feature, params string[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, void>)LoadActionPtrRequired(feature, functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void> LoadActionPtrRequired<TArg0, TArg1, TArg2>(string feature, params string[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, void>)LoadActionPtrRequired(feature, functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3>(string feature, params string[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, void>)LoadActionPtrRequired(feature, functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4>(string feature, params string[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, void>)LoadActionPtrRequired(feature, functions);

		internal static delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void> LoadActionPtrRequired<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5>(string feature, params string[] functions) =>
			(delegate* unmanaged[Stdcall]<TArg0, TArg1, TArg2, TArg3, TArg4, TArg5, void>)LoadActionPtrRequired(feature, functions);

#if false
		internal static class Sdl {
			internal static readonly Type? SdlType = ReflectionExt.GetTypeExt("Sdl");
			internal static readonly Type? SdlGlType = SdlType?.GetNestedType("GL");

			internal static class Generic<T> {
				internal delegate T? LoadFunctionDelegate(IntPtr library, string function, bool throwIfNotFound = false);

				internal static readonly LoadFunctionDelegate? LoadFunction =
					ReflectionExt.GetTypeExt("MonoGame.Framework.Utilities.FuncLoader")?.GetStaticMethod("LoadFunction")?.MakeGenericMethod(typeof(T))
						.CreateDelegate<LoadFunctionDelegate>();
			}

			internal static readonly nint NativeLibrary = (IntPtr?)SdlType?.GetStaticField("NativeLibrary")?.GetValue(null) ?? (nint)0;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			internal delegate nint GetProcAddressDelegate(string proc);

			internal static readonly GetProcAddressDelegate? GetProcAddress = Generic<GetProcAddressDelegate>.LoadFunction?.Invoke(NativeLibrary, "SDL_GL_GetProcAddress");
		}

		internal static nint LoadFunctionPtr(string function, bool throwIfNotFound = false) {
			var result = Sdl.GetProcAddress?.Invoke(function) ?? 0;
			if (result is 0) {
				return 0;
			}
			return result;
		}
#endif

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void CreateTextures(
			TextureTarget target,
			int n,
			[Out] ObjectId* textures
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void CreateTexture(
			TextureTarget target,
			int n,
			ref ObjectId texture
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void TexStorage2D(
			TextureTarget target,
			int levels,
			SizedInternalFormat internalFormat,
			int width,
			int height
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void TextureStorage2D(
			ObjectId target,
			int levels,
			SizedInternalFormat internalFormat,
			int width,
			int height
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void TextureStorage2DExt(
			ObjectId texture,
			TextureTarget target,
			int levels,
			SizedInternalFormat internalFormat,
			int width,
			int height
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void CopyImageSubData(
			ObjectId srcName,
			TextureTarget srcTarget,
			int srcLevel,
			int srcX,
			int srcY,
			int srcZ,
			ObjectId dstName,
			TextureTarget dstTarget,
			int dstLevel,
			int dstX,
			int dstY,
			int dstZ,
			uint srcWidth,
			uint srcHeight,
			uint srcDepth
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void GetInteger64Delegate(
			int param,
			[Out] long* data
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void GetTexImageDelegate(TextureTarget target, int level, PixelFormat format, PixelType type, [Out] nint pixels);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void GetTextureSubImageDelegate(
			ObjectId target,
			int level,
			int xOffset,
			int yOffset,
			int zOffset,
			uint width,
			uint height,
			uint depth,
			PixelFormat format,
			PixelType type,
			uint bufferSize,
			[Out] nint pixels
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void GetCompressedTexImageDelegate(TextureTarget target, int level, [Out] nint pixels);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void GetCompressedTextureSubImageDelegate(
			ObjectId target,
			int level,
			int xOffset,
			int yOffset,
			int zOffset,
			uint width,
			uint height,
			uint depth,
			uint bufferSize,
			[Out] nint pixels
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void DrawElements(
			GLPrimitiveType mode,
			uint count,
			ValueType type,
			nint indices
		);

		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		internal delegate void DrawRangeElements(
			GLPrimitiveType mode,
			uint start,
			uint end,
			uint count,
			ValueType type,
			nint indices
		);
	}
	// ReSharper restore MemberHidesStaticFromOuterClass

	private static bool DebuggingEnabled = false;

	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	private delegate void DebugMessageCallbackProc(int source, int type, int id, int severity, int length, nint message, nint userParam);

	private static readonly DebugMessageCallbackProc DebugProc = DebugMessageCallbackHandler;
	delegate void DebugMessageCallbackDelegate(DebugMessageCallbackProc callback, nint userParam);
	static readonly DebugMessageCallbackDelegate DebugMessageCallback =
		Delegates.Generic<DebugMessageCallbackDelegate>.LoadFunction("glDebugMessageCallback")!;

	private enum CallbackSeverity : int {
		High = 0x9146,
		Medium = 0x9147,
		Low = 0x9148,
		Notification = 0x826B
	}

	private static MonoGameGLException? LastCallbackException = null;
	[DebuggerHidden]
	private static void DebugMessageCallbackHandler(int source, int type, int id, int severityValue, int length, nint message, nint userParam) {
#if GL_DEBUG || DEBUG || CONTRACTS_FULL
		var severity = (CallbackSeverity)severityValue;

		if (severity == CallbackSeverity.Notification) {
			return;
		}

		switch (id) {
			case 131218: // "Program/shader state performance warning: Vertex shader in program 1 is being recompiled based on GL state."
			//case 131185: // "Buffer detailed info: Buffer object 1 (bound to GL_ELEMENT_ARRAY_BUFFER_ARB, usage hint is GL_STATIC_DRAW) will use VIDEO memory as the source for buffer object operations."
			return;
		}

		var errorMessage = Marshal.PtrToStringAnsi(message) ?? "unknown";
		var stackTrace = Environment.StackTrace;
		System.Diagnostics.Debug.WriteLine($"(severity: {severity}, type: {type}, id: {id}, source: {source}) : {errorMessage}");
		System.Diagnostics.Debug.WriteLine(stackTrace);

		try {
			throw new MonoGameGLException(errorMessage);
		}
		catch (MonoGameGLException ex) {
			Debug.Error(errorMessage, ex);
			LastCallbackException = ex;
		}

		Debug.Break();
#endif
	}

	[Conditional("GL_DEBUG"), Conditional("CONTRACTS_FULL"), Conditional("DEBUG")]
	internal static void EnableDebugging() {
		if (DebuggingEnabled) {
			return;
		}

		DebuggingEnabled = true;

		DebugMessageCallback(DebugProc, 0);
		MonoGame.OpenGL.GL.Enable(EnableCap.DebugOutput);
		MonoGame.OpenGL.GL.Enable(EnableCap.DebugOutputSynchronous);
	}

	internal interface IToggledDelegate {
		bool Enabled { get; }
		void Disable();
	}

	internal interface IToggledDelegate<TDelegate> : IToggledDelegate where TDelegate : Delegate {
		[MemberNotNullWhen(true, "Function")]
		new bool Enabled { get; }

		TDelegate? Function { get; }
	}

	[StructLayout(LayoutKind.Auto)]
	internal readonly struct ToggledDelegate<TDelegate> : IToggledDelegate<TDelegate> where TDelegate : Delegate {
		private readonly bool _enabled;
		internal readonly TDelegate? Function;

		[MemberNotNullWhen(true, "Function")]
		internal readonly bool Enabled => _enabled;

		readonly TDelegate? IToggledDelegate<TDelegate>.Function => Function;
		[MemberNotNullWhen(true, "Function")]
		readonly bool IToggledDelegate.Enabled => Enabled;
		[MemberNotNullWhen(true, "Function")]
		readonly bool IToggledDelegate<TDelegate>.Enabled => Enabled;

		private ToggledDelegate(TDelegate? function) {
			Function = function;
			_enabled = function is not null;
		}

		internal ToggledDelegate(string name) : this(Delegates.Generic<TDelegate>.LoadFunction(name)) {
		}

		internal ToggledDelegate(string feature, string name) : this(
			MonoGame.OpenGL.GL.Extensions.Contains(feature) ?
				Delegates.Generic<TDelegate>.LoadFunction(name) :
				null
			) {
		}

		internal ToggledDelegate(params FunctionFeature[] functions) {
			foreach (var function in functions) {
				if (function.Feature is { } feature && !MonoGame.OpenGL.GL.Extensions.Contains(feature)) {
					continue;
				}

				var functionDelegate = Delegates.Generic<TDelegate>.LoadFunction(function.Name);
				if (functionDelegate is null) {
					continue;
				}

				Function = functionDelegate;
				_enabled = true;
				return;
			}

			Function = null;
			_enabled = false;
		}

		internal ToggledDelegate(string feature, params string[] functions) {
			if (!MonoGame.OpenGL.GL.Extensions.Contains(feature)) {
				Function = null;
				_enabled = false;
				return;
			}

			foreach (var function in functions) {
				var functionDelegate = Delegates.Generic<TDelegate>.LoadFunction(function);
				if (functionDelegate is null) {
					continue;
				}

				Function = functionDelegate;
				_enabled = true;
				return;
			}

			Function = null;
			_enabled = false;
		}

		public readonly void Disable() => Unsafe.AsRef(in _enabled) = false;

//[MemberNotNullWhen(true, "Function")]
//public static implicit operator bool(ToggledDelegate<TDelegate> toggledDelegate) => toggledDelegate.Enabled;
	}

	internal abstract class ExtException : Exception {
		internal ExtException(string message) : base(message) {
		}

		internal ExtException(string message, Exception innerException) : base(message, innerException) {
		}
	}

	internal sealed class DelegateException : ExtException {
		private static string MakeMessage(string name) => $"Could not find or load function '{name}'";

		internal DelegateException(string name) : base(MakeMessage(name)) {
		}

		internal DelegateException(string name, Exception innerException) : base(MakeMessage(name), innerException) {
		}
	}

	public enum ValueType : int {
		Byte = 0x1400,
		UnsignedByte = 0x1401,
		Short = 0x1402,
		UnsignedShort = 0x1403,
		Int = 0x1404,
		UnsignedInt = 0x1405,

		Float = 0x1406,
		HalfFloat = 0x140B,
		Double = 0x140A,
		Fixed = 0x140C,
		IntRev_2_10_10_10 = 0x8D9F,
		UIntRev_2_10_10_10 = 0x8368,
		UIntRev_10f_11f_11f = 0x8C3B,
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsLegalIndexType(this ValueType type) =>
		type is ValueType.UnsignedByte or ValueType.UnsignedShort or ValueType.UnsignedInt;

#if false
	internal static readonly delegate* unmanaged<TextureTarget, int, PixelInternalFormat, int, int, void> TexStorage2DPtr =
		(delegate* unmanaged<TextureTarget, int, PixelInternalFormat, int, int, void>)(void*)Delegates.LoadFunctionPtr("glTexStorage2D");
#endif
	internal static readonly ToggledDelegate<Delegates.CreateTextures> CreateTextures = new(
		new FunctionFeature("GL_EXT_direct_state_access", "glCreateTexturesEXT"),
		new FunctionFeature("GL_ARB_direct_state_access", "glCreateTexturesARB"),
		new FunctionFeature("GL_ARB_direct_state_access", "glCreateTextures")
	);
	internal static readonly ToggledDelegate<Delegates.TexStorage2D> TexStorage2D = new(
		"GL_EXT_texture_storage",
		"glTexStorage2DEXT",
		"glTexStorage2D"
	);
	internal static readonly ToggledDelegate<Delegates.TextureStorage2D> TextureStorage2D = new(
		"GL_EXT_texture_storage",
		"glTextureStorage2D"
	);
	internal static readonly ToggledDelegate<Delegates.TextureStorage2DExt> TextureStorage2DExt = new(
		"GL_EXT_texture_storage",
		"glTextureStorage2DEXT"
	);

	internal static readonly ToggledDelegate<Delegates.CopyImageSubData> CopyImageSubData = new(
		"GL_ARB_copy_image",
		"glCopyImageSubDataEXT",
		"glCopyImageSubData"
	);

	internal static readonly ToggledDelegate<Delegates.GetInteger64Delegate> GetInteger64v =
		new("glGetInteger64v");

	internal static readonly ToggledDelegate<Delegates.GetTexImageDelegate> GetTexImage =
		new("glGetTexImage");

	internal static readonly ToggledDelegate<Delegates.GetTextureSubImageDelegate> GetTextureSubImage =
		new("GL_ARB_get_texture_sub_image", "glGetTextureSubImage");
	internal static readonly ToggledDelegate<Delegates.GetCompressedTexImageDelegate> GetCompressedTexImage =
		new("glGetCompressedTexImage");
	internal static readonly ToggledDelegate<Delegates.GetCompressedTextureSubImageDelegate> GetCompressedTextureSubImage =
		new("GL_ARB_get_texture_sub_image", "glGetCompressedTextureSubImage");

	internal static readonly delegate* unmanaged[Stdcall]<GLPrimitiveType, uint, ValueType, nint, void> DrawElements =
		Delegates.LoadActionPtrRequired<GLPrimitiveType, uint, ValueType, nint>("glDrawElements");

	internal static readonly delegate* unmanaged[Stdcall]<GLPrimitiveType, uint, uint, uint, ValueType, nint, void> DrawRangeElements =
		Delegates.LoadActionPtrRequired<GLPrimitiveType, uint, uint, uint, ValueType, nint>(
			"GL_EXT_draw_range_elements",
			"glDrawRangeElements",
			"glDrawRangeElementsEXT"
		);

	internal static readonly delegate* unmanaged[Stdcall]<uint, int, ValueType, bool, uint, nint, void> VertexAttribPointer =
		Delegates.LoadActionPtrRequired<uint, int, ValueType, bool, uint, nint>("glVertexAttribPointer");

	internal static readonly delegate* unmanaged[Stdcall]<BufferTarget, GLExt.ObjectId, void> BindBuffer =
		Delegates.LoadActionPtrRequired<BufferTarget, GLExt.ObjectId>("glBindBuffer");

	internal static readonly delegate* unmanaged[Stdcall]<BufferTarget, nint, nint, BufferUsageHint, void> BufferData =
		Delegates.LoadActionPtrRequired<BufferTarget, nint, nint, BufferUsageHint>("glBufferData");

	internal static readonly delegate* unmanaged[Stdcall]<uint, uint, void> VertexAttribDivisor =
		Delegates.LoadActionPtrRequired<uint, uint>(
			"GL_ARB_instanced_arrays",
			"glVertexAttribDivisorEXT",
			"glVertexAttribDivisorARB",
			"glVertexAttribDivisor"
		);

	internal static readonly delegate* unmanaged[Stdcall]<TextureTarget, TextureParameterName, nint, void> GetTexParameteriv =
		Delegates.LoadActionPtrRequired<TextureTarget, TextureParameterName, nint>("glGetTexParameteriv");

	internal static void Dump() {
		var dumpBuilder = new StringBuilder();

		dumpBuilder.AppendLine("GLExt Dump:");

		var fields = typeof(GLExt)
			.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		List<(string Name, bool Enabled)> fieldValues = new();

		foreach (var field in fields) {
			var fieldValue = field.GetValue(null);
			bool enabled;

			switch (fieldValue)
			{
				case IToggledDelegate toggledDelegate:
					enabled = toggledDelegate.Enabled;
					break;
				case nint pointer:
					enabled = pointer != 0;
					break;
				default:
					continue;
			}

			fieldValues.Add((field.Name, enabled));
		}

		int maxName = fieldValues.MaxF(pair => pair.Name.Length);

		foreach (var (name, enabled) in fieldValues) {
			dumpBuilder.AppendLine($"  {name.PadRight(maxName)}: {enabled}");
		}

		Debug.Message(dumpBuilder.ToString());
	}
}