using Combat.Startup;

namespace Combat
{
	public class ClientInfo : IIdentifiableInfo
	{
		public string     name;
		public TeamRecipe teamRecipe;

		public int ID { get; set; }
	}
}