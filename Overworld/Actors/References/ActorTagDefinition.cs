using UnityEngine;
using Random = System.Random;

namespace Anjin.Actors
{
	public class ActorTagDefinition
	{
		public string ID;
		public string Name;

		#region Metadata

		#if UNITY_EDITOR
		public Color tint = Color.white;
		#endif

		#endregion

		public ActorTagDefinition() : this("") { }
		public ActorTagDefinition(string name)
		{
			Name = name;
			ID = GenerateUniqueID();
		}

		#region ID Utilities
		/// <summary>
		/// How long an ID for any actor reference should be
		/// </summary>
		public const int ID_LENGTH = 7;

		/// <summary>
		/// Generates a unique ID for use in a new actor reference.
		/// </summary>
		public string GenerateUniqueID(Random random = null) => DataUtil.MakeShortID(ID_LENGTH, random);
		#endregion
	}
}