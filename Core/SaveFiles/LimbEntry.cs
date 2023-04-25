using System;
using Data.Combat;
using Data.Nanokin;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Assets.Nanokins
{
	[Serializable, JsonObject(MemberSerialization.Fields)]
	public class LimbEntry
	{
		[SerializeField, FormerlySerializedAs("_guid")]
		public string GUID;
		[SerializeField, TableColumnWidth(50)] public int Level;
		[SerializeField]                       public int RP;
		[SerializeField, AddressFilter("Limbs/", exclude: ".spritesheet")]
		public string Address;
		[SerializeField]					   public bool Favorited;

		[NonSerialized]
		public LimbInstance instance;

		public LimbEntry(int level = 1)
		{
			Level = level;
			GUID  = Guid.NewGuid().ToString();
		}

		public LimbEntry(string address, int level)
		{
			Level   = level;
			Address = address;
			GUID    = Guid.NewGuid().ToString();
		}

		public NanokinLimbAsset Asset => instance.Asset;

		/// <summary>
		/// Max points of the limb. (absolute value)
		/// </summary>
		public Pointf MaxPoints => instance.CalcMaxPoints(Level);

		public LimbType Kind => instance.Asset.Kind;

		public void UpdateInstance(bool isDeserializing = false)
		{
			instance = instance ?? new LimbInstance();

			instance.entry   = this;
			instance.Mastery = Level;
			instance.RP      = RP;
			instance.Favorited = Favorited;
			instance.Asset   = GameAssets.GetLimb(Address);
		}

		public void ApplyInstance()
		{
			if (instance != null)
			{
				Level   = instance.Mastery;
				RP      = instance.RP;
				Favorited = instance.Favorited;
				if (instance.Asset != null)
					Address = instance.Asset.Address;
				else
					Address = instance.entry.Address;
			}
		}
	}
}