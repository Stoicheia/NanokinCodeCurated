using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Core.Flags
{
	public class FlagDefDatabase : SerializedScriptableObject
	{
		public List<FlagDefinitionBase> Flags;

		#region Database Stuff
		public const string DBResourcesPath = "Data/Flags";

		private static FlagDefDatabase _loadedDB;

		[ShowInInspector]
		public static FlagDefDatabase LoadedDB {
			get
			{
				if (_loadedDB == null)
				{
					_loadedDB = Resources.Load<FlagDefDatabase>(DBResourcesPath);
				}

				return _loadedDB;
			}
		}
		#endregion
	}
}