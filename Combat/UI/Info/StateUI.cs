using System;
using System.Collections.Generic;
using Anjin.Util;
using Combat.Data;
using Combat.Data.VFXs;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;
using Util.Components.UI;
using Util.Extensions;

namespace Combat.UI.Info
{
	public class StateUI : StaticBoy<StateUI>
	{
		public Transform NotificationRoot;
		public Transform IconRoot;
		public float     StayDuration = 2.5f;
		[Range(-4, 4)]
		public float VerticalPositioning = 1;
		[Range(-4, 4)]
		public float HorizontalPositioning = 0;

		[Title("Prefabs")]
		public GameObject BuffIconPrefab;
		public GameObject DebuffIconPrefab;
		[FormerlySerializedAs("Panel_Buff")]
		public GameObject BuffPanelPrefab;
		[FormerlySerializedAs("Panel_Debuff")]
		public GameObject DebuffPanelPrefab;


		[Title("VFX")]
		public Color VFXColorBuff = Color.cyan;
		public Color VFXColorDebuff      = Color.magenta;
		public float VFXColorDamping     = 4.5f;
		public Color VFXFlashColorBuff   = Color.white;
		public Color VFXFlashColorDebuff = Color.white;
		public float VFXFlashDuration    = 0.07f;

		[Title("Icons (states)")]
		public Sprite Icon_Buff_Mask;
		public Sprite Icon_Buff_Lvl;

		public Sprite Icon_Buff_HP;
		public Sprite Icon_Buff_SP;

		public Sprite Icon_Buff_Power;
		public Sprite Icon_Buff_Speed;
		public Sprite Icon_Buff_Will;

		public Sprite Icon_Buff_Blunt;
		public Sprite Icon_Buff_Slash;
		public Sprite Icon_Buff_Pierce;
		public Sprite Icon_Buff_Gaia;
		public Sprite Icon_Buff_Astra;
		public Sprite Icon_Buff_Oida;

		public Sprite Icon_Buff_HpRegen;
		public Sprite Icon_Buff_SpRegen;
		public Sprite Icon_Buff_OpRegen;

		public Sprite Icon_Buff_Magic;
		public Sprite Icon_Buff_Physical;


		[Title("Icons (debuffs)")]
		public Sprite Icon_Debuff_Mask;
		public Sprite Icon_Debuff_Lvl;

		public Sprite Icon_Debuff_HP;
		public Sprite Icon_Debuff_SP;

		public Sprite Icon_Debuff_Power;
		public Sprite Icon_Debuff_Speed;
		public Sprite Icon_Debuff_Will;

		public Sprite Icon_Debuff_Blunt;
		public Sprite Icon_Debuff_Slash;
		public Sprite Icon_Debuff_Pierce;
		public Sprite Icon_Debuff_Gaia;
		public Sprite Icon_Debuff_Astra;
		public Sprite Icon_Debuff_Oida;

		public Sprite Icon_Debuff_HpRegen;
		public Sprite Icon_Debuff_SpRegen;
		public Sprite Icon_Debuff_OpRegen;

		public Sprite Icon_Debuff_Magic;
		public Sprite Icon_Debuff_Physical;

		// STATUS
		// ----------------------------------------
		[Title("Icons (status)")]
		public Sprite Icon_Status_Dot;
		public Sprite Icon_Status_Counter;
		public Sprite Icon_Status_Shield;
		public Sprite Icon_Status_Brainfog;
		public Sprite Icon_Status_Corrupt;
		public Sprite Icon_Status_Lag;
		public Sprite Icon_Status_Lock;
		public Sprite Icon_Status_Mark;
		public Sprite Icon_Status_Root;
		public Sprite Icon_Status_Spasm;

		private static List<Notif> _notifications = new List<Notif>();
		private static Func<bool>  _funcNotificationsEmpty;

		public struct Notif
		{
			public State             state;
			public IStatee           statee;
			public StateNotification notif;
		}

		public struct Icon
		{
			public State     state;
			public IStatee   statee;
			public StateIcon icon;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			_funcNotificationsEmpty = () => _notifications.Count == 0;
		}

