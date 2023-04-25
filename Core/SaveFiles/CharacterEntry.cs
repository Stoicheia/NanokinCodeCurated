using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anjin.Scripting;
using Assets.Nanokins;
using Combat.Entry;
using Data.Combat;
using Data.Nanokin;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using Random = System.Random;

namespace SaveFiles
{
	[Serializable, JsonObject(MemberSerialization.OptIn)]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[LuaUserdata]
	public class CharacterEntry
	{
		/// <summary>
		/// Unique ID to refer to this party member.
		/// </summary>
		[SerializeField]
		[JsonProperty("GUID")]
		[FormerlySerializedAs("_guid")]
		[ReadOnly]
		[PropertyOrder(-1)]
		public string ID = null;

		/// <summary>
		/// Address of the character asset.
		/// </summary>
		[SerializeField]
		[JsonProperty("Address")]
		[AddressFilter("Characters/")]
		public string Address = "Characters/Nas";

		/// <summary>
		/// Name of the monster.
		/// </summary>
		[SerializeField]
		[JsonProperty("Name")]
		public string MonsterName;

		/// <summary>
		/// Custom name for the monster.
		/// </summary>
		[CanBeNull]
		[JsonProperty("NanokinName")]
		public string NanokinName;


		/// <summary>
		/// Level of the character. (technically their splicer device)
		/// </summary>
		[JsonProperty("Level")]
		public int Level = 1;

		/// <summary>
		/// XP of the character. (technically their splicer device)
		/// </summary>
		[JsonProperty("XP")]
		public int XP = 0;

		[SerializeField]
		[JsonProperty("Formation")]
		public Vector2Int FormationCoord;

		[JsonProperty]
		public int GridWidth = 5;

		[JsonProperty]
		public int GridHeight = 4;

		[JsonProperty("Stickers")]
		public List<StickerEntry> StickerEntries = new List<StickerEntry>();

		[JsonProperty("StringData")]
		public Dictionary<string, string> StringData = new Dictionary<string, string>();

		[JsonProperty("IntData")]
		public Dictionary<string, int> IntData = new Dictionary<string, int>();

		/// <summary>
		/// Points of the monster, stored in normalized values. (0 to 1)
		/// </summary>
		[SerializeField, Inline, DarkBox]
		[JsonProperty("Points")]
		public Pointf Points = Pointf.One;

		[FormerlySerializedAs("bodyGUID"), SerializeField]
		[JsonProperty("Body")]
		[LabelText("Body")]
		[InventoryLimbPicker(Data.Nanokin.LimbType.Body)]
		public string BodyGUID;

		[FormerlySerializedAs("headGUID"), SerializeField]
		[JsonProperty("Head")]
		[LabelText("Head")]
		[InventoryLimbPicker(Data.Nanokin.LimbType.Head)]
		public string HeadGUID;

		[FormerlySerializedAs("arm1GUID"), SerializeField]
		[JsonProperty("Arm1")]
		[LabelText("Arm1")]
		[InventoryLimbPicker(Data.Nanokin.LimbType.Arm1)]
		public string Arm1GUID;

		[FormerlySerializedAs("arm2GUID"), SerializeField]
		[JsonProperty("Arm2")]
		[LabelText("Arm2")]
		[InventoryLimbPicker(Data.Nanokin.LimbType.Arm2)]
		public string Arm2GUID;

		[NonSerialized, ShowInInspector]
		public NanokinInstance nanokin;

		public string Name => asset.Name;

		[CanBeNull]
		public CharacterAsset asset => GameAssets.GetCharacter(Address);

		public int NextLevelXP => (int)asset.XPCurve.Evaluate(Level);

		public float NextLevelProgress => Mathf.Clamp01(XP / asset.XPCurve.Evaluate(Level));

		public int NextLevelXPLeft => Mathf.Max(0, NextLevelXP - XP);

		public bool has_int(string key)
		{
			return (IntData.ContainsKey(key));
		}

		public int load_int(string key)
		{
			if (!has_int(key))
			{
				IntData.Add(key, 0);
			}

			return IntData[key];
		}

		public void save_int(string key, int value)
		{
			if (has_int(key))
			{
				IntData[key] = value;
			}
			else
			{
				IntData.Add(key, value);
			}
		}

		public void delete_int(string key)
		{
			if (has_int(key))
			{
				IntData.Remove(key);
			}
		}

		public void clear_ints()
		{
			IntData.Clear();
		}

		public bool has_string(string key)
		{
			return (StringData.ContainsKey(key));
		}

		public string load_string(string key)
		{
			if (!has_string(key))
			{
				StringData.Add(key, "");
			}

			return StringData[key];
		}

		public void save_string(string key, string value)
		{
			if (has_string(key))
			{
				StringData[key] = value;
			}
			else
			{
				StringData.Add(key, value);
			}
		}

		public void delete_string(string key)
		{
			if (has_string(key))
			{
				StringData.Remove(key);
			}
		}

		public void clear_strings()
		{
			StringData.Clear();
		}

		public CharacterEntry()
		{
			ID = Guid.NewGuid().ToString();
		}

		public CharacterEntry(string address) : this()
		{
			Address = address;
		}

		// Shortcuts
		public List<StickerInstance> Stickers => nanokin.Stickers;

		public LimbInstance[] Limbs => nanokin.Limbs;

