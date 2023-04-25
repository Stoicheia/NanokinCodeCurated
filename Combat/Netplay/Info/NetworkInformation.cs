namespace Combat
{
	public static class NetworkInformation
	{
		public static IdentifiableInformationRegistry<RoomInfo> RoomRegistry { get; } = new IdentifiableInformationRegistry<RoomInfo>();

		public static ClientInformationRegistry Clients { get; } = new ClientInformationRegistry();
	}
}