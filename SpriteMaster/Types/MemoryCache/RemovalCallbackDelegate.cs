namespace SpriteMaster.Types.MemoryCache;

public delegate void RemovalCallbackDelegate<TKey, TValue>(EvictionReason reason, TKey key, TValue element) where TKey : notnull where TValue : notnull;
