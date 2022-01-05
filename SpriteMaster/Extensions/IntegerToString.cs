﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Extensions;

static partial class Integer {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string ToString16(this int value) => Convert.ToString(value, 16);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string ToString16(this uint value) => Convert.ToString(value, 16);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string ToString16(this long value) => Convert.ToString(value, 16);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string ToString16(this ulong value) => Convert.ToString((long)value, 16);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe string ToString64(this int value) => Convert.ToBase64String(new ReadOnlySpan<byte>(&value, sizeof(int)));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe string ToString64(this uint value) => Convert.ToBase64String(new ReadOnlySpan<byte>(&value, sizeof(uint)));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe string ToString64(this long value) => Convert.ToBase64String(new ReadOnlySpan<byte>(&value, sizeof(long)));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe string ToString64(this ulong value) => Convert.ToBase64String(new ReadOnlySpan<byte>(&value, sizeof(ulong)));
}