		public static void Clear()
		{
			foreach (Notif notif in _notifications)
			{
				if (notif.notif)
					Destroy(notif.notif.gameObject);
			}

			_notifications.Clear();
		}

		public static UniTask AwaitNotifications()
		{
			return UniTask.WaitUntil(_funcNotificationsEmpty);
		}

		public static bool GetNotif(IStatee statee, State state, out StateNotification notif)
		{
			StateInfo info = GetInfo(state);
			if (!info.invalid)
			{
				return FindNotif(statee, state, out notif) || SpawnNotif(statee, state, ref notif, info);
			}
			else
			{
				Live.LogError($"Could not determine how to display state {state}, it will not be displayed.");
				notif = null;
				return false;
			}
		}

		private static bool FindNotif(IStatee statee, State state, out StateNotification notif)
		{
			notif = null;

			foreach (Notif buf in _notifications)
			{
				if (buf.statee == statee && buf.state == state)
				{
					notif = buf.notif;
					return true;
				}
			}

			return false;
		}

		private static bool SpawnNotif(IStatee statee, State state, ref StateNotification notif, StateInfo info)
		{
			if (statee.Actor == null) return false;
			if (info.icon == null) return false;
			if (info.title == null) info.title     = state.ID;
			if (info.subtext == null) info.subtext = GetSubtext(state);

			GameObject obj = Instantiate(info.notificationPrefab, Live.NotificationRoot);

			// SETUP RAYCAST
			WorldToCanvasRaycast raycast = obj.GetComponent<WorldToCanvasRaycast>();
			raycast.SetWorldPos(statee.Actor.transform);
			raycast.CanvasRaycastOffset = (
				Vector2.up * statee.Actor.height * Live.VerticalPositioning +
				Vector2.right * statee.Actor.radius * Live.HorizontalPositioning
			) * MathUtil.WORLD_TO_PIXEL;

			// SETUP NOTIFICATION
			notif              = obj.GetComponent<StateNotification>();
			notif.StayDuration = Live.StayDuration;
			notif.Set(info);

			// REGISTER NOTIF
			_notifications.Add(new Notif
			{
				state  = state,
				statee = statee,
				notif  = notif
			});

			// REGISTER VFX
			statee.Actor.vfx.Add(info.vfx);
			return true;
		}

		/// <summary>
		/// Add a state and notify the player.
		/// </summary>
		public static void NotifyAdd(IStatee statee, State state)
		{
			if (!state.enableUI) return;

			if (GetNotif(statee, state, out StateNotification notif))
			{
				if (notif.Subtext.Text == "")
					notif.Subtext.Text = "Activated!"; // TODO this is temp since we don't have life/decay comprehension yet

				notif.Animator.PlayClip(notif.AnimEnter);
			}
		}

		/// <summary>
		/// Remvoe a state and notify the player.
		/// </summary>
		/// <param name="buffe></param>
		public static void NotifyExpire(IStatee statee, State state)
		{
			if (!state.enableUI) return;
			if (GetNotif(statee, state, out StateNotification notif))
			{
				notif.Animator.PlayClip(notif.AnimExpire);
			}
		}

		/// <summary>
		/// Notify that the state has triggered its effect.
		/// </summary>
		/// <param name="buffe></param>
		public static void NotifyEffect(IStatee statee, State state)
		{
			if (!state.enableUI) return;
			if (GetNotif(statee, state, out StateNotification notif))
			{
				notif.Animator.PlayClip(notif.AnimEffect);
			}
		}

		/// <summary>
		/// Notify that the state has been consumed. (animate)
		/// </summary>
		/// <param name="buffe></param>
		public static void NotifyRefresh(IStatee statee, State state)
		{
			if (!state.enableUI) return;
			if (GetNotif(statee, state, out StateNotification notif))
			{
				notif.Animator.PlayClip(notif.AnimEffect);
				notif.Subtext.Text = "";
			}
		}

