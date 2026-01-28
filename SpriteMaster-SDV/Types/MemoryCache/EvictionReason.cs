namespace SpriteMaster.Types.MemoryCache;

public enum EvictionReason {
	None,
	/// <summary>Manually</summary>
	Removed,
	/// <summary>Overwritten</summary>
	Replaced,
	/// <summary>Timed out</summary>
	Expired,
	/// <summary>Event</summary>
	TokenExpired,
	/// <summary>Overflow</summary>
	Capacity,
}
