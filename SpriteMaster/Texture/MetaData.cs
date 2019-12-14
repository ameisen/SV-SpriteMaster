using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Texture
{
	internal class MetaData
	{
		internal byte[] Data
		{
			get
			{
				if (Data_ != null && Data_.TryGetTarget(out var data))
				{
					return data;
				}
				return null;
			}
		}

		private WeakReference<byte[]> Data_ = null;
	}
}
