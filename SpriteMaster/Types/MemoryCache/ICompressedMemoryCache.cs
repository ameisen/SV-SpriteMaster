using SpriteMaster.Tasking;
using System.Threading;
using System.Threading.Tasks;

namespace SpriteMaster.Types.MemoryCache;

internal interface ICompressedMemoryCache : ICache {
	private static readonly ThreadedTaskScheduler CompressedMemoryCacheScheduler = new(
		concurrencyLevel: null,
		threadNameFunction: i => $"CompressedMemoryCache Compression Thread {i}",
		useBackgroundThreads: true,
		threadPriority: ThreadPriority.Lowest
	);
	protected static readonly TaskFactory TaskFactory = new(CompressedMemoryCacheScheduler);
}
