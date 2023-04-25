using System.Collections.Generic;
using JetBrains.Annotations;

namespace Combat
{
	public class RoomInsideInfo
	{
		public readonly List<int> members = new List<int>();

		public void AddMember([NotNull] ClientInfo client)
		{
			members.Add(client.ID);
		}

		public void RemoveMember([NotNull] ClientInfo client)
		{
			members.Remove(client.ID);
		}
	}
}