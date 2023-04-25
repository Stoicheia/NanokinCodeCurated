using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Utils;
using Assets.Scripts.Utils;
using Knife.DeferredDecals;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;
using Util;
using Vexe.Runtime.Extensions;

namespace Anjin.Nanokin.Map
{
	[RequireComponent(typeof(PhysicsInteractible))]
	public class Pinata : MonoBehaviour
	{
		private                                 PhysicsInteractible  _pi;
		[SerializeField]                private int                  _hitsUntilExplode;
		[SerializeField]                private List<Rigidbody>      _contents;
		[SerializeField]                private List<AudioClip>      _sfxOnHit;
		[SerializeField]                private List<ParticlePrefab> _fxOnHit;
		[SerializeField]                private AudioClip            _sfxOnExplode;
		[SerializeField]                private List<ParticlePrefab> _fxOnExplode;
		[SerializeField]                private Vector3              _explosionVariance;
		[SerializeField]                private float                _explosiveForce;
		[SerializeField] [Range(0, 90)] private float                _explosiveAngle; //

		[SerializeField] private bool _spillOnHit;
		[SerializeField] [ShowIf("@_spillOnHit")]
		private float _amount;

		[SerializeField] private Rotor _rotor;

		private int _hp;

		private void Awake()
		{
			_pi = GetComponent<PhysicsInteractible>();
			_hp = _hitsUntilExplode;
		}

		private void OnEnable()
		{
			_hp = _hitsUntilExplode;
			_pi.OnReceiveHit += HandleHit;
		}

		private void OnDisable()
		{
			_pi.OnReceiveHit -= HandleHit;
		}

		private void HandleHit(IHitInfo hitInfo)
		{
			SwordHit hit          = (SwordHit) hitInfo;
			int      hitsReceived = _hitsUntilExplode - _hp;
			if(_sfxOnHit.Count > 0)
				GameSFX.Play(_sfxOnHit[hitsReceived % _sfxOnHit.Count], transform.position);
			if(_fxOnHit.Count > 0)
				_fxOnHit[hitsReceived % _fxOnHit.Count].Instantiate(transform);
			if (--_hp <= 0)
			{
				Explode(hitInfo);
			}
			else if (_spillOnHit)
			{
				int amountToSpill = UnityEngine.Random.Range(0f, 1f) > _amount - Mathf.Floor(_amount)
					? Mathf.CeilToInt(_amount) : Mathf.FloorToInt(_amount);
				for (int i = 0; i < amountToSpill; i++)
				{
					Spawn(_contents[UnityEngine.Random.Range(0, _contents.Count)]);
				}
			}

			if (_rotor != null)
			{
				_rotor.Spin();
			}
		}

		private void Explode(IHitInfo hitInfo)
		{
			SwordHit hit = (SwordHit) hitInfo;
			GameSFX.Play(_sfxOnExplode, transform.position);
			foreach (var e in _fxOnExplode)
			{
				GameObject particles = e.Instantiate(transform);
				particles.transform.localScale *= 3;
			}

			foreach (Rigidbody content in _contents)
			{
				Spawn(content);
			}

			Destroy(transform.parent == null ? gameObject : transform.parent.gameObject);
		}

		private void Spawn(Rigidbody toSpawn)
		{
			Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(0, _explosionVariance.x),
				UnityEngine.Random.Range(0,_explosionVariance.y),
				UnityEngine.Random.Range(0,_explosionVariance.z));
			Rigidbody rb = Instantiate(toSpawn, transform.position + randomOffset, transform.rotation);
			Decal d = rb.GetComponentInChildren<Decal>();
			if (d != null) d.enabled = false;
			rb.GetComponent<Collectable>()?.DisableColliderForSeconds(0.4f);
			rb.GetComponent<Collider>().isTrigger                  = false;
			rb.GetComponentsInChildren<OscillateOverTime>().Foreach(x => x.enabled = false);
			rb.isKinematic                                         = false;
			rb.GetComponent<Collectable>()?.SetGravity(3.6f);
			rb.AddForce(Geometry.RandomCone(_explosiveForce, _explosiveAngle), ForceMode.Impulse);
		}


	}
}