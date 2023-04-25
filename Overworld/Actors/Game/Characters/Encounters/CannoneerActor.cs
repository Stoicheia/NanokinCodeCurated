using Anjin.Actors.States;
using Anjin.Nanokin.Map;
using Anjin.Nanokin.Park;
using API.Spritesheet.Indexing.Runtime;
using DG.Tweening;
using Overworld.Controllers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class CannoneerVFXOrdinalInfo
	{
		public Vector3 SmokeLocalPosition;
		public Vector3 SmokeLocalEulers;
		[Space]
		public Vector3 ExplosionLocalPosition;
		public Vector3 ExplosionLocalEulers;
	}

	public class CannoneerActor : ActorKCC, IEncounterActor
	{
		public const int STATE_IDLE = 0;
		public const int STATE_ALERT = 1;
		public const int STATE_WAIT_FOR_NEXT_SHOT = 2;
		public const int STATE_RECHARGE = 0;

		[SerializeField] private Transform FirePoint;
		[SerializeField] private float Height = 2;
		[SerializeField] private float FiringSpeed = 500;

		[SerializeField] private CannoneerProjectile P_CannonBall;

		[SerializeField] private ParticleSystem FX_BarrelSmoke;
		[SerializeField] private ParticleSystem FX_CannonFire;

		[SerializeField] private AudioDef		SFX_CannonFire;

		[SerializeField] private GameObject view;

		[System.NonSerialized, ShowInPlay] public Stunnable Stunnable;

		//[ShowInPlay] private CannoneerProjectile cannonball;

		//[System.NonSerialized] public Closure on_fire;

		[SerializeField] private MockState _idleState;       // Wait for a signal to leap.
		[SerializeField] private MockState _alertState;       // Wait for a signal to leap.
		[SerializeField] private MockState _waitingState;       // Wait for a signal to leap.
		[SerializeField] private MockState _rechargeState;       // Wait for a signal to leap.

		private new ActorRenderer renderer;

		public Dictionary<Direction8, CannoneerVFXOrdinalInfo> OrdinalTable;

		protected override void Awake()
		{
			base.Awake();

			Stunnable = GetComponent<Stunnable>();

			renderer = GetComponent<ActorRenderer>();
		}

		public bool IsAggroed
		{
			get
			{
				if (activeBrain is CannoneerBrain lbrain)
				{
					switch (lbrain.State)
					{

						/*case RaptorBrain.States.Idle:    break;
						case RaptorBrain.States.Return:  break;
						case RaptorBrain.States.Meander: break;*/

						case CannoneerBrain.States.Alert:
						case CannoneerBrain.States.WaitForNextShot:
						case CannoneerBrain.States.Recharge:
							return true;

						default: return false;
					}
				}

				return false;
			}
		}

		public override float MaxJumpHeight => 0;

		public override StateKCC GetDefaultState() => _idleState;

		protected override StateKCC GetNextState(ref Vector3 currentVelocity, float deltaTime)
		{
			StateKCC state = _idleState;

			if (activeBrain is CannoneerBrain lbrain)
			{
				if (lbrain.FireCannonball)
				{
					lbrain.FireCannonball = false;

					if (!FX_CannonFire.isPlaying)
					{
						FX_CannonFire.Play(true);
					}

					GameSFX.PlayGlobal(SFX_CannonFire, this);

					Vector3 heading = (transform.rotation * Vector3.forward);
					heading.y = 0;

					CannoneerProjectile cannonball = PrefabPool.Rent(P_CannonBall, null);
					cannonball.transform.position = FirePoint.transform.position;

					cannonball.Initialize(heading, FiringSpeed);

					transform.DOMove(transform.position + (heading * -0.75f), 0.2f).SetLoops(2, LoopType.Yoyo);
				}
				else if (lbrain.ShowSmoke)
				{
					lbrain.ShowSmoke = false;

					if (!FX_BarrelSmoke.isPlaying)
					{
						FX_BarrelSmoke.Play(true);
					}
				}
				else if (lbrain.HideSmoke)
				{
					lbrain.HideSmoke = false;

					if (FX_BarrelSmoke.isPlaying)
					{
						FX_BarrelSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
					}
				}

				switch (lbrain.State)
				{

					/*case RaptorBrain.States.Idle:    break;
					case RaptorBrain.States.Return:  break;
					case RaptorBrain.States.Meander: break;*/

					case CannoneerBrain.States.Alert:
						state = _alertState;
						break;
					case CannoneerBrain.States.WaitForNextShot:
						state = _waitingState;
						break;
					case CannoneerBrain.States.Recharge:
						state = _rechargeState;
						break;
					default:
						state = _idleState;
						break;
				}
			}

			//if (inputs.swordPressed)
			//{
			//	if (!FX_CannonFire.isPlaying)
			//	{
			//		FX_CannonFire.Play(true);
			//	}
			//	else
			//	{
			//		FX_CannonFire.Stop(true, ParticleSystemStopBehavior.StopEmitting);

			//		FX_CannonFire.Play(true);
			//	}

			//	GameSFX.PlayGlobal(SFX_CannonFire, this);

			//	Vector3 heading = (transform.rotation * Vector3.forward);
			//	heading.y = 0;

			//	CannoneerProjectile cannonball = PrefabPool.Rent(P_CannonBall, null);
			//	cannonball.transform.position = FirePoint.transform.position;
			//	cannonball.Initialize(heading, FiringSpeed);

			//	transform.DOMove(transform.position + (heading * -0.75f), 0.2f).SetLoops(2, LoopType.Yoyo);
			//}
			//else if (inputs.glidePressed)
			//{
			//	if (!FX_BarrelSmoke.isPlaying)
			//	{
			//		FX_BarrelSmoke.Play(true);
			//	}
			//	else
			//	{
			//		FX_BarrelSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);

			//		FX_BarrelSmoke.Play(true);
			//	}
			//}
			//else if (inputs.divePressed)
			//{
			//	if (FX_BarrelSmoke.isPlaying)
			//	{
			//		FX_BarrelSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
			//	}
			//}

			return state;
		}

		protected override void RegisterStates()
		{
			RegisterState(STATE_IDLE, _idleState);
			RegisterState(STATE_ALERT, _alertState);
			RegisterState(STATE_WAIT_FOR_NEXT_SHOT, _waitingState);
			RegisterState(STATE_RECHARGE, _rechargeState);
		}

		public override void UpdateRenderState(ref RenderState state)
		{
			var cannoneerBrain = activeBrain as CannoneerBrain;

			if (stateChanged || ((cannoneerBrain != null) && (cannoneerBrain.BackToIdle)))
			{
				if (cannoneerBrain != null)
				{
					cannoneerBrain.BackToIdle = false;
				}

				state = new RenderState(AnimID.Stand);
				state.loops = true;
				state.animRepeats = 0;
			}

			if (_alertState)
			{
				state = new RenderState(AnimID.Light);
				state.loops = false;
				state.animRepeats = 1;
			}
			else if (_waitingState || _rechargeState)
			{
				if ((cannoneerBrain != null) && (!cannoneerBrain.PlayFireAnimInRecharge))
				{
					return;
				}

				state = new RenderState(AnimID.Fire);
				state.loops = false;
				state.animRepeats = 1;
			}
		}

		public void LoadCannonball()
		{
			//if (FirePoint.transform.childCount == 0)
			//{
				CannoneerProjectile cannonball = PrefabPool.Rent(P_CannonBall, null);
				//cannonball.transform.SetParent(FirePoint.transform);
				//cannonball.transform.localPosition = Vector3.zero;
				//cannonball.transform.localEulerAngles = Vector3.zero;
				cannonball.gameObject.SetActive(false);
			//}
		}

		protected override void Update()
		{
			base.Update();

			SpritePlayer player = renderer.Animable.player;

			if (player != null && OrdinalTable.ContainsKey(player.animDir))
			{
				var info = OrdinalTable[player.animDir];

				FX_CannonFire.transform.localPosition = info.ExplosionLocalPosition;
				//FX_CannonFire.transform.localEulerAngles = info.ExplosionLocalEulers;

				FX_BarrelSmoke.transform.localPosition = info.SmokeLocalPosition;
				//FX_BarrelSmoke.transform.localEulerAngles = info.SmokeLocalEulers;
			}
		}
	}
}