		/// <summary>
		/// Notify that the state has been consumed. (animate)
		/// </summary>
		/// <param name="buffe></param>
		public static void NotifyConsume(IStatee statee, State state)
		{
			if (!state.enableUI) return;
			if (GetNotif(statee, state, out StateNotification notif))
			{
				notif.Animator.PlayClip(notif.AnimConsume);
			}
		}

		public static void Remove(StateNotification bnotif)
		{
			for (var i = 0; i < _notifications.Count; i++)
			{
				Notif notif = _notifications[i];
				if (notif.notif == bnotif)
				{
					_notifications.RemoveAt(i);
					Destroy(bnotif.gameObject);
					return;
				}
			}
		}

		[CanBeNull]
		public static StateInfo GetInfo([NotNull] State state)
		{
			StateInfo info = GetSpriteInfo(state);
			info.subtext = GetBuffSubtext(state);

			return info;
		}

		[NotNull]
		public static string GetBuffSubtext([NotNull] State state)
		{
			var subtext = "";

			if (state.life == 0)
			{
				subtext = $"Expired!";
			}
			else if (state.life > -1)
			{
				subtext = $"{state.life} turns left"; // TODO THE CAKE IS A LIE!! We need proper life/decay comprehension with the trigger, it's not necessarily gonna be turns
			}

			return subtext;
		}

		[CanBeNull]
		public static StateInfo GetSpriteInfo([NotNull] State state)
		{
			Status          stats = state.status;
			List<StateCmd>  cmds  = stats.cmds;
			List<StateFunc> funcs = stats.funcs;

			// BY FUNC
			// ----------------------------------------
			for (var i = 0; i < funcs.Count; i++)
			{
				StateFunc func = funcs[i];
				StateStat stat = func.stat;
				StatOp    op   = func.op;


				if (stat == StateStat.skill_usable)
					return GetStatus(Live.Icon_Status_Lock);

				if (stat.IsTargetOptions())
					return GetStatus(Live.Icon_Status_Spasm);

				switch (op)
				{
					case StatOp.up:
					case StatOp.low:
					case StatOp.scale:
						if (state.tags.Contains("good") || op == StatOp.up)
						{
							return GetBuff(
								GetBuffIcon(stat),
								GetBuffTitle(state));
						}
						else if (state.tags.Contains("bad") || op == StatOp.low)
						{
							return GetDebuff(
								GetDebuffIcon(stat),
								GetDebuffTitle(state));
						}

						break;
				}
			}


			// BY TAG
			// ----------------------------------------
			foreach (string t in state.tags)
			{
				switch (t)
				{
					case "dot":     return GetStatus(Live.Icon_Status_Dot);
					case "counter": return GetStatus(Live.Icon_Status_Counter);
					case "shield":  return GetStatus(Live.Icon_Status_Shield);
					case "mark":    return GetStatus(Live.Icon_Status_Mark);
					case "regen":
					{
						foreach (string t2 in state.tags)
						{
							switch (t2)
							{
								case "hp": return GetBuff(Live.Icon_Buff_HpRegen);
								case "sp": return GetBuff(Live.Icon_Buff_SpRegen);
								case "op": return GetBuff(Live.Icon_Buff_OpRegen);
							}
						}

						break;
					}
				}
			}


			// BY ID
			// ----------------------------------------
			switch (state.ID)
			{
				case "root":     return GetStatus(Live.Icon_Status_Root, "Root");
				case "corrupt":  return GetStatus(Live.Icon_Status_Corrupt, "Corrupt");
				case "brainfog": return GetStatus(Live.Icon_Status_Brainfog, "Brainfog");
				case "lag":      return GetStatus(Live.Icon_Status_Lag, "Lag");
			}


			// BY EFLAG
			// ----------------------------------------
			if (stats.engineFlags.Contains(EngineFlags.unmovable))
				return GetStatus(Live.Icon_Status_Root);
			else if (stats.engineFlags.Contains(EngineFlags.lock_formation))
				return GetStatus(Live.Icon_Status_Root);


			// BY STATIC COMMANDS (fallback good/bad auto-detection)
			// -------------------------------------------------------
			for (var i = 0; i < cmds.Count; i++)
			{
				StateCmd cmd = cmds[i];
				if (cmd.Raises)
				{
					return GetBuff(GetBuffIcon(cmd.stat), GetBuffTitle(state));
				}
				else if (cmd.Lowers)
				{
					return GetDebuff(GetDebuffIcon(cmd.stat), GetDebuffTitle(state));
				}
			}


			// BY TRIGGER (CONDITIONAL STATE)
			// -------------------------------------------------------
			if (state.triggers.Count > 0)
			{
				if (state.tags.Contains("good"))
					return GetBuff(GetBuffIcon(StateStat.power), GetBuffTitle(state));
				else if (state.tags.Contains("bad"))
					return GetDebuff(GetDebuffIcon(StateStat.power), GetDebuffTitle(state));
			}

			return new StateInfo { invalid = true };
		}

