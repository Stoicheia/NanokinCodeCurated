using System;
using Anjin.Scripting;
using UnityEngine.Serialization;

namespace Anjin.Actors
{
	/// <summary>
	/// Points to an entry in the actor reference database.
	/// </summary>
	[Serializable, LuaUserdata]
	public struct ActorRef
	{
		public const string NULL_ID = "$NULL$";

		public static readonly ActorRef Nas = new ActorRef("cHxB2C4", "Party/Nas");

		public static readonly ActorRef NullRef = new ActorRef
		{
			ID   = NULL_ID,
			Name = "Unreferenced"
		};

		/// <summary>
		/// The unique ID that points to an entry in the database
		/// </summary>
		public string ID;

		public bool IsNullID => ID == NULL_ID;

		/// <summary>
		///	Purely for editor convenience, we save the name of the reference so if the reference is made invalid, we can at least see what the reference name was.
		/// </summary>
		[FormerlySerializedAs("CachedName")]
		public string Name;
		public string Path;

		public ActorRef(string id, string name)
		{
			ID = id;

			Path = "";
			Name = name;
		}

		public ActorRef(ActorReferenceDefinition def)
		{
			if (def != null)
			{
				ID = def.ID;

				Path = def.Path;
				Name = def.Name;
			}
			else
			{
				ID = NULL_ID;

				Path = "";
				Name = "Unreferenced";
			}
		}

		public override string ToString()
		{
			return $"[{ID},{Name}]";
		}
	}


	/// <summary>
	/// Points to an entry in the actor tags database
	/// </summary>
	[Serializable]
	public struct ActorTag
	{
		public const string NULL_ID = "$NULL$";

		public static ActorTag NullRef = new ActorTag
		{
			ID = NULL_ID,
		};

		/// <summary>
		/// The unique ID that points to an entry in the database
		/// </summary>
		public string ID;

		public bool IsNullID => ID == NULL_ID;

		public ActorTag(ActorTagDefinition def)
		{
			ID = def.ID;
		}

		public ActorTag(string id, string cachedName)
		{
			ID = id;
		}

	#region ID Utilities

		/// <summary>
		/// How long an ID for any actor tag should be
		/// </summary>
		public const int ID_LENGTH = 7;

		/// <summary>
		/// Generates a unique ID for use in a new actor reference.
		/// </summary>
		public string GenerateUniqueID(Random random = null) => DataUtil.MakeShortID(ID_LENGTH, random);

	#endregion
	}
}