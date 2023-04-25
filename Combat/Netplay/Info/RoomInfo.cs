namespace Combat
{
	public class RoomInfo : IIdentifiableInfo
	{
		public string         name;
		public RoomInsideInfo inside = new RoomInsideInfo();

		public int ID { get; set; }
	}
}