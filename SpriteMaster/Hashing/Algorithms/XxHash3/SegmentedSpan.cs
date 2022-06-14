using Microsoft.Toolkit.HighPerformance;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Hashing.Algorithms;

internal static partial class XxHash3 {
	private readonly ref struct SegmentedSpan {
		internal readonly ReadOnlySpan2D<byte> Source;

		internal readonly uint Length => (uint)Source.Length;
		internal readonly uint Width => (uint)Source.Width;

		[MethodImpl(Inline)]
		internal SegmentedSpan(ReadOnlySpan2D<byte> source) {
			Source = source;
		}

		[Pure]
		[MethodImpl(Inline)]
		internal readonly ReadOnlySpan<byte> Slice(Span<byte> temp, uint offset, uint length) {
			uint end = offset + length;

			uint offsetRow = offset / Width;
			uint endRow = (end - 1U) / Width;

			ReadOnlySpan<byte> result;
			if (offsetRow == endRow) {
				result = SliceFast(offset, length, offsetRow);
			}
			else {
				result = SliceSlow(temp, offset, length, offsetRow, endRow);
			}

#if DEBUG
			var copySlice = Source.ToArray().AsSpan().Slice((int)offset, (int)length);
			if (!copySlice.SequenceEqual(result)) {
				Debugger.Break();
				throw new Exception("Sequence Mismatch");
			}
#endif

			return result;
		}

		[MethodImpl(Inline)]
		internal readonly ReadOnlySpan<byte> SliceTo(Span<byte> destination, uint offset) {
			uint length = (uint)destination.Length;
			uint end = offset + length;

			uint offsetRow = offset / Width;
			uint endRow = (end - 1U) / Width;
			if (offsetRow == endRow) {
				return SliceFast(destination, offset, offsetRow);
			}
			else {
				return SliceSlow(destination, offset, offsetRow, endRow);
			}

#if DEBUG
			var copySlice = Source.ToArray().AsSpan().Slice((int)offset, (int)length);
			if (!copySlice.SequenceEqual(destination)) {
				Debugger.Break();
				throw new Exception("Sequence Mismatch");
			}
#endif
		}

		[Pure]
		[MethodImpl(Hot)]
		// The slice crosses multiple rows
		private readonly ReadOnlySpan<byte> SliceSlow(Span<byte> temp, uint offset, uint length, uint startRow, uint endRow) {
			// TODO : find a faster way to do this
			uint rowOffset = offset % Width;
			uint currentWriteOffset = 0U;

			if (rowOffset != 0U) {
				uint copyLength = Math.Min(length, Width - rowOffset);

				var rowSpan = Source.GetRowSpan((int)startRow).Slice((int)rowOffset, (int)copyLength);
				rowSpan.CopyTo(temp.Slice(0, (int)copyLength));

				currentWriteOffset += copyLength;
				++startRow;
			}

			for (uint row = startRow; row <= endRow; ++row) {
				uint copyLength = Math.Min(length - currentWriteOffset, Width);

				var rowSpan = Source.GetRowSpan((int)row).Slice(0, (int)copyLength);
				rowSpan.CopyTo(temp.Slice((int)currentWriteOffset, (int)copyLength));

				currentWriteOffset += copyLength;
			}

			return temp;
		}

		[MethodImpl(Hot)]
		// The slice crosses multiple rows
		private readonly ReadOnlySpan<byte> SliceSlow(Span<byte> destination, uint offset, uint startRow, uint endRow) {
			// TODO : find a faster way to do this
			uint length = (uint)destination.Length;
			uint rowOffset = offset % Width;
			uint currentWriteOffset = 0U;

			if (rowOffset != 0U) {
				uint copyLength = Math.Min(length, Width - rowOffset);

				var rowSpan = Source.GetRowSpan((int)startRow).Slice((int)rowOffset, (int)copyLength);
				rowSpan.CopyTo(destination.Slice(0, (int)copyLength));

				currentWriteOffset += copyLength;
				++startRow;
			}

			for (uint row = startRow; row <= endRow; ++row) {
				uint copyLength = Math.Min(length - currentWriteOffset, Width);

				var rowSpan = Source.GetRowSpan((int)row).Slice(0, (int)copyLength);
				rowSpan.CopyTo(destination.Slice((int)currentWriteOffset, (int)copyLength));

				currentWriteOffset += copyLength;
			}

			return destination;
		}

		[Pure]
		[MethodImpl(Inline)]
		// The slice is entirely within a row
		internal readonly ReadOnlySpan<byte> SliceFast(uint offset, uint length, uint row) {
			var rowSpan = Source.GetRowSpan((int)row);
			if (length == Width) {
				return rowSpan;
			}
			uint rowOffset = offset % Width;
			return rowSpan.Slice((int)rowOffset, (int)length);
		}

		[MethodImpl(Inline)]
		// The slice is entirely within a row
		internal readonly ReadOnlySpan<byte> SliceFast(Span<byte> destination, uint offset, uint row) {
			uint length = (uint)destination.Length;

			var rowSpan = Source.GetRowSpan((int)row);
			if (length == Width) {
				return rowSpan;
			}
			uint rowOffset = offset % Width;
			return rowSpan.Slice((int)rowOffset, (int)length);
		}
	}
}
