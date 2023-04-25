using System;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Combat.Skills.Generic;
using Drawing;
using MoonSharp.Interpreter;
using Overworld.Controllers;
using Overworld.Tags;
using UnityEngine;
using UnityEngine.Playables;
using Util;
using Util.Components;
using Util.Components.Timers;
using Util.Components.VFXs;
using Util.Extensions;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Map {

	[LuaUserdata]
	public class CannonProp : AnjinBehaviour, IHitHandler<SwordHit> {

		public bool RegisterInLevelTable = false;
		public bool ReorientBarrelOnAwake = true;

		public float             FireTime;
		public PlayableDirector  Director;
		public HUDElementSpawner FireIconSpawner;

		public Transform        Target;
		public Transform        FirePoint;
		public Transform        BarrelPivot;
		public float            Height = 10;

		public Option<float> FireSpeed;

		public FollowParabola P_CannonBall;

		public ParticleSystem		FX_BarrelSmoke;
		public OverworldFX	FX_HitTarget;

		[NonSerialized, ShowInPlay]
		public  bool Firing;

		[ShowInPlay] private TimeMarkerSystem _markerSystem;
		[ShowInPlay] private ValTimer         _fireTimer;
		[ShowInPlay] private FollowParabola   _cannonBall;

		[NonSerialized] public Closure on_fire;
		[NonSerialized] public Closure on_hit;

		[SerializeField]
		private bool _canFire = true;

		public bool CanFire {
			get => _canFire;
			set {
				_canFire = value;
			}
		}

		private void Awake()
		{
			_markerSystem           = Director.gameObject.GetOrAddComponent<TimeMarkerSystem>();
			_markerSystem.pauseMode = TimeMarkerSystem.PauseMode.SetSpeedToZero;

			ReorientBarrel();
		}

		private void ReorientBarrel()
		{
			if (ReorientBarrelOnAwake && BarrelPivot && Target && FirePoint) {
				Vector3 pos1 = MathUtil.EvaluateParabola(BarrelPivot.transform.position, Target.transform.position, Height, 0);
				Vector3 pos2 = MathUtil.EvaluateParabola(BarrelPivot.transform.position, Target.transform.position, Height, 0.01f);
				Vector3 dir  = (pos2 - pos1).normalized;

				float angle = 360 - MathUtil.Angle(dir.xy()) + 90;

				BarrelPivot.transform.localRotation = Quaternion.Euler(0, 0, angle);

			}
		}

		private async void Start()
		{
			//PrefabPool.pri(P_CannonBall);
			await GameController.TillLuaIntialized();
			//await Lua.initTask;
			if(RegisterInLevelTable)
				Lua.RegisterToLevelTable(this);
		}

		private void Update()
		{
			if (GameController.IsWorldPaused) return;

			//nocheckin
			ReorientBarrel();

			if (_cannonBall && !_cannonBall.Following) {

				if (FX_HitTarget) {
					OverworldFX fx = PrefabPool.Rent(FX_HitTarget, null);
					fx.IsPooled = true;
					fx.transform.position       = _cannonBall.transform.position;
					fx.Play();
				}

				PrefabPool.Return(_cannonBall);
				_cannonBall = null;
				//Debug.Log("Ball hit its target");

				if(on_hit != null) Lua.RunPlayer(Lua.LevelTable, on_hit);
			}

			if (Firing) {

				if (_markerSystem.reachedMarker) {
					if(!FX_BarrelSmoke.isPlaying)
						FX_BarrelSmoke.Play();

					if(_fireTimer.Tick()) {
						_markerSystem.Resume();
						_markerSystem.reachedMarker = false;
						FX_BarrelSmoke.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

						if(_cannonBall == null && P_CannonBall != null) {
							_cannonBall = PrefabPool.Rent(P_CannonBall, null);

							_cannonBall.Time      = 0;
							_cannonBall.Start     = FirePoint.transform.position;
							_cannonBall.End       = Target.transform.position;
							_cannonBall.Height    = Height;
							_cannonBall.Following = true;

							if (FireSpeed.IsSet)
								_cannonBall.Speed = FireSpeed;

							_cannonBall.Update();

							foreach (var ps in _cannonBall.GetComponentsInChildren<ParticleSystem>(true)) {
								ps.Play();
							}
						}

						if(on_fire != null) Lua.RunPlayer(Lua.LevelTable, on_fire);
					}
				}

				if (_markerSystem.reachedEnd) {
					Firing = false;
					_markerSystem.Reset();
				}
			}

			if (FireIconSpawner) {
				FireIconSpawner.SetActive(CanFire && !Firing);
			}

		}

		public void TM_OnFire()
		{

		}

		public void OnHit(SwordHit hit)
		{
			//Debug.Log("Test");

			if (CanFire && !Firing) {
				Firing = true;
				_fireTimer.Set(FireTime);
				Director.Play();
				_markerSystem.SetTargetMarker(0);
			}

		}

		public bool IsHittable(SwordHit hit) => !Firing && CanFire;

		public override void DrawGizmos()
		{
			if(Target != null && FirePoint != null) {
				Draw.editor.Parabola(FirePoint.transform.position, Target.position, Height, ColorsXNA.Goldenrod);
			}
		}
	}
}