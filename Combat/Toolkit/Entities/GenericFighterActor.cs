using API.PropertySheet;
using API.Puppets.Components;
using Combat.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Util.Animation;

namespace Combat.Entities
{
	public class GenericFighterActor : FighterActor, INameAnimable
	{
		private CapsuleCollider _capsule;

		//public ActorRenderer Renderer;
		public INameAnimable Animable;

		public SimplePuppet _puppet;
		public bool         UseCapsuleDimensions;
		public Sprite       EventSpriteOverride;

		public    GameObject  TurnEventPrefab;
		protected GenericInfo info;

		public override GameObject TurnPrefab => TurnEventPrefab;

		protected override void Awake()
		{
			base.Awake();

			//_trails  = new List<Trail>();
			_capsule = GetComponent<CapsuleCollider>();

			//if (healthbarGroup != null)
			//{
			//	healthbarGroup.alpha = 0;
			//}
		}

		private async void Start()
		{
			await UniTask.DelayFrame(2);
		}

		public void OnCreated()
		{
			//Animable?.Play("idle");

			_puppet.Play("idle");
		}

		// public void SetupHealthbar()
		// {
		// 	if ((healthbarHUD != null) && (fighter != null))
		// 	{
		// 		info = fighter.info as GenericInfo;
		//
		// 		if (info != null)
		// 		{
		// 			//healthbarHUD.Set(info.Points.hp, info.MaxPoints.hp);
		// 			healthbarHUD.Initialize(fighter, info.Points.hp, info.MaxPoints.hp);
		// 		}
		// 	}
		// }

		public override void SetPuppetAnim(PuppetAnimation anim)
		{
			_puppet.Anim = anim;

			if (anim == null)
			{
				_puppet.StopAnim();
			}
		}

		private void Update()
		{
			if (!UseCapsuleDimensions)
			{
				_capsule.height = height;
				_capsule.radius = radius;
				_capsule.center = Vector3.up * height;
			}
			else
			{
				height = _capsule.height;
				radius = _capsule.radius;
			}

			transform.rotation = Quaternion.LookRotation(facing, Vector3.up);

			center = UseCapsuleDimensions ? transform.position + transform.TransformDirection(_capsule.center) : transform.position;
		}

		public AnimationPlayResult Play(PlayAnimations anim, PlayOptions options = PlayOptions.Continue) => Animable != null ? Animable.Play(anim, options) : AnimationPlayResult.AnimatorUnavailable;

		public override async UniTask<Sprite> GetEventSprite() => EventSpriteOverride;
	}
}