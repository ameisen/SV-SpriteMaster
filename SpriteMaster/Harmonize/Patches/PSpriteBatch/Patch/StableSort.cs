using HarmonyLib;
using SpriteMaster.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Harmonize.Patches.PSpriteBatch.Patch;
static class StableSort {
	private static readonly Type? SpriteBatchItemType = typeof(XNA.Graphics.SpriteBatch).Assembly.GetType("Microsoft.Xna.Framework.Graphics.SpriteBatchItem");
	private static readonly Func<object?, float>? GetSortKeyImpl = SpriteBatchItemType?.GetFieldGetter<object?, float>("SortKey");

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static float GetSortKey(object? obj) => obj is null ? float.MinValue : GetSortKeyImpl!(obj);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static void Swap<T>(ref T a, ref T b) => (a, b) = (b, a);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static int Partition<T>(Span<T> data, Span<int> indices, int low, int high) where T : IComparable<T> {
		var pivot = data[high];
		var pivotIndex = indices[high];

		var i = low - 1;

		for (int j = low; j <= high - 1; ++j) {
			var jIndex = indices[j];
			var compareResult = data[j].CompareTo(pivot);
			if (compareResult == 0) {
				compareResult = jIndex - pivotIndex;
			}
			if (compareResult < 0) {
				++i;
				Swap(ref data[i], ref data[j]);
				Swap(ref indices[i], ref indices[j]);
			}
		}
		Swap(ref data[i + 1], ref data[high]);
		Swap(ref indices[i + 1], ref indices[high]);
		return i + 1;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static int PartitionByKey<T>(Span<T> data, Span<int> indices, int low, int high) where T : IComparable<T> {
		var pivot = data[high];
		var pivotKey = GetSortKey(pivot);
		var pivotIndex = indices[high];

		var i = low - 1;

		for (int j = low; j <= high - 1; ++j) {
			var jIndex = indices[j];
			var compareResult = GetSortKey(data[j]).CompareTo(pivotKey);
			if (compareResult == 0) {
				compareResult = jIndex - pivotIndex;
			}
			if (compareResult < 0) {
				++i;
				Swap(ref data[i], ref data[j]);
				Swap(ref indices[i], ref indices[j]);
			}
		}
		Swap(ref data[i + 1], ref data[high]);
		Swap(ref indices[i + 1], ref indices[high]);
		return i + 1;
	}

	private readonly record struct AnchorPair(int Low, int High);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static void QuickSort<T>(Span<T> data, Span<int> indices, int low, int high) where T : IComparable<T> {
		if (data.Length <= 1) {
			return;
		}

		// TODO : this can be parallelized, but I'm not sure if that's actually useful.
		int queueLength = 1;
		Span<AnchorPair> queue = stackalloc AnchorPair[data.Length];
		
		queue[0] = new(low, high);

		while (queueLength != 0) {
			var anchor = queue[--queueLength];
			if (anchor.Low >= anchor.High) {
				continue;
			}

			var pIndex = Partition(data, indices, anchor.Low, anchor.High);

			queue[queueLength++] = new(pIndex + 1, anchor.High);
			queue[queueLength++] = new(anchor.Low, pIndex - 1);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static void QuickSortByKey<T>(Span<T> data, Span<int> indices, int low, int high) where T : IComparable<T> {
		if (data.Length <= 1) {
			return;
		}

		// TODO : this can be parallelized, but I'm not sure if that's actually useful.
		int queueLength = 1;
		Span<AnchorPair> queue = stackalloc AnchorPair[data.Length];

		queue[0] = new(low, high);

		while (queueLength != 0) {
			var anchor = queue[--queueLength];
			if (anchor.Low >= anchor.High) {
				continue;
			}

			var pIndex = PartitionByKey(data, indices, anchor.Low, anchor.High);

			queue[queueLength++] = new(pIndex + 1, anchor.High);
			queue[queueLength++] = new(anchor.Low, pIndex - 1);
		}
	}

	/*
	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static void ArrayStableSort<T>(T[] array, int index, int length) where T : IComparable<T> {
		if (DrawState.CurrentBlendState == Microsoft.Xna.Framework.Graphics.BlendState.Additive) {
			// There is basically no reason to sort when the blend state is additive.
			return;
		}
		
		if (!Config.Enabled || !Config.Extras.StableSort) {
			Array.Sort(array, index, length);
			return;
		}

		// Not _optimal_, really need a proper stable sort. Optimize later.
		Span<int> indices = stackalloc int[length];
		for (int i = 0; i < length; ++i) {
			indices[i] = i;
		}
		var span = new Span<T>(array, index, length);
		QuickSort(span, indices, 0, length - 1);
	}*/

	private static int TotalElements = 0;
	private static long TotalDuration = 0;
	private static int CountCount = 0;
	private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static void ArrayStableSortByKey<T>(T[] array, int index, int length) where T : IComparable<T> {
		if (DrawState.CurrentBlendState == Microsoft.Xna.Framework.Graphics.BlendState.Additive) {
			// There is basically no reason to sort when the blend state is additive.
			return;
		}

		if (!Config.Enabled || !Config.Extras.StableSort) {
			Array.Sort(array, index, length);
			return;
		}

		var startTime = Stopwatch.Elapsed;

		// Not _optimal_, really need a proper stable sort. Optimize later.
		Span<int> indices = stackalloc int[length];
		for (int i = 0; i < length; ++i) {
			indices[i] = i;
		}
		var span = new Span<T>(array, index, length);
		if (GetSortKeyImpl is null) {
			QuickSort(span, indices, 0, length - 1);
		}
		else {
			QuickSortByKey(span, indices, 0, length - 1);
		}

		var fromStart = Stopwatch.Elapsed - startTime;
		TotalDuration += fromStart.Ticks;
		TotalElements += array.Length;

		if ((++CountCount % 200) == 0) {
			var duration = TimeSpan.FromTicks(TotalDuration);
			var durationPerElement = (duration * 1000) / TotalElements;

			Debug.Info($"Total Duration: {duration}, Total Count: {TotalElements} :: Per 1,000 Elements: {durationPerElement}");
		}
	}

	[Harmonize(
		typeof(XNA.Graphics.SpriteBatch),
		"Microsoft.Xna.Framework.Graphics.SpriteBatcher",
		"DrawBatch",
		fixation: Harmonize.Fixation.Transpile
	)]
	internal static IEnumerable<CodeInstruction> SpriteBatcherTranspiler(IEnumerable<CodeInstruction> instructions) {
		if (SpriteBatchItemType is null) {
			Debug.Error($"Could not apply SpriteBatcher stable sorting patch: {nameof(SpriteBatchItemType)} was null");
			return instructions;
		}

		if (GetSortKeyImpl is null) {
			Debug.Warning($"Could not get accessor for SpriteBatchItem 'SortKey' - slower path being used");
		}


		var newMethod = typeof(StableSort).GetMethod(GetSortKeyImpl is null ? "ArrayStableSortByKey" : "ArrayStableSortByKey", BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(new Type[] { SpriteBatchItemType });
		//var newMethod = typeof(StableSort).GetMethod("ArrayStableSort", BindingFlags.Static | BindingFlags.NonPublic)?.MakeGenericMethod(new Type[] { SpriteBatchItemType });

		if (newMethod is null) {
			Debug.Error($"Could not apply SpriteBatcher stable sorting patch: could not find MethodInfo for ArrayStableSort");
			return instructions;
		}

		IEnumerable<CodeInstruction> ApplyPatch() {
			foreach (var instruction in instructions) {
				if (
					instruction.opcode.Value != OpCodes.Call.Value ||
					instruction.operand is not MethodInfo callee ||
					!callee.IsGenericMethod ||
					callee.GetGenericArguments().FirstOrDefault() != SpriteBatchItemType ||
					callee.DeclaringType != typeof(Array) ||
					callee.Name != "Sort" ||
					callee.GetParameters().Length != 3
				) {
					yield return instruction;
					continue;
				}

				yield return new CodeInstruction(OpCodes.Call, newMethod);
			}
		}

		var result = ApplyPatch();

		if (result.SequenceEqual(instructions)) {
			Debug.Error("Could not apply SpriteBatcher stable sorting patch: Sort call could not be found in IL");
		}

		return result;
	}
}
