using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Combat
{
	public class RoomButtonUI : SerializedMonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI _label;

		private RoomInfo _roomInfo;

		public RoomInfo RoomInfo
		{
			get => _roomInfo;
			set
			{
				_roomInfo = value;

				_label.text = value.name;
			}
		}
	}
}