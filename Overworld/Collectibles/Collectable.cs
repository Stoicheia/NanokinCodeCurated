using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Data.Shops;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Util.Odin.Attributes;
using Random = System.Random;

namespace Anjin.Nanokin.Map
{
	public class Collectable : MonoBehaviour
	{
		public event Action OnSuccessfulCollect;

		[Title("Setup")]
		public bool HasRespawn = true;

		[ShowIf("@HasRespawn")]
		public float RespawnDuration = 20f;

		public bool PlayerAttracts;

		[Range(0, PlayerControlBrain.COLLECTABLE_DETECT_RADIUS)]
		[ShowIf("$PlayerAttracts")]
		public float PlayerAttractRange;

		[NonSerialized, ShowInPlay]
		public Vector3? InitialPosition;

		public Transform  ParticleSpawnPoint;
		public GameObject CollectParticlePrefab;

		[NonSerialized] public Closure on_collect_lua;
		public                 string  on_collect_global_func;

		[NonSerialized] public bool  spawned;
		[NonSerialized] public float respawnTimer;

		[NonSerialized] public bool respawnFlag;

		[Title("SFX")]
		[SerializeField] private List<AudioClip> pickupSFX;

		private Collider _col;
		private Rigidbody _rb;
		private float _myGravity;
		private bool _gravitational;
		[SerializeField] private Transform _excludeFromDeactivation;

		protected virtual void OnDestroy()
		{
			if (!spawned && HasRespawn)
			{
				CollectibleSystem.respawnUpdate.Remove(this);
			}
		}

		private void Awake()
		{
			InitialPosition = transform.position;
			_col = GetComponent<Collider>();
			_rb = GetComponent<Rigidbody>();
		}

		private void Start()
		{
			spawned = true;
		}

		public virtual bool OnCollect()
		{
			if (!spawned)
				return false;

			spawned = false;
			if (HasRespawn)
			{
				respawnTimer = RespawnDuration;
				CollectibleSystem.respawnUpdate.Add(this);
			}

			//print("collect " + transform.GetSiblingIndex() + ", " + name);
			//Lua.RunPlayer().AutoReturn();

			on_collect_lua?.Call();

			if (!on_collect_global_func.IsNullOrWhitespace())
			{
				Lua.InvokeGlobal(on_collect_global_func, null, true);
			}

			// TODO now that Update() happens in CollectibleSystem, we can probably just disable the root object
			for (int i = 0; i < transform.childCount; i++)
			{
				if(transform.GetChild(i) != _excludeFromDeactivation)
					Enabler.Set(transform.GetChild(i).gameObject, false, SystemID.Self);
			}

			if (CollectParticlePrefab)
				PrefabPool.Rent(CollectParticlePrefab, ParticleSpawnPoint.transform.position, Quaternion.identity, transform);

			if(pickupSFX.Count > 0)
				GameSFX.PlayGlobal(pickupSFX[UnityEngine.Random.Range(0,pickupSFX.Count)]);
			OnSuccessfulCollect?.Invoke();
			return true;
		}

		[Button, ShowInPlay]
		public void Reset()
		{
			spawned      = true;
			respawnTimer = 0;

			if (InitialPosition.HasValue)
				transform.position = InitialPosition.Value;

			for (int i = 0; i < transform.childCount; i++)
				Enabler.Set(transform.GetChild(i).gameObject, true, SystemID.Self);
		}

		private void FixedUpdate()
		{
			if(_gravitational)
				_rb.AddForce(Physics.gravity * _rb.mass * _myGravity);
		}

		[Button, ShowInPlay]
		public void Respawn()
		{
			respawnFlag = true;
		}

		private void OnTriggerEnter(Collider collider)
		{
			if (respawnTimer > 0) return;

			if (collider.TryGetComponent<PlayerActor>(out PlayerActor actor) && actor == ActorController.playerActor)
			{
				OnCollect();
			}
		}

		private void OnCollisionEnter(Collision c)
		{
			if (respawnTimer > 0) return;

			if (c.collider.TryGetComponent<PlayerActor>(out PlayerActor actor) && actor == ActorController.playerActor)
			{
				OnCollect();
			}
		}

		[LuaProxyTypes(typeof(StatCollectable))]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class CollectableProxy<T> : MonoLuaProxy<T> where T : Collectable
		{
			public bool    has_respawn      { get => proxy.HasRespawn;      set => proxy.HasRespawn = value; }
			public float   respawn_duration { get => proxy.RespawnDuration; set => proxy.RespawnDuration = value; }
			public Closure on_collect       { get => proxy.on_collect_lua;  set => proxy.on_collect_lua = value; }

			public void reset() => proxy.Reset();
		}

		public class CollectableProxy : CollectableProxy<Collectable> { }

		public class ItemCollectableProxy : CollectableProxy<LootCollectable>
		{
			public bool      auto_play_item_get_cutscene { get => proxy.AutoPlayItemGetCutscene; set => proxy.AutoPlayItemGetCutscene = value; }
			public LootEntry loot                        { get => proxy.Loot;                    set => proxy.Loot = value; }
		}

		private IEnumerator DisableColliderSequence(float seconds)
		{
			_col.enabled = false;
			PlayerAttracts = false;
			yield return new WaitForSeconds(seconds);
			_col.enabled = true;
			PlayerAttracts = true;
		}

		public void DisableColliderForSeconds(float seconds)
		{
			StartCoroutine(DisableColliderSequence(seconds));
		}

		public void SetGravity(float g)
		{
			_myGravity = g;
			_gravitational = true;
		}
	}
}