		public static string GetSubtext([NotNull] State state)
		{
			return $"{state.maxlife} Turns";
		}

		public static string GetBuffTitle([NotNull] State state)
		{
			return state.ID;
		}

		public static string GetDebuffTitle([NotNull] State state)
		{
			return state.ID;
		}

		public static Sprite GetBuffIcon(StateStat stat)
		{
			// value state
			switch (stat)
			{
				case StateStat.lvl:   return Live.Icon_Buff_Lvl;
				case StateStat.hp:    return Live.Icon_Buff_HP;
				case StateStat.sp:    return Live.Icon_Buff_SP;
				case StateStat.power: return Live.Icon_Buff_Power;
				case StateStat.speed: return Live.Icon_Buff_Speed;
				case StateStat.will:  return Live.Icon_Buff_Will;

				case StateStat.atk_blunt:
				case StateStat.res_blunt:
				case StateStat.def_blunt:
					return Live.Icon_Buff_Blunt;

				case StateStat.atk_slash:
				case StateStat.res_slash:
				case StateStat.def_slash:
					return Live.Icon_Buff_Slash;

				case StateStat.atk_pierce:
				case StateStat.res_pierce:
				case StateStat.def_pierce:
					return Live.Icon_Buff_Pierce;

				case StateStat.atk_gaia:
				case StateStat.res_gaia:
				case StateStat.def_gaia:
					return Live.Icon_Buff_Gaia;

				case StateStat.atk_astra:
				case StateStat.res_astra:
				case StateStat.def_astra:
					return Live.Icon_Buff_Astra;

				case StateStat.atk_oida:
				case StateStat.res_oida:
				case StateStat.def_oida:
					return Live.Icon_Buff_Oida;

				case StateStat.use_cost:
				case StateStat.skill_cost:
				case StateStat.sticker_cost:
					return Live.Icon_Status_Brainfog;
			}

			return null;
		}

		private static Sprite GetDebuffIcon(StateStat stat)
		{
			// value state
			switch (stat)
			{
				case StateStat.lvl:   return Live.Icon_Debuff_Lvl;
				case StateStat.hp:    return Live.Icon_Debuff_HP;
				case StateStat.sp:    return Live.Icon_Debuff_SP;
				case StateStat.power: return Live.Icon_Debuff_Power;
				case StateStat.speed: return Live.Icon_Debuff_Speed;
				case StateStat.will:  return Live.Icon_Debuff_Will;

				case StateStat.atk_blunt:
				case StateStat.res_blunt:
				case StateStat.def_blunt:
					return Live.Icon_Debuff_Blunt;

				case StateStat.atk_slash:
				case StateStat.res_slash:
				case StateStat.def_slash:
					return Live.Icon_Debuff_Slash;

				case StateStat.atk_pierce:
				case StateStat.res_pierce:
				case StateStat.def_pierce:
					return Live.Icon_Debuff_Pierce;

				case StateStat.atk_gaia:
				case StateStat.res_gaia:
				case StateStat.def_gaia:
					return Live.Icon_Debuff_Gaia;

				case StateStat.atk_astra:
				case StateStat.res_astra:
				case StateStat.def_astra:
					return Live.Icon_Debuff_Astra;

				case StateStat.atk_oida:
				case StateStat.res_oida:
				case StateStat.def_oida:
					return Live.Icon_Debuff_Oida;

				case StateStat.use_cost:
				case StateStat.skill_cost:
				case StateStat.sticker_cost:
					return Live.Icon_Status_Brainfog;
			}

			return null;
		}

