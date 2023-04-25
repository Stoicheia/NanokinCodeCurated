using Sirenix.OdinInspector;

namespace Combat
{
	public class RoomUI : SerializedMonoBehaviour
	{
		public RoomListUI   roomListUI;
		public RoomInsideUI roomInsideUI;

		private void Awake()
		{
			roomListUI.HideUI();
			roomInsideUI.HideUI();
		}

		public void ShowRoom(RoomInfo room) { }
	}
}