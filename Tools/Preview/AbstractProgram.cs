﻿namespace SpriteMaster.Tools.Preview;

internal abstract class AbstractProgram : IDisposable, IAsyncDisposable {
	internal static AbstractProgram? Instance;

	internal virtual int SubMain(Options options, List<Argument> arguments) {
		return Task.Run(async () => await SubMainAsync(options, arguments)).Result;
	}

	internal Task<int> SubMainAsync(Options options, List<Argument> arguments) {
		if (Interlocked.Exchange(ref Instance, this) is { } oldInstance) {
			Console.Error.WriteLine("Old Program instance was not properly disposed");
		}
		return OnSubMainAsync(options, arguments);
	}

	internal abstract Task<int> OnSubMainAsync(Options options, List<Argument> arguments);

	public virtual void Dispose() {
		Interlocked.CompareExchange(ref Instance, null, this);
	}

	public virtual ValueTask DisposeAsync() {
		return Instance is null ?
			ValueTask.CompletedTask :
			new(Task.Run(Dispose));
	}
}
