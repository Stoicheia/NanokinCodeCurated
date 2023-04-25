using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Utils;
using UnityEngine;


namespace Anjin.Nanokin.Map
{
	[RequireComponent(typeof(TimedObjectsActivator), typeof(Collectable))]
	public class InitialTimedCollectible : MonoBehaviour
	{
		private TimedObjectsActivator _toa;
		private Collectable _collectable;
		private List<Collectable> _objects;

		[SerializeField] private List<AudioDef> _collectionAudio;
		[SerializeField] private AudioDef _sfxOnCollectAll;
		[SerializeField] private List<ParticlePrefab> _particlesOnCollectAll;
		private int _numberCollected;

		private void Awake()
		{
			_toa = GetComponent<TimedObjectsActivator>();
			_collectable = GetComponent<Collectable>();
			_objects = _toa.Objects.Select(x => x.GetComponent<Collectable>()).Where(x => x != null).ToList();
		}

		private void OnEnable()
		{
			_numberCollected = 0;
			_collectable.OnSuccessfulCollect += ActivateObjects;
			_objects.ForEach(x => x.OnSuccessfulCollect += RegisterCollection);
		}

		private void OnDisable()
		{
			_collectable.OnSuccessfulCollect -= ActivateObjects;
			_objects.ForEach(x => x.OnSuccessfulCollect -= RegisterCollection);
		}

		private void ActivateObjects()
		{
			_toa.ActivateObjects();
		}

		private void RegisterCollection()
		{

			GameSFX.Play(_collectionAudio[_numberCollected++ % _collectionAudio.Count], transform);
			Debug.Log("Collected... " + _numberCollected);
			if (_numberCollected >= _objects.Count)
			{
				_toa.DisableSystem();
				GameSFX.PlayGlobal(_sfxOnCollectAll);
				_particlesOnCollectAll.ForEach(x => x.Instantiate(transform));
			}
		}
	}
}
