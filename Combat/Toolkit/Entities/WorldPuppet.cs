using System;
using Anjin.Actors;
using Anjin.Util;
using API.PropertySheet;
using API.PropertySheet.Runtime;
using API.Puppets.Components;
using Combat.Data.VFXs;
using JetBrains.Annotations;
using Puppets;
using Puppets.Render;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using Util;
using Util.Animation;
using Util.PropertyStores;

namespace Combat.Entities
{
	public abstract class WorldPuppet : PuppetComponent, IPuppetLinked, IPuppetAnimable, INameAnimable
	{
		// Component Configuration
		// ----------------------------------------
		[SerializeField, SceneObjectsOnly]
		private GameObject DropShadowDecal;

		// Runtime State
		// ----------------------------------------
		[NonSerialized, ShowInInspector] public AnimationStates animState = AnimationStates.Idle;

		[NonSerialized] public PuppetPlayer puppetPlayer;
		[NonSerialized] public ActorBase    actor;
		[NonSerialized] public VFXManager   vfx;

		private PuppetAnimatorVFX _puppetAnimatorVFX = new PuppetAnimatorVFX();
		private Vector3           _initialShadowScale;

		/// <summary>
		/// Get the position of the puppet's center visually.
		/// </summary>
		protected abstract IRenderStrategy RenderStrategy { get; }

		public enum AnimationStates
		{
			/// <summary>
			/// Playing the idle animation.
			/// </summary>
			Idle,

			/// <summary>
			/// Playing an animation by some external system, either name-based or a puppetanimation.
			/// </summary>
			External,

			/// <summary>
			/// Playing an animation through a VFX which overrides everything else, either name-based or a puppetanimation.
			/// </summary>
			VFXOverride
		}

		/// <summary>
		/// Get the position of the puppet's center visually.
		/// </summary>
		protected abstract Vector3 PuppetCenter { get; }

		// TODO this has to go, bad separation of concern
		protected bool isSilhouette;

		private PuppetAnimation _lastVFXAnimation;

		public virtual bool IsSilhouette
		{
			get => isSilhouette;
			set => isSilhouette = value;
		}

		protected override void Awake()
		{
			base.Awake();

			actor = gameObject.GetOrAddComponent<ActorBase>();
			vfx   = gameObject.GetOrAddComponent<VFXManager>();

			puppetPlayer            = gameObject.GetOrAddComponent<PuppetPlayer>();
			puppetPlayer.sceneSpace = new ActorSceneSpace(actor);

			if (DropShadowDecal)
			{
				DropShadowDecal.SetActive(false);
				_initialShadowScale = DropShadowDecal.transform.localScale;
			}
		}

		protected override void OnPuppetChanged([CanBeNull] PuppetState state)
		{
			bool hasActor = actor != null;
			if (hasActor)
			{
				actor.radius = 0;
				actor.height = 0;
				actor.center = transform.position;
			}

			if (DropShadowDecal != null)
			{
				DropShadowDecal.SetActive(state != null);
			}

			if (state != null)
			{
				state.renderStrategy = RenderStrategy;
				state.Render();

				if (hasActor)
				{
					actor.center = PuppetCenter;
					actor.radius = puppetState.composition.CalculateBounds().width / 2f * MathUtil.PIXEL_TO_WORLD;
					actor.height = puppetState.composition.CalculateBounds().height * MathUtil.PIXEL_TO_WORLD;
				}

				SetIdle();
			}
		}


		protected override void Update()
		{
			base.Update();

			if (puppetState?.tree?.Root?.State != null)
			{
				actor.center = PuppetCenter;

				puppetState.PlaySpeed = timeScale.current;
				puppetPlayer.speed    = timeScale.current;

				UpdateDropShadow();
			}
		}

		protected virtual void LateUpdate()
		{
			UpdateAnimationOverride();
			UpdateDropShadow();
		}

