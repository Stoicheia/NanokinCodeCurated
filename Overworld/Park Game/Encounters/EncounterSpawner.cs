using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Drawing;
using Overworld.Controllers;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtilities;
using Util;
using Util.Odin.Attributes;
using _MinValue = Sirenix.OdinInspector.MinValueAttribute;

namespace Anjin.Nanokin.Park
{
	// Placed in edit mode. Spawns encounters based on the state from the ParkController.
	[AddComponentMenu("Anjin: Game Building/Encounter Spawner")]
	public class EncounterSpawner : MonoBehaviourGizmos
	{
		/// <summary>
		/// All the encounter spawners currently active in the game.
		/// </summary>
		public static readonly List<EncounterSpawner> All = new List<EncounterSpawner>();

		[Title("Overworld Monsters")]
		[MinValue(1)] public int SpawnCount;

		[MinValue(0)] public float AreaRadius;

		[EnumToggleButtons]
		public LaunchMode Mode = LaunchMode.Normal;

		[Title("Settings Overrides")]
		[Inline]
		public EncounterSettings Settings;

		[Title("Debug")]
		[SerializeField] private Color GizmoColor = Color.red.Alpha(0.8f);

		[NonSerialized] public List<SpawnedEncounter> spawnedMonsters;

		private EncounterSettings      _settings;
		private List<SpawnedEncounter> _inactiveMonsters;
		private EncounterBounds        _bounds;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			All.Clear();
		}

		private void Awake()
		{
			All.Add(this);

			_bounds         = GetComponent<EncounterBounds>();
			spawnedMonsters = new List<SpawnedEncounter>();
		}

		private void Start()
		{
			_settings = new EncounterSettings();
			EncounterLayer.Refresh(ref _settings, Settings, transform.position);

			if (_settings.MonsterPrefab == null)
			{
				DebugLogger.Log("Could not find any monster prefab! Must set one in the EncounterSpawner or set defaults through a EncounterDefaultsVolume.", LogContext.Overworld, LogPriority.High);
				return;
			}

			if (!GameOptions.current.ow_encounters)
				return;

			// Instantiate and setup the monsters
			for (var i = 0; i < SpawnCount; i++)
			{
				GameObject obj = PrefabPool.Rent(_settings.MonsterPrefab, transform.position, Quaternion.identity);

				EncounterMonster monster = obj.GetComponent<EncounterMonster>();
				monster.home       = transform.position;
				monster.homeRadius = AreaRadius;
				monster.bounds = _bounds;

				Actor            actor = obj.GetComponent<Actor>();
				SpawnedEncounter mo    = new SpawnedEncounter(obj, actor, monster);
				spawnedMonsters.Add(mo);

				// Set-up the encounter trigger.

				monster.onTrigger = TriggerEncounter;
			}

			Respawn(true);
		}

		public void TriggerEncounter(EncounterMonster monster, EncounterAdvantages advantage) => TriggerEncounter(monster, advantage, null);

		public async void TriggerEncounter(EncounterMonster monster, EncounterAdvantages advantage, LaunchMode? modeOverride = null)
		{
			if (GameController.Live.IsInBattle) return;
			if (!GameOptions.current.ow_encounters_launch)
			{
				DebugLogger.Log($"Not starting encounter fight because {nameof(GameOptions.current.ow_encounters_launch)} is enabled in option.ini", this, LogContext.Overworld, LogPriority.Critical);
				return;
			}

			if ((modeOverride ?? Mode) == LaunchMode.Disabled) return;

			if (advantage != EncounterAdvantages.Player)
			{
				if (monster.TryGetComponent(out Actor ccEnemy) && ccEnemy.IsMotorStable) ccEnemy.ClearVelocity();

				if (ActorController.playerActor.IsMotorStable)
					ActorController.playerActor.ClearVelocity();
			}

			EncounterLayer.Refresh(ref _settings, Settings, transform.position);

			await BattleController.LaunchEncounter(monster,  _settings, advantage);

			monster.Despawn();
		}

		/// <summary>
		/// Trigger the first spawned encounter of this spawner.
		/// </summary>
		/// <param name="advantage"></param>
		public void TriggerFirst(EncounterAdvantages advantage)
		{
			if (spawnedMonsters.Count == 0)
				return;

			TriggerEncounter(spawnedMonsters[0].encounter, advantage, LaunchMode.Normal);
		}

