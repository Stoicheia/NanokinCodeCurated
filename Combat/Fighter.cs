using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using API.PropertySheet;
using Combat.Data;
using Combat.Data.VFXs;
using Combat.Entities;
using Combat.Features.TurnOrder.Events;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using SaveFiles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Combat
{
	[LuaUserdata]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public sealed class Fighter : BattleResource, ITurnActer, ILogger, ITargetable, IStatee
	{
		/// <summary>
		/// Unique ID of the fighter.
		/// This is used to identify the fighters in netplay,
		/// as well as recognizing them for debugging.
		/// </summary>
		public string id;

		/// <summary>
		/// The source data of the fighter. (stateless, represents the maxed out state)
		/// </summary>
		public FighterInfo info;

		/// <summary>
		/// A script with custom logic for the fighter.
		/// </summary>
		[CanBeNull]
		public FighterScript script;

		/// <summary>
		/// Current team.
		/// </summary>
		public Team team;

		/// <summary>
		/// Will override the team's brain if not null.
		/// </summary>
		[CanBeNull]
		public BattleBrain brain;

		/// <summary>
		/// Current home slot.
		/// This is only backend data, setting this does not ensure
		/// that the fighter will be positioned on the slot visually.
		/// </summary>
		[CanBeNull]
		public Slot home;

		/// <summary>
		/// The slot to try to claim upon spawning.
		/// </summary>
		public Slot spawnHome;

		private Slot virtualHome;

		/// <summary>
		/// Current hp, sp, and op.
		/// </summary>
		public Pointf points;

		public int HP => (int)points.hp;
		public int SP => (int)points.sp;
		public int OP => (int)points.op;

		/// <summary>
		/// Current combo state.
		/// </summary>
		public Battle.ComboState combo;

		/// <summary>
		/// Current state stats. (computed from the states list)
		/// </summary>
		public Status status = new Status();

		/// <summary>
		/// Basic configuration for the fighter, used for
		/// configuring the fighter's initial state and spawning.
		/// </summary>
		public State baseState = new State();

		/// <summary>
		/// Alias for the home slot.
		/// </summary>
		public Slot slot => home;

		/// <summary>
		/// Current home coordinate.
		/// </summary>
		public Vector2Int coord => home.coord;

		public List<StickerInstance> stickers = new List<StickerInstance>();

		/// <summary>
		/// All currently managed skills for this fighter.
		/// </summary>
		public List<BattleSkill> skills = new List<BattleSkill>();

		/// <summary>
		/// All tags associated with this fighter
		/// </summary>
		public List<string> tags = new List<string>();

		public List<Trigger> triggers = new List<Trigger>();

		/// <summary>
		/// Whether or not this fighter can be targeted.
		/// This is a global flag to create special fighters like the death crystal.
		/// The state API should be used instead in most circumstances.
		/// </summary>
		public bool targetable = true;

		/// <summary>
		/// Whether or not the fighter should be included in the turn order.
		/// </summary>
		public bool turnEnable = true;

		/// <summary>
		/// Marked for death. The fighter is "dead" and will
		/// be killed on the next death flush.
		/// </summary>
		public bool deathMarked;

		/// <summary>
		/// Marked for revive. The fighter will
		/// be brought back to live on the next revive flush.
		/// </summary>
		public bool reviveMarked;

		/// <summary>
		/// The fighter's existence state.
		/// </summary>
		public Existence existence = Existence.None;

		/// <summary>
		/// Handler to reintegrate the fighter into battle
		/// and clean up the death.
		/// </summary>
		public Action<Fighter> onRevive;

		/// <summary>
		/// Table to use in scripting for arbitrary storage on a fighter.
		/// </summary>
		public Table props;

		/// <summary>
		/// The prefab to spawn this fighter's actor.
		/// </summary>
		public GameObject prefab;

		/// <summary>
		/// The prefab to spawn this fighter's actor.
		/// </summary>
		public string prefabAddress;

		/// <summary>
		/// The fighter's view.
		/// In simulated combat, this will be null!
		/// </summary>
		public FighterActor actor;

		/// <summary>
		/// The fighter's coach.
		/// </summary>
		public Coach coach;

		public MoveSemantic moveSemantic = MoveSemantic.Ground;

		public bool dead => existence == Existence.Dead;

		public string dna => info.DNA;

		public string sample
		{
			get => info.Sample;
			set => info.Sample = value;
		}

		[UsedImplicitly]
		public DynValue this[string key]
		{
			get => props.Get(key);
			set => props[key] = value;
		}

		public Fighter([NotNull] FighterInfo info)
		{
			props     = new Table(Lua.envScript);
			this.info = info;
			status.Add(baseState);
		}

		public UniTask<Sprite> GetEventSprite()
		{
			if (actor == null)
				return UniTask.FromResult<Sprite>(null);

			return actor.GetEventSprite();
		}

		[UsedImplicitly]
		public void SetPuppetAnim(PuppetAnimation anim)
		{
			if (actor != null)
			{
				actor.SetPuppetAnim(anim);
			}
		}

		public bool       TurnEnable   => turnEnable;
		public int        TurnBonus    => info.Actions;
		public int        TurnPriority => info.Priority;
		public GameObject TurnPrefab   => actor.TurnPrefab;
		public string     TurnName     => Name;

		public UniTask<GameObject> ShadowPrefab    => actor.CreateSilhouette();
		public Slot                HomeTargeting   => virtualHome ?? home;
		public bool                VirtuallyAtHome => virtualHome == null || virtualHome == home;

		public override bool IsAlive => existence == Existence.Exist;

	#region Shortcuts

		public bool is_player => team?.isPlayer == true;

		[NotNull]
		public string Name => info.Name;

		public float    hp_percent => points.hp / battle.GetMaxPoints(this).hp;
		public float    sp_percent => points.sp / battle.GetMaxPoints(this).sp;
		public Pointf   max_points => battle.GetMaxPoints(this);
		public Statf    stats      => battle.GetStats(this);
		public Elementf resistance => battle.GetResistance(this);
		public Elementf res        => battle.GetResistance(this);
		public Elementf atk        => battle.GetOffense(this);
		public Elementf def        => battle.GetDefense(this);

		public float hp => points.hp;
		public float sp => points.sp;
		public float op => points.op;

		public float max_hp => max_points.hp;
		public float max_sp => max_points.sp;
		public float max_op => max_points.op;

		public float power => stats.power;
		public float speed => stats.speed;
		public float will  => stats.will;

		public float blunt  => res.blunt;
		public float slash  => res.slash;
		public float pierce => res.pierce;
		public float gaia   => res.gaia;
		public float astra  => res.astra;
		public float oida   => res.oida;

		public float atk_blunt  => atk.blunt;
		public float atk_slash  => atk.slash;
		public float atk_pierce => atk.pierce;
		public float atk_gaia   => atk.gaia;
		public float atk_astra  => atk.astra;
		public float atk_oida   => atk.oida;

		public float def_blunt  => def.blunt;
		public float def_slash  => def.slash;
		public float def_pierce => def.pierce;
		public float def_gaia   => def.gaia;
		public float def_astra  => def.astra;
		public float def_oida   => def.oida;

		public int? level
		{
			get
			{
				if (info is NanokinInfo ninfo && ninfo.instance != null)
					return ninfo.instance.Level;
				return null;
			}
		}

		[UsedImplicitly]
		public Vector3 position
		{
			get => actor.transform.position;
			set => actor.transform.position = value;
		}

		[UsedImplicitly]
		public Vector3 facing
		{
			get => actor.facing;
			set => actor.facing = value;
		}

		[UsedImplicitly]
		public float radius => actor.radius;

		[UsedImplicitly]
		public float height => actor.height;


		public void NotifyCoach(AnimID id)
		{
			if (coach != null)
			{
				coach.SetAnim(id);
			}
		}

		/// <summary>
		/// Check that the fighter has the specified state. (by id)
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[UsedImplicitly]
		public bool has_state(string id) => battle.HasState(this, id);

		/// <summary>
		/// Check that the fighter has the specified state. (by tag)
		/// </summary>
		/// <param name="tag"></param>
		/// <returns></returns>
		[UsedImplicitly]
		public bool has_tag(string tag) => battle.HasTag(this, tag);

		/// <summary>
		/// Get the states for a tag or id.
		/// </summary>
		/// <param name="tbl">Table of ID strings</param>
		/// <returns>Temporary result, do not cache it.</returns>
		[UsedImplicitly]
		public List<State> get_states([NotNull] string tag_or_id) => battle.GetStates(tag_or_id);

		[UsedImplicitly]
		public State get_state([NotNull] string tag_or_id)
		{
			List<State> states = battle.GetStates(tag_or_id);
			DebugLogger.Log("I got " + states.Count + " states", LogContext.Combat, LogPriority.Low);
			return states.Count > 0 ? battle.GetStates(tag_or_id)[0] : null;
		}


		/// <summary>
		/// Check that the fighter has all the states. (by id)
		/// </summary>
		/// <param name="tbl">Table of ID strings</param>
		/// <returns></returns>
		[UsedImplicitly]
		public bool get_states([NotNull] Table tbl)
		{
			for (var i = 0; i < tbl.Length; i++)
			{
				if (!tbl.Get(i).AsString(out string stateid) || !battle.HasState(this, stateid))
					return false;
			}

			return true;
		}

		[UsedImplicitly]
		public int add_states([NotNull] string id) => battle.CountStates(this, id);

		[UsedImplicitly]
		public void add_state([NotNull] Table tbl)
		{
			State state = new State();
			state.AddStatees(this);
			state.ConfigureTB(tbl);
			battle.AddState(state);
		}

		[UsedImplicitly]
		public int remove_states([NotNull] Table tbl) => battle.LoseStates(this, tbl);

		[UsedImplicitly]
		public int remove_states(string idOrTag) => battle.LoseStates(this, idOrTag, idOrTag);

		[UsedImplicitly]
		public void kill()
		{
			battle.RemoveFighter(this, true);
		}

		public int get_removed_state_uid(string id) => status.get_removed_state_uid(id);

		[UsedImplicitly]
		public void kill_mark()
		{
			deathMarked = true;
		}

		public override void ConfigureTB([CanBeNull] Table tb)
		{
			base.ConfigureTB(tb);
			if (tb == null) return;

			info.ConfigureTB(tb);

			id        = tb.TryGet("id", id);
			points.hp = tb.TryGet("hp", hp);
			points.sp = tb.TryGet("sp", sp);
			points.op = tb.TryGet("op", op);

			if (tb.TryGet("tag", out string tag)) tags.Add(tag);
			if (tb.TryGet("tags", out Table tagsTable))
			{
				for (var i = 0; i < tagsTable.Length; i++)
				{
					if (tagsTable.Get(i).AsString(out string tstr))
						tags.Add(tstr);
				}
			}

			home       = tb.TryGet("home", home);
			home       = tb.TryGet("slot", home);
			team       = tb.TryGet("team", team);
			turnEnable = tb.TryGet("turns", turnEnable);
			targetable = tb.TryGet("target", targetable);

			if (tb.TryGet("prefab", out prefab, prefab))
			{
				if (prefab.TryGetComponent(out ObjectFighterActor actor))
				{
					ObjectFighterAsset asset = actor.Asset;
					if (asset != null)
						script = new FighterScript(this, asset);
				}
			}

			if (tb.TryGet("slot", out Vector2Int v2i)) home       = battle.GetSlot(v2i);
			else if (tb.TryGet("home", out Vector2Int v2i2)) home = battle.GetSlot(v2i2);

			if (tb.TryGet("brain", out BattleBrain bbrain)) brain = bbrain;
			if (tb.TryGet("brain", out string sbrain)
			    && LuaUtil.ParseEnum(sbrain, out BattleBrains brainType)) brain = CombatAPI.Brain(brainType);
		}

		[MoonSharpVisible(true)]
		private Fighter configure([CanBeNull] DynValue dv)
		{
			if (dv == null) return this;
			if (dv.AsTable(out Table tbl)) ConfigureTB(tbl);

			return this;
		}

	#endregion

	#region Actor Stuff

		// These shortcuts help to make Lua scripting more comfortable
		// ------------------------------------------------------------

		[UsedImplicitly, CanBeNull] public AudioClip sfx_hurt    => actor.HurtSFX;
		[UsedImplicitly, CanBeNull] public AudioClip sfx_grunt   => actor.GruntSFX;
		[UsedImplicitly, CanBeNull] public AudioClip sfx_screech => actor.ScreechSFX;
		[UsedImplicitly, CanBeNull] public AudioClip sfx_death   => actor.DeathSFX;


		public void set_actor(GameObject gobj)
		{
			if (gobj != null && gobj.TryGetComponent(out actor))
			{
				actor.fighter = this;
				SceneManager.MoveGameObjectToScene(gobj, battle.runner.Scene);
				gobj.name = $"Fighter {id} - {info.Name}";

				info.delete_string("no_hurt_anim");

				if (actor is GenericFighterActor)
				{
					info.clear_ints();
					info.clear_strings();
				}
			}
		}

		/// <summary>
		/// Move and orient the fighter to/with its respective slot.
		/// </summary>
		public void snap_home()
		{
			if (actor == null) return;
			if (home == null) return;
			if (home.actor == null) return;

			actor.transform.position = home.actor.transform.position;
			actor.facing             = home.facing;
			this.LogVisual("--", "fighter.snap_home");
		}

		/// <summary>
		/// Move and orient the fighter to/with its respective slot.
		/// </summary>
		public void snap_slot(Slot slot)
		{
			if (actor != null)
			{
				actor.transform.position = slot.position;
				actor.facing             = slot.facing;
				this.LogVisual("--", "fighter.snap_");
			}
		}


		public WorldPoint anchor(string id) => actor.anchor(id);


		public WorldPoint rel_offset(Vector3 from, float fwd, float y, float horizontal) => actor.rel_offset(from, fwd, y, horizontal);
		public WorldPoint xy_offset(float    fwd,  float y,   float horizontal = 0) => actor.xy_offset(fwd, y, horizontal);
		public WorldPoint offset(float       z,    float y,   float x          = 0) => actor.offset(z, y, x);

		[UsedImplicitly]
		public Vector3 offset3(float z, float y, float x) =>
			// Technically this is the same as offset_zyx right above
			// We should try to remove offset3 and replace with simply offset to remain consistent
			actor.center + facing * z + actor.Up * y + Vector3.Cross(facing, Vector3.up) * x;

		public WorldPoint identity_offset(float d)                                           => actor.identity_offset(d);
		public WorldPoint polar_offset(float    rad,      float angle, float horizontal = 0) => actor.polar_offset(rad, angle, horizontal);
		public WorldPoint ahead(float           distance, float horizontal = 0) => actor.ahead(distance, horizontal);
		public WorldPoint behind(float          distance, float horizontal = 0) => actor.behind(distance, horizontal);
		public WorldPoint above(float           distance = 0) => actor.above(distance);
		public WorldPoint under(float           distance)     => actor.under(distance);

	#endregion

		public WorldPoint Center => new WorldPoint(actor.center) { gameobject = actor.gameObject };

	#region ITargetable

		public Vector3 GetTargetPosition() => actor != null ? actor.transform.position : Vector3.zero;

		public Vector3 GetTargetCenter() => actor != null ? actor.center : Vector3.zero;

		public GameObject GetTargetObject() => actor.gameObject;

	#endregion

	#region IStatee

		public List<State> States => states;
		public Status      Status => status;
		public BattleActor Actor  => actor;

		public void AddVFX(VFX vfx)
		{
			actor.vfx.Add(vfx);
		}

		public void RemoveVFX(VFX vfx)
		{
			actor.vfx.Remove(vfx);
		}

	#endregion


	#region Logging

		public override string ToString() => $"[{id}]-{info.Name}";

		[NotNull]
		public new string LogID => $"{id}-{info.Name}";

		public new bool LogSilenced => battle.LogSilenced;

		public List<State> states => status.states;

	#endregion
	}
}