		private void UpdateAnimationOverride()
		{
			if (vfx.state.animSet != null)
			{
				animState = AnimationStates.VFXOverride;

				Profiler.BeginSample("WorldPuppet apply VFX animSet");
				puppetState.Play(vfx.state.animSet);
				puppetPlayer.Stop();
				Profiler.EndSample();
			}
			else if (vfx.state.puppetSet != null)
			{
				animState         = AnimationStates.VFXOverride;
				_lastVFXAnimation = vfx.state.puppetSet;

				Profiler.BeginSample("WorldPuppet apply VFX puppetSet");
				if (_lastVFXAnimation == vfx.state.puppetSet && puppetPlayer.current != vfx.state.puppetSet)
				{
					_lastVFXAnimation = vfx.state.puppetSet;
					puppetPlayer.Play(vfx.state.puppetSet, vfx.state.puppetSetMarkerStart, vfx.state.puppetSetMarkerEnd, PlayOptions.Continue);
				}
				else
				{
					puppetPlayer.Play(vfx.state.puppetSet, vfx.state.puppetSetMarkerStart, vfx.state.puppetSetMarkerEnd, PlayOptions.Continue);
				}

				Profiler.EndSample();
			}
			else if (animState == AnimationStates.VFXOverride)
			{
				// Clear the VFX override
				_lastVFXAnimation = null;
				SetIdle();
			}
		}

		private void UpdateDropShadow()
		{
			if (DropShadowDecal != null && puppetState != null)
			{
				DropShadowDecal.transform.position = transform.position +
				                                     actor.facing * (puppetState.tree.Root.State.Layout.position.x * (1 / 32f)) +
				                                     Vector3.up * 0.25f;

				DropShadowDecal.transform.localScale = new Vector3(actor.radius * 2, _initialShadowScale.y, actor.radius * 2) * vfx.state.opacity;
			}
		}

		private void SetIdle()
		{
			// Undo puppet state overrides
			puppetState.Layout = PuppetLayout.Identity;
			vfx.Remove(_puppetAnimatorVFX);

			// For the limbs, the idle anim will automatically clear the rest
			animState = AnimationStates.Idle;
			puppetState.Play("idle", PlayOptions.ForceReset);

			puppetPlayer.FreeExtensions();
		}

		public AnimationPlayResult Play(PlayAnimations anim, PlayOptions options = PlayOptions.Continue)
		{
			UpdateAnimationOverride();

			if (animState == AnimationStates.Idle || animState == AnimationStates.External)
			{
				if (anim.reset)
				{
					SetIdle();
					return AnimationPlayResult.OK;
				}
				else
				{
					AnimationPlayResult result = puppetState.Play(anim, options);
					if (result.IsPlaying())
					{
						animState = AnimationStates.External;
						puppetPlayer.FreeExtensions();
					}

					return result;
				}
			}

			return AnimationPlayResult.AnimatorUnavailable;
		}

		public void Play(PuppetAnimation animation, string from, string to)
		{
			UpdateAnimationOverride();
			if (animState == AnimationStates.Idle || animState == AnimationStates.External)
			{
				animState = AnimationStates.External;
				puppetPlayer.Play(animation, from, to);
			}
		}

	#region Puppet Anim

		public virtual void OnPuppetAnimLinked(PuppetAnimation propertySheet, int elementID) { }

		public virtual void OnPuppetAnimControled(ISceneSpace sceneSpace)
		{
			puppetState.Paused = true;
			vfx.Add(_puppetAnimatorVFX);

			if (animState == AnimationStates.Idle)
			{
				this.LogWarn("STRANGE BEHAVIOR! WorldPuppet implements IPuppetAnimable but it is not being used, PuppetPlayer was probably used directly.");
				animState = AnimationStates.External;
			}
		}

		public virtual void OnPuppetAnimFreed()
		{
			puppetState.Paused = false;
			if (animState == AnimationStates.External)
			{
				// Return to idle
				SetIdle();
			}
			else if (animState == AnimationStates.VFXOverride)
			{
				// Hold the last frame
			}
		}

		public virtual void OnPuppetAnimProperties(ISceneSpace sceneSpace, [NotNull] PropertyStore properties)
		{
			if (properties.TryGet(StandardProperties.Position, out Vector2 pos))
			{
				puppetState.Layout = puppetState.Layout.SetPosition(pos);
			}

			if (properties.TryGet(StandardProperties.Tint, out Color tint))
			{
				_puppetAnimatorVFX.tint = tint;
			}
		}

		public virtual void OnPuppetAnimFrame(ISceneSpace sceneSpace, int idxFrame) { }

		public virtual void OnPuppetAnimExit(ISceneSpace sceneSpace) { }

	#endregion

		public virtual void ClearView() { }

		[CanBeNull] public abstract Transform GetNodeTransform(string id);

		public abstract Vector3 GetNodePosition(string id);


		/// <summary>
		/// This VFX is used to apply properties of the current puppet animation to the puppet.
		/// </summary>
		public class PuppetAnimatorVFX : VFX
		{
			public Color tint = Color.white;

			public override Color Tint => tint;

			public override bool IsActive => true;
		}
	}
}