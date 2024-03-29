﻿namespace Benchmarks.Strings.Benchmarks.Sources;

public abstract class MultiStringSource<TSource0, TSource1> : StringSource
	where TSource0 : StringSource
	where TSource1 : StringSource {

	static MultiStringSource() {
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TSource0).TypeHandle);
		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TSource1).TypeHandle);
	}
}