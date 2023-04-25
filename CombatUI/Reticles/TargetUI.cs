using System;
using System.Collections.Generic;
using Anjin.Util;
using Combat.Components;
using Combat.Data;
using Combat.Toolkit;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.UI
{
	public class TargetUI : StaticBoy<TargetUI>
	{
		private const string HEALTHBAR_ENABLE_FLAG_NAME = "target_ui";

		// Settings
		// ------------------------------------------------------------

		[FormerlySerializedAs("_canvas"), SerializeField]
		private RectTransform Canvas;

		[FormerlySerializedAs("_slotUI"), SerializeField]
		private SlotUI SlotUI;

		[FormerlySerializedAs("_pfbReticle"), SerializeField]
		private GameObject PfbReticle;

		[SerializeField]
		private Color ReticleHoverColorStart = Color.clear;

		[FormerlySerializedAs("_reticleHoverColor"), SerializeField]
		private Color ReticleHoverColor = Color.magenta;

		[SerializeField]
		private float ReticleHoverColorDamping = 2;

		[SerializeField]
		private float IrrelevantColorDamping = 2;

		public SelectUIStyle FighterSelectStyle;

		[Space]
		[SerializeField] private float IrrelevantSlotOpacity = 0.45f;
		[SerializeField] private float RelevantSlotOpacity          = 2.0f;
		[SerializeField] private float IrrelevantSlotOpacityPreview = 0.45f;
		[SerializeField] private float RelevantSlotOpacityPreview   = 2.0f;
		[SerializeField] private Color IrrelevantColor              = Color.grey;

		// State
		// ------------------------------------------------------------

		[NonSerialized]
		public static Battle battle;

		private static SelectUIManager                _reachman;
		private static Dictionary<Fighter, UIFighter> _fighters = new Dictionary<Fighter, UIFighter>(8);
		private static Dictionary<Slot, UISlot>       _slots    = new Dictionary<Slot, UISlot>(18);

		public class UIFighter
		{
			public Fighter        fighter;
			public Reticle        reticle;
			public FighterFX      reticlefx;
			public FighterFX      dimfx;
			public SelectUIObject reachable;

			public UIFighter(Fighter fighter)
			{
				this.fighter = fighter;

				var dimvfx     = new ManualVFX { fill = Live.IrrelevantColor };
				var reticlevfx = new ManualVFX { fill = Live.ReticleHoverColor };

				fighter.actor.vfx.Add(dimvfx);
				fighter.actor.vfx.Add(reticlevfx);

				fighter.coach?.actor.vfx.Add(dimvfx);

				dimfx = new FighterFX
				{
					fighter = fighter,
					vfx     = dimvfx,
					target  = 0
				};


				reticlefx = new FighterFX
				{
					fighter = fighter,
					vfx     = reticlevfx,
					target  = 0
				};
			}
		}

		public class UISlot
		{
			public Slot           slot;
			public Reticle        reticle;
			public SlotFX         reticlefx;
			public SlotFX         dimfx;
			public SelectUIObject reachable;

			public UISlot(Slot slot)
			{
				this.slot = slot;

				var dimvfx     = new ManualVFX { fill = Live.IrrelevantColor };
				var reticlevfx = new ManualVFX { fill = Live.ReticleHoverColor };

				slot.actor.vfx.Add(dimvfx);
				slot.actor.vfx.Add(reticlevfx);

				dimfx = new SlotFX
				{
					slot   = slot,
					vfx    = dimvfx,
					target = 0
				};


				reticlefx = new SlotFX
				{
					slot   = slot,
					vfx    = reticlevfx,
					target = 0
				};
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			battle = null;

			// _selected = null;
			_reachman = new SelectUIManager();
		}

		private static UIFighter GetUI([NotNull] Fighter fighter)
		{
			if (_fighters.TryGetValue(fighter, out UIFighter value))
				return value;

			return _fighters[fighter] = new UIFighter(fighter);
		}

		private static UISlot GetUI([NotNull] Slot slot)
		{
			if (_slots.TryGetValue(slot, out UISlot value))
				return value;

			return _slots[slot] = new UISlot(slot);
		}

		public static void Cleanup()
		{
			foreach (UIFighter fter in _fighters.Values)
			{
				fter.dimfx.vfx.removalStaged     = true;
				fter.reticlefx.vfx.removalStaged = true;
			}

			foreach (UISlot slot in _slots.Values)
			{
				slot.dimfx.vfx.removalStaged     = true;
				slot.reticlefx.vfx.removalStaged = true;
			}

			_fighters.Clear();
			_slots.Clear();
			battle = null;
		}

		public static void SetNone(bool overridePersistentHighlight = false)
		{
			UndoSelections(overridePersistentHighlight);
			UndoPreviews(overridePersistentHighlight);
		}

		public static void Confirm()
		{
			foreach (UIFighter ui in _fighters.Values)
			{
				if (ui.reticle != null)
				{
					ui.reticle.Disappear(true);
					ui.reticle = null;
				}

				Fighter fter = ui.fighter;
				if (fter != null)
				{
					HealthbarUI.EnableFlag(fter, HEALTHBAR_ENABLE_FLAG_NAME, false);
				}
			}

			foreach (UISlot slot in _slots.Values)
			{
				if (slot.reticle != null)
				{
					slot.reticle.Disappear(true);
					slot.reticle = null;
				}

				Fighter fter = slot.slot.owner;
				if (fter != null)
				{
					HealthbarUI.EnableFlag(fter, HEALTHBAR_ENABLE_FLAG_NAME, false);
				}
			}
		}

	#region Preview

		/// <summary>
		/// Displays the effective range.
		/// </summary>
		public static void SetPreview(Targeting targeting, int index, bool highlightFighters = true)
		{
			SetNone();
			SetEffectiveRange(targeting, index, highlightFighters);
		}

		private static void UndoPreviews(bool overridePersistentHighlight = false)
		{
			foreach (UIFighter fter in _fighters.Values)
			{
				SetReachable(fter.fighter, false);
			}

			foreach (Slot slot in battle.slots)
			{
				SetReachable(slot, false, true, overridePersistentHighlight);
			}
		}

	#endregion

	#region Selection

		public static void SetSelection(Targeting targeting, int index, [NotNull] Target selected)
		{
			// DespawnReticles(); // TODO tween the existing reticles to move instead of spawning new ones (only spawn if there's not enough)
			SetNone();
			SetEffectiveRange(targeting, index);
			AddSelection(selected);
		}

		public static void AddSelection(Target selected)
		{
			foreach (Slot slot in selected.slots)
			{
				SetSelected(slot);

				if (slot.actor != null)
				{
					SetReticle(slot, selected.showSlotReticle);
				}
			}

			foreach (Fighter fighter in selected.fighters)
			{
				if (fighter.HomeTargeting != null)
				{
					SetReachable(fighter.HomeTargeting);
				}

				if (fighter.actor != null)
				{
					SetReticle(fighter);
				}
			}
		}

		public static void UndoSelections(bool overridePersistentHighlight = false)
		{
			foreach (Slot slot in battle.slots)
			{
				if (!slot.persistentHighlight || overridePersistentHighlight)
				{
					SetSelected(slot, false);
					slot.persistentHighlight = false;
					//SlotUI.ChangeMaterial(slot, false);
				}
			}

			Confirm();
			UndoReticles();
		}


		public static void SetSelected(Slot slot, bool b = true)
		{
			SlotUI.SetHighlight(slot, b);
		}

	#endregion

	#region Effective Range

		private static void SetEffectiveRange(Targeting targeting, int index, bool highlightFighters = true)
		{
			foreach (Slot slot in battle.slots) SetReachable(slot, false);
			foreach (UIFighter ui in _fighters.Values) SetReachable(ui.fighter, false);

			List<EffectiveSlot> range   = targeting.range.SafeGet(index);
			List<Target>        options = targeting.options.SafeGet(index);

			// Do not highlight fighter slots when there are multiple targets
			if (options != null && options.Count > 0)
				highlightFighters = false;

			// Do not highlight fighter slots when the targeting has nothing to do with them
			if (highlightFighters && options != null)
			{
				var onlySlots = true;

				foreach (Target t in options)
				{
					if (t.fighters.Count > 0)
					{
						onlySlots = false;
						break;
					}
				}

				if (!onlySlots)
					highlightFighters = false;
			}

			if (range != null)
			{
				// Explicit range
				foreach (EffectiveSlot eslot in range)
				{
					Slot slot = eslot.slot;
					SetReachable(slot, highlightFighters: highlightFighters);
				}
			}

			// Options are also part of the range, naturally)
			if (options != null)
			{
				foreach (Target target in options)
				{
					AddEffectiveRange(target, options, highlightFighters);
				}
			}
		}

		private static void AddEffectiveRange(Target target, List<Target> options, bool highlightFighters)
		{
			foreach (Slot slot in target.slots)
			{
				SetReachable(slot, highlightFighters: highlightFighters);
			}

			// Automatically highlight slots for fighters
			foreach (Fighter fter in target.fighters)
			{
				if (fter.HomeTargeting != null)
				{
					SetReachable(fter.HomeTargeting, highlightFighters: highlightFighters);
					if (options.Count == 1)
					{
						SlotUI.SetHighlight(fter.HomeTargeting);
					}
				}
			}
		}

		private static void SetReachable(Slot slot, bool b = true, bool highlightFighters = true, bool overridePersistentHighlight = false)
		{
			SlotUI.SetOpacity(slot, b ? Live.RelevantSlotOpacityPreview : Live.IrrelevantSlotOpacityPreview);

			if (highlightFighters && slot.owner != null)
			{
				SlotUI.SetHighlight(slot, b, overridePersistentHighlight);
			}
		}

		public static void SetReachable(Fighter fighter, bool b)
		{
			UIFighter ui = GetUI(fighter);
			_reachman.Set(ref ui.reachable, b);
			ui.dimfx.target = b ? 0 : 1;
		}

	#endregion

	#region Reticle

		public static void SetReticle(Fighter fighter, bool enable = true)
		{
			if (fighter == null) return;

			UIFighter ui = GetUI(fighter);

			ui.reticlefx.target = enable ? 1 : 0;

			if (ui.reticle == null && enable)
			{
				// Spawn the reticle
				Reticle reticle = Live.PfbReticle.Instantiate<Reticle>(Live.transform);
				reticle.Raycast.SetCanvasRect(Live.Canvas);
				reticle.Raycast.SetWorldPos(fighter.actor.center);
				reticle.Appear();

				ui.reticle = reticle;
			}
			else if (ui.reticle != null && !enable)
			{
				// Remove the reticle
				ui.reticle.Disappear(false);
				ui.reticle = null;
			}

			//if (fighter.actor && (fighter.team != null))
			//{
			//	fighter.actor.ToggleHealthbarVisibility(!fighter.team.isPlayer ? b : false);
			//}

			if (fighter.team != null)
			{
				HealthbarUI.EnableFlag(fighter, HEALTHBAR_ENABLE_FLAG_NAME, enable);
			}
		}

		public static void SetReticle(Slot slot, bool enable = true)
		{
			if (slot == null) return;

			UISlot ui = GetUI(slot);

			ui.reticlefx.target = enable ? 1 : 0;

			if (ui.reticle == null && enable)
			{
				// Spawn the reticle
				Reticle reticle = Live.PfbReticle.Instantiate<Reticle>(Live.transform);

				reticle.Raycast.SetCanvasRect(Live.Canvas);
				reticle.Raycast.SetWorldPos(slot.actor.Position);

				reticle.Appear();

				Vector3 eulerAngles = reticle.transform.localEulerAngles;
				eulerAngles.x                      = -45;
				reticle.transform.localEulerAngles = eulerAngles;

				ui.reticle = reticle;
			}
			else if (ui.reticle != null && !enable)
			{
				// Remove the reticle
				ui.reticle.Disappear(false);
				ui.reticle = null;
			}

			Fighter owner = slot.owner;

			if (owner != null && owner.actor != null && owner.team != null)
			{
				HealthbarUI.EnableFlag(owner, HEALTHBAR_ENABLE_FLAG_NAME, enable);
			}
		}

		private static void UndoReticles()
		{
			foreach (UIFighter ui in _fighters.Values)
			{
				SetReticle(ui.fighter, false);
			}

			foreach (UISlot ui in _slots.Values)
			{
				SetReticle(ui.slot, false);
			}
		}

	#endregion

		private void Update()
		{
			foreach (UIFighter fter in _fighters.Values)
			{
				var dimTarget     = Color.Lerp(Color.clear, IrrelevantColor, fter.reachable.brightness);
				var reticleTarget = Color.Lerp(Color.clear, ReticleHoverColor, fter.reticlefx.target);

				fter.dimfx.vfx.fill     = fter.dimfx.vfx.fill.LerpDamp(dimTarget, IrrelevantColorDamping);
				fter.reticlefx.vfx.fill = fter.reticlefx.vfx.fill.LerpDamp(reticleTarget, ReticleHoverColorDamping);

				_reachman.Update(ref fter.reachable, ref FighterSelectStyle);
			}

			foreach (UISlot slot in _slots.Values)
			{
				var dimTarget     = Color.Lerp(Color.clear, IrrelevantColor, slot.reachable.brightness);
				var reticleTarget = Color.Lerp(Color.clear, ReticleHoverColor, slot.reticlefx.target);

				slot.dimfx.vfx.fill     = slot.dimfx.vfx.fill.LerpDamp(dimTarget, IrrelevantColorDamping);
				slot.reticlefx.vfx.fill = slot.reticlefx.vfx.fill.LerpDamp(reticleTarget, ReticleHoverColorDamping);
			}
		}

		public struct FighterFX
		{
			public Fighter   fighter;
			public ManualVFX vfx;
			public float     target;
		}

		public struct SlotFX
		{
			public Slot      slot;
			public ManualVFX vfx;
			public float     target;
		}
	}
}