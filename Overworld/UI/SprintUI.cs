using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Util.RenderingElements.PointBars;

namespace Anjin.Nanokin.Map
{
	public class SprintUI : SerializedMonoBehaviour
	{
		[SerializeField] public  PointBar Bar;
		[SerializeField] private Image    BarImage;
		[Space]
		[SerializeField] private Color InactivableColor;
		[SerializeField] private Color ActivableColor;

		[NonSerialized]
		public bool canSprint;

		private void Update()
		{
			BarImage.color = canSprint ? ActivableColor : InactivableColor;
		}
	}
}