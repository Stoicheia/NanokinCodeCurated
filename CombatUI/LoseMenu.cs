using System;
using Anjin.Nanokin;
using Anjin.Util;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Combat.UI
{
	public class LoseMenu : StaticBoy<LoseMenu>
	{
		[SerializeField] private GameObject Root;
		[SerializeField] private Button     RetryButton;
		[SerializeField] private Button     RespawnButton;

		[NonSerialized] public UnityAction onRetry;
		[NonSerialized] public UnityAction onRespawn;

		protected override void OnAwake()
		{
			RetryButton.onClick.AddListener(() => onRetry?.Invoke());
			RespawnButton.onClick.AddListener(() => onRespawn?.Invoke());
		}

		private void Update()
		{
			if (GameInputs.confirm.IsPressed)
			{
				if (EventSystem.current.currentSelectedGameObject == RetryButton.gameObject)
				{
					onRetry?.Invoke();
				} else if (EventSystem.current.currentSelectedGameObject == RespawnButton.gameObject)
				{
					onRetry?.Invoke();
				}
			}

			if (Root.activeSelf && GameInputs.move.AnyPressed && EventSystem.current.currentSelectedGameObject == null)
			{
				RetryButton.Select();
			}
		}

		public void SetVisible(bool state)
		{
			Root.SetActive(state);
			GameInputs.mouseUnlocks.Set("lose_menu", state);

			if (state)
				RetryButton.Select();
		}
	}
}