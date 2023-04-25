using Sirenix.OdinInspector;
using UnityEngine;

namespace Combat.Components.VictoryScreen.Menu
{
	public class VColumn : SerializedMonoBehaviour
	{
		public RectTransform Rect;

		public virtual void StepGains(ref int currentTotal) { }
	}
}