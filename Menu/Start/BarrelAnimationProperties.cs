using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.RenderingElements.Barrel;

namespace Menu.Start
{
	public class BarrelAnimationProperties : SerializedMonoBehaviour
	{
		[SerializeField] public Vector3 PanelsOffset;
		[SerializeField] public float   PanelsOpacity;

		[NonSerialized] public Action<MenuInteractivity> onUpdateInteractivity;
		[NonSerialized] public Action<bool>                onUpdateMenuVisibility;

		private BarrelMenu _barrel;

		private void Awake()
		{
			_barrel = GetComponent<BarrelMenu>();
		}

		public void UpdateInputLevel(MenuInteractivity lvl)
		{
			onUpdateInteractivity?.Invoke(lvl);
		}

		public void UpdateMenuVisibility(int state)
		{
			onUpdateMenuVisibility(state > 0);
		}

		private void LateUpdate()
		{
			foreach (ListPanel panel in _barrel.AllPanels)
			{
				panel.offset  = PanelsOffset;
				panel.opacity = PanelsOpacity;
			}
		}
	}
}