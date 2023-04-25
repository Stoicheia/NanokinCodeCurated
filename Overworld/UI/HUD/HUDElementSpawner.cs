using System;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using Overworld.Tags;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Assets;
using Util.Odin.Attributes;

namespace Anjin.UI {
	public class HUDElementSpawner : SerializedMonoBehaviour, IActivable {

		public                                          GameObject Prefab;
		public                                          Actor      Actor;

		[FormerlySerializedAs("ScaleWithActor")]
		public bool       ScaleWithTransformOrActor = true;

		public Asset<Config, HUDElementSpawnerConfig> config = new Asset<Config, HUDElementSpawnerConfig>();

		[NonSerialized, ShowInPlay]
		public HUDElement Element;

		[ShowInPlay]
		private bool _spawned;
		private bool _showing;

		[ShowInPlay]
		private Actor _actor;

		[ShowInPlay]
		private float _targetAlpha;


		private void Awake()
		{
			if (Actor != null) {
				_actor = Actor;
			} else {
				_actor = GetComponent<Actor>();
				if (_actor == null) {
					_actor = GetComponentInParent<Actor>();
				}
			}

		}

		private void Start() => Spawn();

		private void OnDestroy() => Despawn();

		[Button]
		public void Spawn()
		{
			if(_spawned) Despawn();

			if (Prefab == null || !GameHUD.Exists) return;

			var ins = Prefab.Instantiate(GameHUD.Live.ElementsRect);
			Element = ins.GetComponent<HUDElement>();

			if (ScaleWithTransformOrActor)
			{
				ViewScaledHUDElement vshe = Element.GetComponent<ViewScaledHUDElement>();
				if (vshe == null)
					Debug.LogWarning("No scaling component on this gameobject.", this);
				else {
					vshe.SetTarget(_actor != null ? _actor.transform : transform);
				}
			}

			Element.SetPositionModeWorldPoint(_actor ? _actor.GetHeadPoint() : new WorldPoint(gameObject), Vector3.zero);

			Element.Alpha = 0;

			_spawned = true;
		}

		//This update loop is already filled with expensive functions, so adding a few cheap ones won't hurt
		private void Update()
		{
			if (!_spawned) return;

			bool within_range = ActorController.playerActor != null &&
								ActorController.playerActor.transform.position.Distance(transform.position) < config.Value.ActiveDistance;

			bool can_show     = within_range &&
								!GameHUD.Live.showingInteract &&
								GameController.Live.IsPlayerControlled &&
								ActorController.playerActor.activeBrain == ActorController.playerBrain &&
								!SplicerHub.menuActive;

			_targetAlpha = can_show ? 1 : 0;
			_showing = can_show;
			Element.Alpha += (_targetAlpha * 2 -1) * config.Value.AlphaTransitionSpeed * Time.deltaTime;
			Element.Alpha = Mathf.Clamp01(Element.Alpha);
			Element.gameObject.SetActive(Element.Alpha > 0.001f);
		}

		[Button]
		public void Despawn()
		{
			if (!_spawned) return;
			_spawned = false;

			if (Element != null) {
				Element.gameObject.Destroy();
			}
		}

		[Button]
		public void Show(bool anim = true)
		{
			if(Element)
				Element.gameObject.SetActive(true);

			_targetAlpha = 1;
		}

		[Button]
		public void Hide(bool anim = true)
		{
			_targetAlpha = 0;

			if(Element)
				Element.gameObject.SetActive(false);
		}

		public void OnActivate()   => Show(false);
		public void OnDeactivate() => Hide(false);


		[Serializable]
		public class Config {
			public float ActiveDistance = 30f;
			public float AlphaTransitionSpeed = 2f;
		}
	}
}