		public static StateInfo GetBuff(Sprite icon, string title = null)
		{
			return new StateInfo
			{
				icon               = icon,
				icon_mask          = Live.Icon_Buff_Mask,
				notificationPrefab = Live.BuffPanelPrefab,
				vfx                = new StateFlashVFX(Live.VFXColorBuff, Live.VFXColorDamping, Live.VFXFlashColorBuff, Live.VFXFlashDuration)
			};
		}

		public static StateInfo GetDebuff(Sprite icon, string title = null)
		{
			return new StateInfo
			{
				icon               = icon,
				icon_mask          = Live.Icon_Debuff_Mask,
				notificationPrefab = Live.DebuffPanelPrefab,
				vfx                = new StateFlashVFX(Live.VFXColorDebuff, Live.VFXColorDamping, Live.VFXFlashColorDebuff, Live.VFXFlashDuration)
			};
		}

		public static StateInfo GetStatus(Sprite icon = null, string title = null)
		{
			return new StateInfo
			{
				icon               = icon,
				icon_mask          = Live.Icon_Debuff_Mask,
				notificationPrefab = Live.DebuffPanelPrefab,
				vfx                = new StateFlashVFX(Live.VFXColorDebuff, Live.VFXColorDamping, Live.VFXFlashColorDebuff, Live.VFXFlashDuration)
			};
		}
	}

	/// <summary>
	/// A color flash that quickly fades away!
	/// </summary>
	public class StateFlashVFX : VFX
	{
		public readonly Color color;
		public readonly float damping;
		public readonly Color flashColor;
		public readonly float flashDuration;

		public float value = 1;

		private float _elapsed = 0;

		public StateFlashVFX(
			Color color,
			float damping,
			Color flashColor,
			float flashDuration
		)
		{
			this.color         = color;
			this.damping       = damping;
			this.flashColor    = flashColor;
			this.flashDuration = flashDuration;
		}

		public override Color Tint => Color.Lerp(Color.white, color, value);

		public override Color Fill
		{
			get
			{
				if (_elapsed < flashDuration)
					return flashColor;

				return color.Alpha(value);
			}
		}

		public override bool IsActive => value > 0.05f;

		public override void Update(float dt)
		{
			base.Update(dt);

			value    =  value.LerpDamp(0, damping);
			_elapsed += dt;
		}
	}

	public struct StateInfo
	{
		/// <summary>
		/// Sprite of the icon.
		/// </summary>
		public Sprite icon;

		/// <summary>
		/// Sprite of the icon.
		/// </summary>
		public Sprite icon_mask;

		/// <summary>
		/// Notification panel.
		/// </summary>
		public GameObject notificationPrefab;

		/// <summary>
		/// VFX to add to the statee.
		/// </summary>
		public VFX vfx;

		[CanBeNull]
		public string title;

		[CanBeNull]
		public string subtext;

		public bool invalid;

		public StateInfo(Sprite icon, Sprite mask, GameObject notificationPrefab, string title, string subtext)
		{
			this.icon               = icon;
			this.icon_mask          = mask;
			this.notificationPrefab = notificationPrefab;
			this.title              = title;
			this.subtext            = subtext;
			this.vfx                = null;
			this.invalid            = false;
		}

		public static implicit operator StateInfo((Sprite, Sprite, GameObject, string, string) tu) => new StateInfo(tu.Item1, tu.Item2, tu.Item3, tu.Item4, tu.Item5);

		public static implicit operator StateInfo((Sprite, Sprite, GameObject, string) tu) => new StateInfo(tu.Item1, tu.Item2, tu.Item3, tu.Item4, null);

		public static implicit operator StateInfo((Sprite, Sprite, GameObject) tu) => new StateInfo(tu.Item1, tu.Item2, tu.Item3, null, null);
	}
}