using JetBrains.Annotations;

namespace SpriteMaster;

public sealed partial class SpriteMaster {
	[UsedImplicitly]
	public void Entry() {
		Initialize();

		PostInitialize();
	}
}