		[CanBeNull]
		public LimbInstance Body
		{
			get => nanokin.Body;
			set
			{
				nanokin.Body = value;
				ApplyInstance();
			}
		}

		[CanBeNull]
		public LimbInstance Head
		{
			get => nanokin.Head;
			set
			{
				nanokin.Head = value;
				ApplyInstance();
			}
		}

		[CanBeNull]
		public LimbInstance Arm1
		{
			get => nanokin.Arm1;
			set
			{
				nanokin.Arm1 = value;
				ApplyInstance();
			}
		}

		[CanBeNull]
		public LimbInstance Arm2
		{
			get => nanokin.Arm2;
			set
			{
				nanokin.Arm2 = value;
				ApplyInstance();
			}
		}

		public LimbEntry this[LimbType type]
		{
			get
			{
				switch (type)
				{
					case Data.Nanokin.LimbType.Body: return Body.entry;
					case Data.Nanokin.LimbType.Head: return Head.entry;
					case Data.Nanokin.LimbType.Arm1: return Arm1.entry;
					case Data.Nanokin.LimbType.Arm2: return Arm2.entry;
					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, null);
				}
			}
			set
			{
				switch (type)
				{
					case Data.Nanokin.LimbType.Body:
						Body = value.instance;
						break;

					case Data.Nanokin.LimbType.Head:
						Head = value.instance;
						break;

					case Data.Nanokin.LimbType.Arm1:
						Arm1 = value.instance;
						break;

					case Data.Nanokin.LimbType.Arm2:
						Arm2 = value.instance;
						break;

					case Data.Nanokin.LimbType.None:
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, null);
				}
			}
		}

		public Pointf MaxPoints
		{
			get
			{
				Pointf total = Pointf.Zero;

				total += Body.CalcMaxPoints(Level);
				total += Head.CalcMaxPoints(Level);
				total += Arm1.CalcMaxPoints(Level);
				total += Arm2.CalcMaxPoints(Level);

				return total;
			}
		}

		public bool GainXP(int gain)
		{
			if (Level == StatConstants.MAX_LEVEL) return false;

			bool changed = false;

			XP += gain;
			while (XP >= NextLevelXP && Level < StatConstants.MAX_LEVEL)
			{
				XP -= NextLevelXP;
				Level++;
				changed = true;
			}

			nanokin.Level = Level;
			nanokin.XP = XP;
			return changed;
		}

		static Random _rand = new Random();

		public void ScrambleLimbs(SaveData save)
		{
			Head = save.Limbs.Where(x => x.Kind == LimbType.Head).RandomElement(_rand).instance;
			Body = save.Limbs.Where(x => x.Kind == LimbType.Body).RandomElement(_rand).instance;
			Arm1 = save.Limbs.Where(x => x.Kind == LimbType.Arm1).RandomElement(_rand).instance;
			Arm2 = save.Limbs.Where(x => x.Kind == LimbType.Arm2).RandomElement(_rand).instance;

			ApplyInstance();
		}

		/// <summary>
		/// Add a sticker to the instance.
		/// </summary>
		public void AddSticker(StickerInstance sticker)
		{
			StickerEntries.Add(sticker.Entry);

			nanokin.Stickers.Add(sticker);
			nanokin.RecalculateStats();
		}

		/// <summary>
		/// Remove a sticker from the instance.
		/// </summary>
		/// <param name="sticker"></param>
		public void RemoveSticker(StickerInstance sticker)
		{
			StickerEntries.Remove(sticker.Entry);

			nanokin.Stickers.Remove(sticker);
			nanokin.RecalculateStats();
		}


		private void Heal(int hp)
		{
			float percent = hp / MaxPoints.hp;
			nanokin.Points.hp += percent;
		}

		private void HealPercent(float percent)
		{
			nanokin.Points.hp += percent;
		}

		public void UpdateInstance(SaveData data, bool isDeserializing = false)
		{
			nanokin        = nanokin ?? new NanokinInstance();
			nanokin.entry  = this;
			nanokin.Head   = data.GetLimb(HeadGUID)?.instance;
			nanokin.Body   = data.GetLimb(BodyGUID)?.instance;
			nanokin.Arm1   = data.GetLimb(Arm1GUID)?.instance;
			nanokin.Arm2   = data.GetLimb(Arm2GUID)?.instance;
			nanokin.Level  = Level;
			nanokin.XP     = XP;
			nanokin.Points = Points;
			nanokin.Name = !string.IsNullOrEmpty(MonsterName)
				? MonsterName
				: asset != null
					? asset.Name
					: null;

			nanokin.RecalculateStats();

			foreach (StickerEntry sticker in StickerEntries)
			{
				sticker.UpdateInstance();
				nanokin.Stickers.Add(sticker.instance);
			}
		}

		public void ApplyInstance()
		{
			if (nanokin != null)
			{
				HeadGUID    = nanokin.Head?.entry?.GUID;
				BodyGUID    = nanokin.Body?.entry?.GUID;
				Arm1GUID    = nanokin.Arm1?.entry?.GUID;
				Arm2GUID    = nanokin.Arm2?.entry?.GUID;
				MonsterName = nanokin.Name;
				Level       = nanokin.Level;
				XP          = nanokin.XP;
				Points      = nanokin.Points;

				foreach (StickerEntry stickerEntry in StickerEntries)
				{
					stickerEntry.ApplyInstance();
				}
			}
		}
	}
}