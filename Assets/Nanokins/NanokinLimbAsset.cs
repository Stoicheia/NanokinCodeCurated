using System;
using System.Collections.Generic;
using System.Linq;
using Combat;
using Data.Combat;
using Data.Nanokin;
using Data.Shops;
using JetBrains.Annotations;
using Puppets.Assets;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Assets.Nanokins
{
	/// <summary>
	/// Asset definition for a nanokin limb asset.
	/// </summary>
	[Searchable]
	[BasePath("Assets/Resources/Limbs")]
	public class NanokinLimbAsset : ScriptableLimb
	{
		// LIMB PROPERTIES
		// ----------------------------------------
		[Title("General")]
		[SerializeField, Optional]
		public string DisplayName;

		[SerializeField]
		public LimbType Kind;



		[SerializeField, Inline, FoldoutGroup("Stats"), Title("Points")]
		private GrowingPoints GrowingPoints = new GrowingPoints();

		[SerializeField, Inline, FoldoutGroup("Stats"), Title("Stats")]
		private GrowingStats GrowingStats = new GrowingStats();

		[SerializeField, Inline, FoldoutGroup("Stats"), Title("Efficiencies")]
		private GrowingElements GrowingEfficiencies = new GrowingElements();

		public List<SkillAsset> Skills;

		[SerializeField, Optional]
		private RPCurveAsset RPCurveAsset;

		private List<GrowingCurvedValue> _curvedValues;
		[HideInInspector][SerializeField] private int _undoParity = 0; //hack for passing to undo

		[HideIf("@RPCurveAsset != null")]
		private int[] RPCurveCustom = { 100, 200, 350 };

		// BODY PROPERTIES
		// ----------------------------------------
		[Title("Body")]
		[ShowIf("IsBody")] public string MoveTargeter = "neighbors";
		[ShowIf("IsBody")] public string MoveAnimation = "leap";
		[ShowIf("IsBody")] public Color  Color1;
		[ShowIf("IsBody")] public Color  Color2;

		// HEAD PROPERTIES
		// ----------------------------------------
		[Title("Head")]
		[ShowIf("IsHead")] public AssetReferenceScreechSoundSet Sounds;
		[ShowIf("IsHead")] public AssetReferenceAudioClip Hurt;
		[ShowIf("IsHead")] public AssetReferenceAudioClip Screech;
		[ShowIf("IsHead")] public AssetReferenceAudioClip Death;
		[ShowIf("IsHead")] public AssetReferenceAudioClip Grunt;

		// RUNTIME
		// ----------------------------------------
		[NonSerialized]
		public NanokinAsset monster;

		/// <summary>
		/// Inherent drops for this nanokin. Note: battle recipes themselves may contain drop tables. This is additive.
		/// </summary>
		[ShowIf("IsBody")]
		public LootDropInfo Drops;

		//[ShowIf("IsBody")]
		//public Dictionary<string, LootDropInfo.TableInfo> DropTables;



		public int[] RPCurve => RPCurveAsset
			? RPCurveAsset.Curve
			: RPCurveCustom;

		/// <summary>
		/// Gets the rendering layer of this limb based on its kind.
		/// </summary>
		public override int Layer
		{
			get
			{
				switch (Kind)
				{
					case LimbType.Body:
						return 0;

					case LimbType.Head:
						return 1;

					case LimbType.Arm1:
						return 2;

					case LimbType.Arm2:
						return -1;

					default:
						return 999;
				}
			}
		}


		//protected override void OnValidate()
		//{
		//	Drops.DropTables = DropTables.Values.ToList();
		//}

		public void OnEnable()
		{
			_curvedValues = new List<GrowingCurvedValue>()
			{
				//GrowingEfficiencies.Astra,
				//GrowingEfficiencies.Blunt,
				//GrowingEfficiencies.Gaia,
				//GrowingEfficiencies.Oida,
				//GrowingEfficiencies.Pierce,
				//GrowingEfficiencies.Slash,
				GrowingPoints.HP,
				GrowingPoints.SP,
				GrowingStats.Power,
				GrowingStats.Speed,
				GrowingStats.Will
			};
			foreach (var curvedValue in _curvedValues)
			{
				curvedValue.OnValueChange += UpdateStatValues;
			}
		}

		public void OnDisable()
		{
			foreach (var curvedValue in _curvedValues)
			{
				curvedValue.OnValueChange -= UpdateStatValues;
			}
		}

		public Pointf CalcPoints(int level) =>
			new Pointf
			{
				hp = GrowingPoints.HP.Calculate(level, StatConstants.MAX_HP, StatConstants.MAX_LEVEL),
				sp = GrowingPoints.SP.Calculate(level, StatConstants.MAX_SP, StatConstants.MAX_LEVEL)
			};

		public Statf CalcStats(int level)
		{
			int ap = 0;

			if (Kind == LimbType.Body)
			{
				for (var i = 0; i < GrowingStats.AP.Length; i++)
				{
					int levelStep = GrowingStats.AP[i];
					if (level < levelStep)
						break;

					ap++;
				}
			}

			return new Statf
			{
				power = GrowingStats.Power.Calculate(level, StatConstants.MAX_STAT, StatConstants.MAX_LEVEL),
				speed = GrowingStats.Speed.Calculate(level, StatConstants.MAX_STAT, StatConstants.MAX_LEVEL),
				will  = GrowingStats.Will.Calculate(level, StatConstants.MAX_STAT, StatConstants.MAX_LEVEL),

				ap = ap
			};
		}

		public Elementf CalcEfficiency(int level) =>
			new Elementf
			{
				// Physical
				//blunt  = GrowingEfficiencies.Blunt.Calculate(level, StatConstants.MAX_EFFICIENCY, StatConstants.MAX_LEVEL),
				//slash  = GrowingEfficiencies.Slash.Calculate(level, StatConstants.MAX_EFFICIENCY, StatConstants.MAX_LEVEL),
				//pierce = GrowingEfficiencies.Pierce.Calculate(level, StatConstants.MAX_EFFICIENCY, StatConstants.MAX_LEVEL),
				blunt = GrowingEfficiencies.Blunt,
				slash = GrowingEfficiencies.Slash,
				pierce = GrowingEfficiencies.Pierce,
				// Magical
				//gaia  = GrowingEfficiencies.Gaia.Calculate(level, StatConstants.MAX_EFFICIENCY, StatConstants.MAX_LEVEL),
				//oida  = GrowingEfficiencies.Oida.Calculate(level, StatConstants.MAX_EFFICIENCY, StatConstants.MAX_LEVEL),
				//astra = GrowingEfficiencies.Astra.Calculate(level, StatConstants.MAX_EFFICIENCY, StatConstants.MAX_LEVEL)
				gaia = GrowingEfficiencies.Gaia,
				oida = GrowingEfficiencies.Oida,
				astra = GrowingEfficiencies.Astra
			};

		public string NanokinName
		{
			get
			{
				if (monster != null)
					return monster.name;

				return name;
			}
		}

		public string FullName
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(DisplayName)) return DisplayName;
				if (monster != null)
				{
					string monsterName = monster.name;

					if (!string.IsNullOrWhiteSpace(monster.DisplayName))
						monsterName = monster.DisplayName;

					return $"{monsterName}'s {Kind.ToGameName()}";
				}

				return name;
			}
		}

		public override string ToString() => name;

		// NOTE(CL): ???
		//#if UNITY_EDITOR

		[UsedImplicitly]
		public bool IsBody => Kind == LimbType.Body;

		[UsedImplicitly]
		public bool IsHead => Kind == LimbType.Head;

		private void UpdateStatValues()
		{
			#if UNITY_EDITOR
			UnityEditor.Undo.RecordObject(this, "Changed Stats");
			//_undoParity = 1-_undoParity;
			#endif
		}


		//#endif
	}
}