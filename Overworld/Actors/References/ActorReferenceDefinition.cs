using System;

namespace Anjin.Actors
{
	public class ActorReferenceDefinition
	{
		/*/// <summary>
		/// Provides a way to designate what kind of actor this reference should be used for, to better help organization
		/// </summary>
		public enum RefType
		{
			None,
			Character,
		}

		/// <summary>
		/// Provides a way to designate which domain this actor generally operates in.
		/// </summary>
		public enum RefDomain
		{
			/// <summary>
			/// This actor reference may be encountered in multiple levels
			/// </summary>
			Global,

			/// <summary>
			/// This actor reference is only ever encountered in one level
			/// </summary>
			Level,
		}*/

		public string ID;
		public string Name;

		/// <summary>
		/// For organization. Provides a way to organize references in a folder like structure without affecting the reference itself.
		///
		/// Definition:
		/// 	[folder name]/[folders...]/
		///
		/// </summary>]
		public string Path;

		/*#region Metadata
		public string LevelName = "";
		#endregion*/

		public ActorReferenceDefinition() : this("") { }
		public ActorReferenceDefinition(string name)
		{
			ID = GenerateUniqueID();
			Name = name;
			Path = "";
		}

		public ActorReferenceDefinition(string name, string path)
		{
			ID   = GenerateUniqueID();
			Name = name;
			Path = path;
		}

		/// <summary>
		///
		///	Validates a path.
		///
		/// Path Rules:
		///
		/// -Must not be null.
		/// -Must contain at least one slash.
		/// -Should not contain double slashes.
		/// -After slashes and spaces are removed, must have at least one character.
		///
		/// </summary>
		/// <returns></returns>
		public static bool IsPathValid(string path)
		{
			if ( path == null) 										 return false;
			//if (!path.Contains("/")) 								 return false;
			if ( path.Contains("//"))								 return false;
			if ( path.Replace("/", "").Replace(" ", "").Length == 0) return false;

			return true;
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