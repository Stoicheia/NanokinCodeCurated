using Anjin.Util;
using UnityEngine;

namespace Overworld.Tags
{
	public interface IActivable
	{
		void OnActivate();
		void OnDeactivate();
	}

	public static class IActivableExtensions {

		public static void SetActive(this IActivable activable, bool active)
		{
			if (active) {
				activable.OnActivate();
			} else {
				activable.OnDeactivate();
			}

			if (activable is Component comp)
				comp.gameObject.SetActive(active);

		}
	}
}