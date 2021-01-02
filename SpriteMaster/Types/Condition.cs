using System.Threading;

namespace SpriteMaster.Types
{
	internal sealed class Condition
	{
		private volatile bool State = false;
		private volatile AutoResetEvent Event = new(false);

		public Condition(bool initialState = false)
		{
			State = initialState;
		}

		public static implicit operator bool(Condition condition) => condition.State;

		// This isn't quite thread-safe, but the granularity of this in our codebase is really loose to begin with. It doesn't need to be entirely thread-safe.
		public bool Wait()
		{
			Event.WaitOne();
			return State;
		}

		public void Set(bool state = true)
		{
			State = state;
			Event.Set();
		}

		// This clears the state without triggering the event.
		public void Clear()
		{
			State = false;
		}
	}
}