		[Button, ShowInPlay]
		public void Despawn()
		{
			foreach (SpawnedEncounter mo in spawnedMonsters)
			{
				mo.encounter.Despawn();
			}
		}

		/// <summary>
		/// Respawn the encounters that are despawned.
		/// </summary>
		[Button, ShowInPlay]
		public void Respawn(bool force = false)
		{
			foreach (SpawnedEncounter monster in spawnedMonsters)
			{
				if (!force && monster.encounter.spawned)
					// Already spawned!
					continue;

				if (Enabler.Set(monster.gobject, true, SystemID.Spawned, (int)SystemID.Occluder))
				{
					monster.actor.Teleport(transform.position + RNG.OnCircle * AreaRadius); // Teleport to a new position.

					List<IRecyclable> recyclables = ListPool<IRecyclable>.Claim(4);

					monster.gobject.GetComponentsInChildren(recyclables);
					foreach (IRecyclable recyclable in recyclables)
						recyclable.Recycle();

					ListPool<IRecyclable>.Release(ref recyclables);

					monster.encounter.spawned = true;
					monster.encounter.onSpawn?.Invoke();

					if (_bounds && monster.actor is ActorKCC kcc)
						kcc.bounds = _bounds;
				}
			}
		}

		/// <summary>
		/// Re-activate the monsters. (does not activate despawned monsters)
		/// </summary>
		[Button, ShowInPlay]
		public void Enable()
		{
			foreach (SpawnedEncounter monster in spawnedMonsters)
			{
				Enabler.Enable(monster.gobject, SystemID.Encounters);
			}
		}

		/// <summary>
		/// De-activate the monsters. (does not despawn)
		/// </summary>
		[Button, ShowInPlay]
		public void Disable()
		{
			foreach (SpawnedEncounter monster in spawnedMonsters)
			{
				Enabler.Disable(monster.gobject, SystemID.Encounters);
			}
		}

		private void OnDestroy()
		{
			All.Remove(this);

			foreach (SpawnedEncounter monster in spawnedMonsters)
			{
				if(monster.gobject != null)
					PrefabPool.Return(monster.gobject);
			}

			spawnedMonsters.Clear();
		}

		private void OnDespawn()
		{

		}

		public override void DrawGizmos()
		{
			base.DrawGizmos();

			if (GizmoContext.InSelection(this))
			{
				Draw.WireSphere(transform.position, AreaRadius, GizmoColor);
			}
		}

		[LuaGlobalFunc("encounters_deactivate")]
		[Button, ShowInPlay]
		public static void DeactivateEncounters()
		{
			foreach (EncounterSpawner spawner in All)
				spawner.Disable();
		}

		[LuaGlobalFunc("encounters_activate")]
		[Button, ShowInPlay]
		public static void ActivateEncounters()
		{
			foreach (EncounterSpawner spawner in All)
				spawner.Enable();
		}

		[Title("Start Battle")]
		[Button, ShowInPlay, LabelText("Neutral Advantage")]
		public void TriggerWithNeutralAdvantage() => TriggerFirst(EncounterAdvantages.Neutral);

		[Button, ShowInPlay, LabelText("Player Advantage")]
		public void TriggerWithPlayerAdvantage() => TriggerFirst(EncounterAdvantages.Player);

		[Button, ShowInPlay, LabelText("Enemy Advantage")]
		public void TriggerWithEnemyAdvantage() => TriggerFirst(EncounterAdvantages.Enemy);

		// TODO: Add NoIntro option for debugging
		public enum LaunchMode
		{
			Normal,
			Disabled,
		}

		/// <summary>
		/// A monster that has been spawned by this EncounterSpawner.
		/// Stores the object and the actor.
		/// </summary>
		public readonly struct SpawnedEncounter
		{
			public readonly GameObject       gobject;
			public readonly Actor            actor;
			public readonly EncounterMonster encounter;

			public SpawnedEncounter(GameObject gobject, Actor actor, EncounterMonster encounter)
			{
				this.gobject   = gobject;
				this.actor     = actor;
				this.encounter = encounter;
			}
		}
	}
}