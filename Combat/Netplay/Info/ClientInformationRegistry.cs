using Combat.Startup;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;

namespace Combat
{
	public class ClientInformationRegistry : IdentifiableInformationRegistry<ClientInfo>
	{
		[NotNull]
		public ClientInfo UpdateDisplayInformation([NotNull] PacketReader pr)
		{
			int    clientID   = pr.Int();
			string clientName = pr.String();

			ClientInfo info = Update(clientID);
			info.name = clientName;

			return info;
		}

		public void UpdateTeam([NotNull] PacketReader pr)
		{
			int        clientID         = pr.Int();
			TeamRecipe clientTeamRecipe = pr.UnitTeam();

			ClientInfo info = Update(clientID);
			info.teamRecipe = clientTeamRecipe;
		}
	}
}