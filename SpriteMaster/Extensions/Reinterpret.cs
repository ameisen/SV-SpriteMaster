namespace SpriteMaster.Extensions;

internal static class Reinterpret {
	internal static unsafe U ReinterpretAs<T, U>(this T value) where T : unmanaged where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(T));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this bool value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(bool));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this byte value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(byte));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this sbyte value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(sbyte));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this ushort value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(ushort));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this short value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(short));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this uint value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(uint));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this int value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(int));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this ulong value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(ulong));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this long value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(long));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this half value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(half));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this float value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(float));
		return *(U*)&value;
	}

	internal static unsafe U ReinterpretAs<U>(this double value) where U : unmanaged {
		sizeof(U).AssertLessEqual(sizeof(double));
		return *(U*)&value;
	}
}
