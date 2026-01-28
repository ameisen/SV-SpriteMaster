using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace SpriteMaster.Types;

internal interface IPurgeable {
	internal readonly struct Target {
		[Pure]
		internal readonly ulong CurrentMemoryUsage { get; init; }
		[Pure]
		internal readonly ulong TargetMemoryUsage { get; init; }

		[Pure]
		internal readonly ulong Difference => (ulong)Math.Max(0L, (long)CurrentMemoryUsage - (long)TargetMemoryUsage);
	}

	ulong? OnPurgeHard(Target target, CancellationToken cancellationToken = default);
	ulong? OnPurgeSoft(Target target, CancellationToken cancellationToken = default);

	Task<ulong?> PurgeHardAsync(Target target, CancellationToken cancellationToken = default) =>
		Task.Run(() => OnPurgeHard(target, cancellationToken), cancellationToken);
	Task<ulong?> PurgeSoftAsync(Target target, CancellationToken cancellationToken = default) =>
		Task.Run(() => OnPurgeSoft(target, cancellationToken), cancellationToken);
}
