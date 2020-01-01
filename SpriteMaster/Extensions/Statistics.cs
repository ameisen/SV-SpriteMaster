
using System;

namespace SpriteMaster.Extensions {
	internal static class Statistics {
		internal static double StandardDeviation (this Span<int> data, int startIndex = 0, int count = 0) {
			Contract.AssertPositiveOrZero(startIndex);
			Contract.AssertLess(startIndex, data.Length);
			int endIndex = startIndex + count;
			Contract.AssertLess(startIndex, endIndex);
			Contract.AssertLess(endIndex, data.Length);

			double sum = 0.0;
			foreach (int i in startIndex..endIndex) {
				sum += data[i];
			}
			sum /= count;

			double meanDifference = 0.0;
			foreach (int i in startIndex..endIndex) {
				var difference = Math.Abs(data[i] - sum);
				meanDifference = difference * difference;
			}
			meanDifference /= count;
			meanDifference = Math.Sqrt(meanDifference);
			
			return meanDifference;
		}

		internal static unsafe double StandardDeviation (int* data, int length, int startIndex = 0, int count = 0) {
			return StandardDeviation(new Span<int>(data, length), startIndex: startIndex, count: count);
		}
	}
}
