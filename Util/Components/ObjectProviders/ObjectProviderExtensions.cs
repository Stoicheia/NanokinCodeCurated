using UnityEngine;

namespace Util.ObjectProviders
{
	public static class ObjectProviderExtensions
	{
		public static TComponent GetNext<TComponent>(this ObjectProvider provider)
		{
			GameObject next          = provider.GetNext();
			TComponent nextComponent = next.GetComponent<TComponent>();
			
			return nextComponent;
		}
	}
}