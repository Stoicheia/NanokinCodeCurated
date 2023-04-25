using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Combat
{
	public class RoomListUI : SerializedMonoBehaviour
	{
		[SerializeField, Tooltip("The object which will hold the buttons.")]
		private GameObject _roomButtonContainer;

		[SerializeField, Tooltip("The prefab to instantiate for a room button.")]
		private GameObject _pfbRoomButton;

		private List<RoomButtonUI>            _buttons       = new List<RoomButtonUI>();
		private Dictionary<int, RoomButtonUI> _buttonsByRoom = new Dictionary<int, RoomButtonUI>();

		public Action<RoomInfo> onRoomPicked;

		public void HideUI()
		{
			gameObject.SetActive(false);
		}

		public void ShowUI([NotNull] List<RoomInfo> rooms)
		{
			gameObject.SetActive(true);

			_buttonsByRoom.Clear();

			while (_buttons.Count < rooms.Count)
			{
				// Too few buttons, we need to allocate a couple
				GameObject   goButton = Instantiate(_pfbRoomButton, _roomButtonContainer.transform);
				RoomButtonUI btnRoom  = goButton.GetComponent<RoomButtonUI>();
				Button       btnUI    = goButton.GetComponent<Button>();

				btnUI.onClick.AddListener(() =>
				{
					onRoomPicked?.Invoke(btnRoom.RoomInfo);
				});

				_buttons.Add(btnRoom);
			}

			while (_buttons.Count > rooms.Count)
			{
				// Too many buttons for the rooms we need to display.
				_buttons.RemoveAt(_buttons.Count - 1);
			}

			for (var index = 0; index < rooms.Count; index++)
			{
				RoomInfo     roomInfo     = rooms[index];
				RoomButtonUI roomButtonUI = _buttons[index];

				roomButtonUI.RoomInfo = roomInfo;

				_buttonsByRoom.Add(roomInfo.ID, roomButtonUI);
			}
		}
	}
}