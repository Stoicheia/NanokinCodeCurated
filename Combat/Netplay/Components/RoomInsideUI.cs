using System;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace Combat
{
	public class RoomInsideUI : SerializedMonoBehaviour
	{
		[NonSerialized] public Action onLeave;
		[NonSerialized] public Action onStartBattle;

		[SerializeField] private TextMeshProUGUI                     _tmpRoomNameLabel;
		[SerializeField] private Button                              _btnLeave;
		[SerializeField] private Button                              _btnStartBattle;
		[SerializeField] private GameObjectListPool<TextMeshProUGUI> _memberLabelPool;

		private RoomInfo _roomInfo;

		private void Start()
		{
			_btnLeave.onClick.AddListener(() =>
			{
				onLeave?.Invoke();
			});

			_btnStartBattle.onClick.AddListener(() =>
			{
				onStartBattle?.Invoke();
			});
		}

		public void ShowUI()
		{
			gameObject.SetActive(true);
		}

		public void ShowUI([NotNull] RoomInfo room)
		{
			ShowUI();

			_roomInfo              = room;
			_tmpRoomNameLabel.text = room.name;

			UpdateMembers();
		}

		public void UpdateMembers()
		{
			_memberLabelPool.Reserve(_roomInfo.inside.members);

			for (var i = 0; i < _roomInfo.inside.members.Count; i++)
			{
				int             memberID = _roomInfo.inside.members[i];
				TextMeshProUGUI label    = _memberLabelPool[i];

				ClientInfo clientInfo = NetworkInformation.Clients[memberID];
				label.text = $"{i + 1}. {clientInfo.name}";
			}
		}

		public void HideUI()
		{
			gameObject.SetActive(false);
		}

		public void Leave() { }
	}
}