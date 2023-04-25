using Anjin.Util;
using DG.Tweening;
using UnityEngine;

namespace Util.Extensions
{
	public static partial class Extensions
	{
		public static Tween DestroyOnComplete(this Tween t, params GameObject[] game_objects_to_destroy)
		{
			t.OnComplete(() =>
			{
				foreach (GameObject gameObject in game_objects_to_destroy)
				{
					gameObject.Destroy();
				}
			});

			return t;
		}
	}
}