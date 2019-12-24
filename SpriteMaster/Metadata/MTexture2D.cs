namespace SpriteMaster.Metadata {
	internal sealed class MTexture2D {
		public long LastAccessFrame { get; private set; } = DrawState.CurrentFrame;
		public ulong Hash = 0;

		public void UpdateLastAccess() {
			LastAccessFrame = DrawState.CurrentFrame;
		}
	}
}
