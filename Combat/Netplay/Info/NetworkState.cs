using Combat.Networking;

namespace Combat
{
	public static class NetworkState
	{
		public static int                                    ClientID { get; set; }
		public static RegistryInformationReference<RoomInfo> Room     { get; } = new RegistryInformationReference<RoomInfo>(NetworkInformation.RoomRegistry);
		public static BattleInfo                             Battle   { get; set; }
	}
}