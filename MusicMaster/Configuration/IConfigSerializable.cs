using System.Diagnostics.CodeAnalysis;

namespace MusicMaster.Configuration;

internal interface IConfigSerializable {
	bool TrySerialize([NotNullWhen(true)] out string serialized);
	bool TryDeserialize(string serialized, [MaybeNullWhen(true)] out object? deserialized);
}

internal interface IConfigSerializable<T> : IConfigSerializable {
	bool TryDeserialize(string serialized, [MaybeNullWhen(true)] out T? deserialized);

	bool IConfigSerializable.TryDeserialize(string serialized, [MaybeNullWhen(true)] out object? deserialized) =>
		TryDeserialize(serialized, out deserialized);
}
