using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat
{
	public class RegistryInformationReference<TInfo>
		where TInfo : class, IIdentifiableInfo, new()
	{
		private IdentifiableInformationRegistry<TInfo> _registry;

		public RegistryInformationReference([NotNull] IdentifiableInformationRegistry<TInfo> registry)
		{
			if (registry == null)
				throw new ArgumentException("The registry is null! It must be initialized in order to refer information contained within it.");

			_registry = registry;
		}

		public bool HasValue => ID >= 0;

		public bool IsUnassigned => ID < 0;

		public int ID { get; private set; } = -1;

		public TInfo Value
		{
			get
			{
				if (ID >= 0)
					throw new InvalidOperationException("The reference is currently unassigned!");

				if (!_registry.HasInformation(ID))
					throw new InvalidOperationException("");

				return _registry[ID];
			}
		}

		/// <summary>
		/// Clear the information reference.
		/// </summary>
		public void Clear()
		{
			ID = -1;
		}

		/// <summary>
		/// Set the information reference by ID. The information should be registred within the registry at this point.
		/// </summary>
		/// <param name="informationID"></param>
		public void Set(int informationID)
		{
			if (!_registry.HasInformation(informationID))
			{
				Debug.LogError("The information reference has been set to information which is not contained by the registry. The registry should be updated with that information first before setting the reference.");
			}

			ID = informationID;
		}

		public bool Get(out TInfo roomInfo)
		{
			if (IsUnassigned)
			{
				roomInfo = null;
				return false;
			}

			roomInfo = _registry[ID];
			return true;
		}
	}
}