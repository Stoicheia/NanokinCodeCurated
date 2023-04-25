using System.Collections.Generic;
using Anjin.Util;
using API.PropertySheet;
using API.Puppets;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using JetBrains.Annotations;
using Puppets;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util.Addressable;
using Util.Animation;
using Util.RenderingElements.Trails;

namespace Combat.Entities
{
	public class NanokinFighterActor : FighterActor
	{
		[SerializeField]
		public WorldPuppet Puppet;

		private NanokinInstance _nanokin;
		private Sprite          _eventSprite;

		private List<Trail>     _trails;
		private TweenMovable    _movable;
		private CapsuleCollider _capsule;
		private INameAnimable   _nameAnimable;

		private AsyncOperationHandle<ScreechSoundSet> _soundset;
		private AsyncOperationHandle<AudioClip>       _hndHurt;
		private AsyncOperationHandle<AudioClip>       _hndScreech;
		private AsyncOperationHandle<AudioClip>       _hndDeath;
		private AsyncOperationHandle<AudioClip>       _hndGrunt;
		private AudioClip                             _sfxHurt;
		private AudioClip                             _sfxScreech;
		private AudioClip                             _sfxDeath;
		private AudioClip                             _sfxGrunt;

		private AsyncHandles _handles;

		public override AudioClip HurtSFX    => _sfxHurt;
		public override AudioClip GruntSFX   => _sfxGrunt;
		public override AudioClip ScreechSFX => _sfxScreech;
		public override AudioClip DeathSFX   => _sfxDeath;

		protected override void Awake()
		{
			base.Awake();

			_trails  = new List<Trail>();
			_capsule = GetComponent<CapsuleCollider>();
			_handles = new AsyncHandles();
		}

		// TODO OnCleanup

		public async UniTask ChangeNanokin([NotNull] NanokinInstance nanokin) //
		{
			_nanokin          = nanokin;
			_movable          = GetComponent<TweenMovable>();
			_movable.BaseMove = nanokin[LimbType.Body].Asset.MoveAnimation; //
			var puppet = new PuppetState(nanokin.ToPuppetTree(_handles));
			await puppet.AwaitLoading();

			Puppet.SetPuppet(puppet);

			_nameAnimable = GetComponent<INameAnimable>();
			_nameAnimable.Play("idle", PlayOptions.ForceReset);

			_eventSprite                   = puppet.CreateStaticSprite("face");
			Puppet.puppetState.PlayPercent = RNG.Float; // so that nanokins created on this frame don't all sync perfectly, more individuality!

			// Prepare the default settings for trails using nanokin's colors
			// ----------------------------------------------
			Color c1 = nanokin[LimbType.Body].Asset.Color1;
			Color c2 = nanokin[LimbType.Body].Asset.Color2;

			GetComponentsInChildren(_trails);
			foreach (Trail trail in _trails)
			{
				trail.DefaultRenderSettings = Instantiate(trail.DefaultRenderSettings);
				trail.DefaultRenderSettings.Tint.colorKeys = new[]
				{
					new GradientColorKey(c1, 0),
					new GradientColorKey(c2, 1)
				};
				trail.DefaultRenderSettings.Overlay.colorKeys = new[]
				{
					new GradientColorKey(c1, 0),
					new GradientColorKey(c2, 1)
				};
			}

			// Prepare the sound set
			// ----------------------------------------
			if (nanokin.Head?.Asset != null)
			{
				_soundset   = await Addressables2.LoadHandleAsync(nanokin.Head.Asset.Sounds);
				_hndHurt    = await Addressables2.LoadHandleAsync(nanokin.Head.Asset.Hurt);
				_hndScreech = await Addressables2.LoadHandleAsync(nanokin.Head.Asset.Screech);
				_hndDeath   = await Addressables2.LoadHandleAsync(nanokin.Head.Asset.Death);
				_hndGrunt   = await Addressables2.LoadHandleAsync(nanokin.Head.Asset.Grunt);

				_sfxHurt    = _hndHurt.IsValid() ? _hndHurt.Result : _soundset.Result.Hurt;
				_sfxScreech = _hndScreech.IsValid() ? _hndScreech.Result : _soundset.Result.Screech;
				_sfxDeath   = _hndDeath.IsValid() ? _hndDeath.Result : _soundset.Result.Death;
				_sfxGrunt   = _hndGrunt.IsValid() ? _hndGrunt.Result : _soundset.Result.Grunt;
			}
		}

