using System;
using System.Collections.Generic;
using Anjin.Utils;
using Combat.Data;
using Combat.UI;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Overworld.Cutscenes;
using Pathfinding.Util;
using UnityEngine;
using Util.RenderingElements.Trails;
using Object = UnityEngine.Object;

namespace Combat.Toolkit
{
	public class OverdriveAnim : BattleAnim
	{
		private List<BattleAnim> _actions;
		private Settings         _settings;

		// Resources
		private Trigger         _triggerReceivedProc;
		private TimeScaleVolume _timescale;
		private List<Trail>     _userTrail;
		private GameObject      _sparks;

		// Logic
		private bool  _procced           = false;
		private float _elapsedSinceProc  = -1;
		private float _elapsedSinceStart = -1;
		private bool  _running;

		public OverdriveAnim(Fighter fighter, List<BattleAnim> actions) : base(fighter)
		{
			_actions = actions;
		}

		public override void RunInstant()
		{
			base.RunInstant();

			// Simply run each action
			foreach (BattleAnim act in _actions)
			{
				RunInstant(act);
			}
		}

		public override async UniTask RunAnimated()
		{
			battle.AddPoints(fighter ?? throw new InvalidOperationException(), -new Pointf(op: _actions.Count - 1));
			battle.EnableCombo(fighter, true);
			battle.ZeroCombo(fighter);

			OnInitialize();

			//_timescale.Value = 1.75f;
			_timescale.Value = 1f;

			_running = true;

			CinematicBordersUI.Enable();
			CombatUI.SetOverdriveActive(true);
			ComboUI.StartCombo();
			ComboUI.UpdateCombo(0);

			GameSFX.PlayGlobal(runner.animConfig.adOverdriveActivate);

			await UniTask.Delay(750, cancellationToken: cts.Token).SuppressCancellationThrow();

			for (var i = 0; i < _actions.Count; i++)
			{
				if (cts.IsCancellationRequested)
					break;

				BattleAnim anim = _actions[i];
				anim.CopyContext(this);

				if (i < _actions.Count - 1)
					anim.skipflags = new List<string> { "end" };

				anim.animflags = new List<string> { AnimFlags.Overdrive };
				anim.fighter   = fighter;

				await RunAnimated(anim, false);
			}

			await RunAnimated("Skills/endoverdrive", battle, targeting, false);

			battle.ResetCombo(fighter, false);
			battle.EnableCombo(fighter, false);

			CombatUI.SetOverdriveActive(false);
			CinematicBordersUI.Disable();

			ComboUI.EndCombo().Forget();

			_running = false;

			OnCleanup();
		}

		private void OnProc(TriggerEvent obj)
		{
			_procced          = true;
			_elapsedSinceProc = 0;
		}

		private void OnInitialize()
		{
			_triggerReceivedProc = new Trigger { signal = Signals.receive_proc };
			//battle.AddTrigger(OnProc, _triggerReceivedProc); //TODO: this currently breaks procs for some reason. fix post- publisher demo
			_settings = runner.animConfig.Overdrive;

			// Timescale
			// ----------------------------------------
			var objTimescale = new GameObject("Overdrive Time Scale");
			objTimescale.transform.position = fighter.actor.transform.position;

			SphereCollider sphere = objTimescale.AddComponent<SphereCollider>();
			sphere.isTrigger = true;
			sphere.radius    = 100f;
			_timescale       = objTimescale.AddComponent<TimeScaleVolume>();


			// Trails
			// ----------------------------------------
			_userTrail = ListPool<Trail>.Claim(8);
			fighter.actor.GetComponentsInChildren(_userTrail);
			foreach (Trail trail in _userTrail)
			{
				trail.RenderSettings         = Object.Instantiate(trail.DefaultRenderSettings);
				trail.RenderSettings.Overlay = trail.RenderSettings.Tint;

				// trail.SetLayer(Layers.AboveUI);
				trail.lengthMultiplier = 3;
				trail.Play();
			}

			// Sparks
			// ----------------------------------------
			_sparks = Object.Instantiate(_settings.SparksParticlePrefab, fighter.actor.transform, false);
		}

		public override void Update()
		{
			base.Update();
			if (_running == false) return;

			for (var i = 0; i < _userTrail.Count; i++)
			{
				Trail trail = _userTrail[i];
				trail.lengthMultiplier = _settings.BaseTrailLength.Evaluate(_elapsedSinceStart);
				if (_procced)
					trail.lengthMultiplier = _settings.TrailLengthAfterProc.Evaluate(_elapsedSinceProc);
			}

			_elapsedSinceStart += Time.deltaTime;
			_elapsedSinceProc  += Time.deltaTime;

			//_timescale.Value = _settings.BaseSpeed.Evaluate(_elapsedSinceStart);
			//if (_procced)
			//	_timescale.Value *= _settings.SpeedAfterProc.Evaluate(_elapsedSinceProc);
		}

		private void OnCleanup()
		{
			battle.RemoveTrigger(_triggerReceivedProc);

			// Trails
			// ----------------------------------------

			foreach (Trail trail in _userTrail)
			{
				trail.lengthMultiplier = 1;
				// trail.SetLayer(Layers.Default);
				trail.StopProgressive();
			}

			ListPool<Trail>.Release(ref _userTrail);

			// TimeScale
			// ----------------------------------------
			Object.Destroy(_timescale.gameObject);

			// Sparks
			// ----------------------------------------
			Object.Destroy(_sparks);
		}

		[Serializable]
		public class Settings
		{
			public GameObject SparksParticlePrefab;

			public AnimationCurve BaseSpeed            = AnimationCurve.EaseInOut(0, 0.85f, 1.5f, 1.575f);
			public AnimationCurve BaseTrailLength      = AnimationCurve.EaseInOut(0, 1f, 0.875f, 2.8f);
			public AnimationCurve SpeedAfterProc       = AnimationCurve.EaseInOut(0, 0.75f, 1.75f, 1.15f);
			public AnimationCurve TrailLengthAfterProc = AnimationCurve.EaseInOut(0, 0.25f, 1.6f, 4f);
		}
	}
}