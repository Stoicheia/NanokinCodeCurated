using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Editor;
using Anjin.Nanokin.ParkAI;
using Anjin.Scripting;
using Anjin.Util;
using API.Spritesheet.Indexing;
using API.Spritesheet.Indexing.Runtime;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Rendering;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityUtilities;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

#endif

namespace Anjin.Actors
{
	public class ActorDesigner : MonoBehaviour
	{
		public enum Modes
		{
			Whole,
			BodyHead
		}

		[DisableInPlayMode]
		[OnValueChanged("OnValueChanged")]
		[ToggleButton(ButtonSizes.Large)]
		[GUIColor("BakeButtonColor")]
		[LabelText("@BakeButtonText")]
		[PropertyOrder(10)]
		[PropertySpace]
		[HideInPrefabAssets]
		public bool Bake;

		[Space]
		[HideLabel, EnumToggleButtons]
		[OnValueChanged("OnValueChanged")]
		[DisableInPlayMode]
		public Modes Mode;

		// ----------------------------------------

		[BoxGroup]
		[ShowIf("IsWhole")]
		[Space]
		[DisableInPlayMode]
		[OnValueChanged("OnValueChanged", true)]
		public List<IndexedSpritesheetAsset> Sheets;

		[BoxGroup]
		[ShowIf("IsWhole")]
		[Optional, CanBeNull]
		[DisableInPlayMode]
		[OnValueChanged("OnValueChanged", true)]
		public ColorReplacementProfile Color;

		[BoxGroup]
		[ShowIf("IsWhole")]
		[Optional, CanBeNull]
		[DisableInPlayMode]
		[OnValueChanged("OnValueChanged", true)]
		public PeepDef? PeepDefinition;
		// ----------------------------------------
		// @formatter:off

		[Space]
		[BoxGroup, OnValueChanged("OnValueChanged"), Delayed, ShowIf("IsBodyHead"), Required, DisableInPlayMode] public IndexedSpritesheetAsset Head;
		[BoxGroup, OnValueChanged("OnValueChanged"), Delayed, ShowIf("IsBodyHead"), Required, DisableInPlayMode] public IndexedSpritesheetAsset Body;
		[BoxGroup, OnValueChanged("OnValueChanged"), Delayed, ShowIf("IsBodyHead"), Required, DisableInPlayMode] public ColorReplacementProfile SkinColor;
		[BoxGroup, OnValueChanged("OnValueChanged"), Delayed, ShowIf("IsBodyHead"), Required, DisableInPlayMode] public ColorReplacementProfile HairColor;

		// These two don't seem necessary anymore? Zeroing both positions lines them up properly. Not sure what's going on

	#region OLD

		[BoxGroup, OnValueChanged("OnValueChanged"), Delayed, ShowIf("IsBodyHead"), DisableInPlayMode] public float HeadYOffset;
		[BoxGroup, OnValueChanged("OnValueChanged"), Delayed, ShowIf("IsBodyHead"), DisableInPlayMode] public bool  AddBaseOffset = true;

	#endregion

		// @formatter:on
		// ----------------------------------------

		private CharacterRig  _rig;
		private ActorRenderer _renderer;

		[DisableInPlayMode]
		[OnValueChanged("OnValueChanged")]
		public CharacterRig Rig
		{
			get
			{
				if (_rig == null) _rig = GetComponentInChildren<CharacterRig>(true);

				return _rig;
			}
		}

		/*[DisableInPlayMode]
		[OnValueChanged("OnValueChanged")]
		public CharacterRenderer Renderer
		{
			get
			{
				if (_renderer == null) _renderer = GetComponentInChildren<CharacterRenderer>(true);

				return _renderer;
			}
		}*/