		public override bool IsSameFacing(Vector3 lookpos)
		{
			if (base.IsSameFacing(lookpos))
				return true;

			float diff = Vector3.Distance(facing, transform.position.Towards(lookpos));
			return diff < 1.5f; // One quarter-action is 1.412

			// float curr = MathUtil.ToWorldAzimuthBlendable(facing);
			// float test = MathUtil.ToWorldAzimuthBlendable(transform.position.Towards(lookpos));

			// const float fuzzing = 0.3f;

			// if (curr < 0.5f) return test < 0.5f + fuzzing;
			// if (curr > 0.5f) return test > 0.5f - fuzzing;

			// return false; // computer very bad.
		}

		private void Update()
		{
			_capsule.height = height;
			_capsule.radius = radius;
			_capsule.center = Vector3.up * height / 2f;
		}


		public override Transform GetAnchorTransform(string id)
		{
			return Puppet.GetNodeTransform(id);
		}

		public override async UniTask<Sprite> GetEventSprite()
		{
			await UniTask.WaitUntil(() => _eventSprite != null);
			return _eventSprite;
		}

		public override async UniTask<GameObject> CreateSilhouette()
		{
			var puppet = new PuppetState(_nanokin.ToPuppetTree(_handles));
			await puppet.AwaitLoading();

			WorldPuppet puppetObject = Instantiate(Puppet);
			puppetObject.SetPuppet(puppet);
			puppetObject.ClearView();
			puppetObject.puppetState.PlayPercent = RNG.Float;
			puppetObject.IsSilhouette            = true;

			// Prepare the default settings for trails using nanokin's colors
			// ----------------------------------------------
			Color c1 = Color.black;
			Color c2 = Color.black;

			GetComponentsInChildren(_trails);
			foreach (Trail trail in _trails)
			{
				trail.DefaultRenderSettings = Instantiate(trail.DefaultRenderSettings);
				trail.DefaultRenderSettings.Tint.colorKeys = new[]
				{
					new GradientColorKey(c1, 0),
					new GradientColorKey(c2, 1)
				};
				trail.DefaultRenderSettings.Overlay.colorKeys = new[]
				{
					new GradientColorKey(c1, 0),
					new GradientColorKey(c2, 1)
				};
			}

			return puppetObject.gameObject;
		}

		// public override int Layer
		// {
		// 	set
		// 	{
		// 		Puppet.puppetState.Render();
		// 		Puppet.gameObject.SetLayerRecursively(value);
		// 	}
		// }
		//

		// public override Vector3 GetNodePosition(string id)
		// {
		// 	return Puppet.GetNodePosition(id);
		// }

#if UNITY_EDITOR
		// private void OnDrawGizmos()
		// {
		// 	// if (Puppet.puppetState == null || Puppet.puppetState.composition == null) return;
		//
		// 	// Rect rect = entity.Puppet.Composition.CalculateBounds().Scaled(MathUtil.PIXEL_TO_WORLD);
		//
		// 	// Transform trans = entity.ViewTransform;
		//
		// 	// Matrix4x4 mat = Handles.matrix;
		// 	// Handles.matrix = Matrix4x4.TRS(trans.position, trans.rotation, trans.localScale);
		//
		// 	// Handles.DrawSolidRectangleWithOutline(rect, ColorsXNA.Indigo.ScaleAlpha(0.3f), Color.black);
		//
		// 	// Handles.matrix = mat;
		// }
#endif
	}
}