		/// <summary>
		/// Write the parts into the rig for the
		/// design parameters configured on this component.
		/// Does not handle anything related to baking or spawning.
		/// </summary>
		public void WriteParts()
		{
			Rig.Parts.Clear();

			switch (Mode)
			{
				case Modes.Whole:
					Rig.Parts.Add(new CharacterRig.Part
					{
						Name   = "character",
						Sheets = Sheets,
						Colors = Color != null ? new[] { Color } : new ColorReplacementProfile[0]
					});
					break;

				case Modes.BodyHead:
					// 	Parts.Add(new CharacterRig.Part
					// 	{
					// 		Name   = "body",
					// 		Sheets = ani.BodySheets.ToArray(),
					// 		Colors = new[] {ani.SkinProfile}
					// 	});
					//
					// 	Parts.Add(new CharacterRig.Part
					// 	{
					// 		Name       = "head",
					// 		Sheets     = ani.HeadSheets.ToArray(),
					// 		Colors     = new[] {ani.SkinProfile, ani.HairProfile},
					// 		Offset     = Vector3.up * ani.HeadYOffset + Vector3.forward * 0.01f,
					// 		BaseOffset = ani.AddBaseOffset ? new Vector3(0, 0.65f, 0) : Vector3.zero
					// 	});

					Rig.Parts.Add(new CharacterRig.Part
					{
						Name   = "body",
						Sheets = new List<IndexedSpritesheetAsset> { Body },
						Colors = new[] { SkinColor } // very important note: does not support ruff chest hair
					});

					Rig.Parts.Add(new CharacterRig.Part
					{
						Name   = "head",
						Offset = new Vector3(0, GetHeadOffsetY(), -0.01f),
						Sheets = new List<IndexedSpritesheetAsset> { Head },
						Colors = new[] { SkinColor, HairColor }
					});

					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public float GetHeadOffsetY()
		{
			if (PeepDefinition.HasValue) {
				var def = PeepDefinition.Value;
				if (def.HeadAccessory != PeepAccessory.None)
				{
					if (def.Type     == PeepType.Child)		return 0.06f;
					if (def.BodyType == PeepBodyType.Round) return .2f;

					return .125f;
				}
			}

			return 0;
		}

		public void SetRandomPeep()
		{
			var (body, head, peep) = PeepGenerator.MakePeep();

			Mode      = Modes.BodyHead;
			Body      = body;
			Head      = head;
			HairColor = GameAssets.Live.PeepSpriteDatabase.HairProfiles.Choose();
			SkinColor = GameAssets.Live.PeepSpriteDatabase.SkinProfiles.Choose();

			PeepDefinition = peep;
		}

		public async void SetSpritesUsingTable(Table table)
		{
			PeepDefinition = null;

			PeepSpriteDatabase db = PeepGenerator.Database;

			IndexedSpritesheetAsset head_sheet   = Head;
			IndexedSpritesheetAsset body_sheet   = Body;
			ColorReplacementProfile skin_profile = SkinColor;
			ColorReplacementProfile hair_profile = HairColor;

			Modes _mode = Modes.BodyHead;

			void _rand_skin()
			{
				skin_profile = db.SkinProfiles.Choose();
			}

			void _rand_hair()
			{
				hair_profile = db.HairProfiles.Choose();
			}

			void _set_skin(DynValue val)
			{
				if (val.Type == DataType.String)
				{
					for (var i = 0; i < db.SkinProfiles.Count; i++)
					{
						if (db.SkinProfiles[i].ScriptID != val.String) continue;
						skin_profile = db.SkinProfiles[i];
						break;
					}
				}
				else if (val.Type == DataType.UserData && val.UserData.TryGet(out PeepRace race))
				{
					for (var i = 0; i < db.SkinProfiles.Count; i++)
					{
						if (db.SkinProfiles[i].Race != race) continue;
						skin_profile = db.SkinProfiles[i];
						break;
					}
				}
			}

			async UniTask _set_address(string sheet_address)
			{
				IndexedSpritesheetAsset sheet = await GameAssets.LoadAsset<IndexedSpritesheetAsset>(sheet_address);
				if (sheet != null) {
					_mode  = Modes.Whole;
					Sheets = new List<IndexedSpritesheetAsset> { sheet };
				}
			}

			void _set(Table _table)
			{
				PeepGender gender = Random.value > 0.5 ? PeepGender.Male : PeepGender.Female;
				PeepType   type   = Random.value > 0.5 ? PeepType.Adult : PeepType.Child;

				_table.TryGet("type", out type);
				_table.TryGet("gender", out gender);

				/*{
					if (_table.TryGet("gender", out PeepGender gender)) {
						PeepSpriteDatabaseEntry head = db.Heads.Filter(x => x.Type  == type).RandomElement(rand);
						PeepSpriteDatabaseEntry body = db.Bodies.Filter(x => x.Type == type).RandomElement(rand);

						if (head != null) head_sheet = head.sequencer;
						if (body != null) body_sheet = body.sequencer;
					}
				}

				{
					if (_table.TryGet("type", out PeepType type)) {
						PeepSpriteDatabaseEntry head = db.Heads.Filter(x => x.Type  == type).RandomElement(rand);
						PeepSpriteDatabaseEntry body = db.Bodies.Filter(x => x.Type == type).RandomElement(rand);

						if (head != null) head_sheet = head.sequencer;
						if (body != null) body_sheet = body.sequencer;
					}
				}*/

				{
					bool _filter(PeepSpriteDatabaseEntry x)
					{
						return x.Type == type && x.Gender == gender;
					}

					PeepSpriteDatabaseEntry head = db.Heads.Filter(_filter).Choose();
					PeepSpriteDatabaseEntry body = db.Bodies.Filter(_filter).Choose();

					if (head != null) head_sheet = head.sequencer;
					if (body != null) body_sheet = body.sequencer;
				}

				{
					if (_table.TryGet("head_sheet", out string head))
					{
						for (var i = 0; i < db.Heads.Count; i++)
						{
							if (db.Heads[i].Name != head) continue;
							head_sheet = db.Heads[i].sequencer;
							break;
						}
					}

					if (_table.TryGet("body_sheet", out string body))
					{
						for (var i = 0; i < db.Bodies.Count; i++)
						{
							if (db.Bodies[i].Name != body) continue;
							body_sheet = db.Bodies[i].sequencer;
							break;
						}
					}
				}

				if (_table.TryGet("race", out DynValue val1))
				{
					_set_skin(val1);
				}
				else if (_table.TryGet("skin", out DynValue val2))
				{
					_set_skin(val2);
				}
				else
				{
					_rand_skin();
				}

				if (_table.TryGet("hair", out string hair))
				{
					for (var i = 0; i < db.HairProfiles.Count; i++)
					{
						if (db.HairProfiles[i].ScriptID != hair) continue;
						hair_profile = db.HairProfiles[i];
						break;
					}
				}
				else
				{
					_rand_hair();
				}
			}


			if (table.TryGet("appearance", out DynValue appearance)) {
				if(appearance.AsTable(out Table tbl))
					_set(tbl);
				if(appearance.AsString(out string address))
					await _set_address(address);

			} else
				_set(table);

			bool any_changed = false;

			if (head_sheet != null)
			{
				Head        = head_sheet;
				any_changed = true;
			}

			if (body_sheet != null)
			{
				Body        = body_sheet;
				any_changed = true;
			}

			if (skin_profile != null)
			{
				SkinColor   = skin_profile;
				any_changed = true;
			}

			if (hair_profile != null)
			{
				HairColor   = hair_profile;
				any_changed = true;
			}

			if (any_changed)
			{
				Mode = _mode;
				Rig.Despawn();
				WriteParts();
				Rig.Despawn();
				Rig.Spawn();
			}
		}

#if UNITY_EDITOR
		private Color  BakeButtonColor => Bake ? ColorsXNA.PaleGreen.Alpha(0.85f) : ColorsXNA.Orange.Alpha(0.85f);
		private string BakeButtonText  => Bake ? "Baked" : "Bake";

		[UsedImplicitly]
		private bool IsWhole => Mode == Modes.Whole;

		[UsedImplicitly]
		private bool IsBodyHead => Mode == Modes.BodyHead;

		[LabelText("Random Peep")]
		[Button]
		[HideInPlayMode]
		[UsedImplicitly]
		[PropertyOrder(15)]
		private void SetRandomPeepEditor()
		{
			Undo.RecordObject(this, $"{nameof(ActorDesigner)}.{nameof(SetRandomPeepEditor)}");

			(var bodySheet, var headSheet, PeepDef peep) = PeepGenerator.MakePeep();

			Bake           = true;
			Mode           = Modes.BodyHead;
			Body           = bodySheet;
			Head           = headSheet;
			SkinColor      = PeepGenerator.Database.SkinProfiles.Choose();
			HairColor      = PeepGenerator.Database.HairProfiles.Choose();

			PeepDefinition = peep;

			Rig.Bake(false);
			WriteParts();
			Rig.Bake(true);

			PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
			PrefabUtility.RecordPrefabInstancePropertyModifications(this);
			PrefabUtility.RecordPrefabInstancePropertyModifications(Rig.gameObject);
			PrefabUtility.RecordPrefabInstancePropertyModifications(Rig);

			EditorSceneManager.MarkSceneDirty(gameObject.scene);
		}

		[Button]
		[UsedImplicitly]
		[HideInPlayMode]
		[PropertyOrder(20)]
		[HideInEditorMode]
		public void MigrateFromAnimable() // TODO remove me
		{
			SpriteAnim[] anis = GetComponentsInChildren<SpriteAnim>().Where(a => a.BodySheets?.Count > 0).ToArray();
			if (anis.Length == 0)
			{
				Debug.LogError($"No animable found for {name}!", gameObject);
				return;
			}

			SpriteAnim     ani = anis.First();
			SpriteRenderer sr  = GetComponentInChildren<SpriteRenderer>();

			// ReSharper disable once RedundantCheckBeforeAssignment
			if (Rig.Material != sr.sharedMaterial)
			{
				Rig.Material = sr.sharedMaterial;
			}

			Rig.Bake(false);
			Rig.Clear();

			PeepDefinition = null;
			Mode           = ani.Mode;
			Sheets         = ani.Spritesheets;
			Head           = ani.HeadSheets[0];
			Body           = ani.BodySheets[0];
			HairColor      = ani.HairProfile;
			HeadYOffset    = ani.HeadYOffset;
			AddBaseOffset  = ani.AddBaseOffset;

			if (Mode == Modes.Whole) Color = ani.SkinProfile;
			else SkinColor                 = ani.SkinProfile;

			WriteParts();
			Rig.Bake(true);

			PrefabUtility.RecordPrefabInstancePropertyModifications(gameObject);
			PrefabUtility.RecordPrefabInstancePropertyModifications(this);
			PrefabUtility.RecordPrefabInstancePropertyModifications(Rig.gameObject);
			PrefabUtility.RecordPrefabInstancePropertyModifications(Rig);
		}

		private void OnValidate()
		{
			if (!gameObject.scene.IsValid() || PrefabStageUtility.GetCurrentPrefabStage() != null) return;
			if (Bake && !Rig.Baked)
			{
				EditorApplication.delayCall += () =>
				{
					if (Application.isPlaying) return;

					Rig.Despawn();
					Rig.Bake(true);
					UnityEditor.EditorUtility.SetDirty(this);
				};
			}
		}

		private void OnValueChanged()
		{
			if (Rig == null) return;

			Rig.Despawn();
			WriteParts();

			if (Bake)
			{
				if (PrefabStageUtility.GetCurrentPrefabStage() != null) return;

				if (Rig.Root && Rig.Root.TryGetComponent(out SpriteRenderer placeholder))
					placeholder.enabled = false;

				Rig.Bake(Bake);

				// Set the sprite on the SpriteRenderer when in editor
				// that way the preview matches the first spritesheet.
				if (!Application.isPlaying)
				{
					SpriteRenderer sr = Rig.Root.GetComponent<SpriteRenderer>();
					if (sr != null)
					{
						if (Mode == Modes.Whole)
						{
							sr.enabled = true;
							sr.sprite = Sheets.Count > 0
								? Sheets.FirstOrDefault()?.spritesheet?.Spritesheet?[0, 0].Sprite
								: null;
						}
						else
						{
							sr.enabled = false;
						}
					}
				}
			}
			else
			{
				if (Mode == Modes.Whole && Sheets.Count > 0 && Sheets[0] != null)
				{
					if (Rig.Root && Rig.Root.TryGetComponent(out SpriteRenderer placeholder))
					{
						placeholder.sprite  = Sheets[0].spritesheet._spritesheet[0].Sprite;
						placeholder.enabled = true;
					}
				}
			}

			UnityEditor.EditorUtility.SetDirty(gameObject);
		}
#endif
		public void Apply()
		{
			Rig.Despawn();
			WriteParts();
			Rig.Spawn();
		}
	}
}