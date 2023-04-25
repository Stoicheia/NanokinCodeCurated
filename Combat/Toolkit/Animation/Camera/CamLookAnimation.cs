using System;
using Anjin.Scripting;
using Anjin.Util;
using Anjin.Utils;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine;

namespace Combat.Toolkit.Camera
{
	public class CamLookAnimation : CoroutineManaged
	{
		public readonly Table config;
		public readonly bool  separated;

		// private float       _speed = 1;

		private ArenaCamera     _arenaCam;
		private MotionBehaviour _bodyMotion;
		private MotionBehaviour _lookMotion;
		private bool            _active;

		public CamLookAnimation(DynValue dv, float duration = 0.5f, Ease ease = Ease.InOutSine)
		{
			config = new Table(Lua.envScript)
			{
				[1] = MotionAPI.Tween(duration, ease),
				[2] = dv
			};
		}

		public CamLookAnimation(Table point, bool ext = false)
		{
			config    = point;
			separated = ext;
		}

		public override bool Active => _active;

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			if (!_active) return true;

			if (!separated)
				return _bodyMotion.state != MotionBehaviour.State.Main;

			return (_bodyMotion == null || _bodyMotion.state != MotionBehaviour.State.Main) &&
			       (_lookMotion == null || _lookMotion.state != MotionBehaviour.State.Main);
		}

		public override void OnStart()
		{
			base.OnStart();

			_arenaCam   = costate.battle.camera;
			_bodyMotion = null;
			_lookMotion = null;

			if (!separated)
			{
				_arenaCam.SetMode(ArenaCamera.BodyLookModes.Synchronized, out MotionBehaviour bm, out MotionBehaviour _);

				_bodyMotion        = bm;
				_bodyMotion.battle = costate.battle.battle;
				_bodyMotion.procs  = costate.procs;

				_bodyMotion.ConfigureTB(config);

				ProcessMotion(_bodyMotion.mainPath);
				ProcessMotion(_bodyMotion.stayPath);
				ProcessMotion(_bodyMotion.exitPath);

				if (_bodyMotion.stayPath == null)
					_bodyMotion.stayPath = _bodyMotion.mainPath;

				_bodyMotion.Play();
				_active = true;
			}
			else
			{
				bool hasBody = config.TryGet("body", out Table bodyConfig);
				bool hasLook = config.TryGet("look", out Table lookConfig);

				if (hasBody || hasLook)
				{
					_arenaCam.SetMode(ArenaCamera.BodyLookModes.Separate, out MotionBehaviour bm, out MotionBehaviour lm);

					if (hasBody)
					{
						_bodyMotion        = bm;
						_bodyMotion.battle = costate.battle.battle;
						_bodyMotion.procs  = costate.procs;

						_bodyMotion.ConfigureTB(bodyConfig);

						ProcessMotion(_bodyMotion.mainPath);
						ProcessMotion(_bodyMotion.stayPath);
						ProcessMotion(_bodyMotion.exitPath);

						if (_bodyMotion.stayPath == null)
							_bodyMotion.stayPath = _bodyMotion.mainPath;

						_bodyMotion.Play();
					}

					if (hasLook)
					{
						_lookMotion        = lm;
						_lookMotion.battle = costate.battle.battle;
						_lookMotion.procs  = costate.procs;

						_lookMotion.ConfigureTB(lookConfig);

						ProcessMotion(_lookMotion.mainPath);
						ProcessMotion(_lookMotion.stayPath);
						ProcessMotion(_lookMotion.exitPath);

						if (_lookMotion.stayPath == null)
							_lookMotion.stayPath = _lookMotion.mainPath;

						_lookMotion.Play();
					}

					_active = true;
				}
			}
		}

		private void ProcessMotion([CanBeNull] MotionPath motion)
		{
			if (motion == null) return;

			GameObject arena = costate.battle.arena.gameObject;

			for (var i = 0; i < motion.waypoints.Count; i++)
			{
				MotionPoint wp = motion.waypoints[i];
				WorldPoint2 tg = wp.target;

				switch (tg.Type)
				{
					case WorldPoint2.Types.Point:
						if (tg.P1.gameobject == arena)
						{
							tg.P1.offset     = costate.battle.arena.CameraCenterOffset;
							tg.P1.offsetMode = WorldPoint.OffsetMode.World;

							// 	_lookOffset.value = _arenacam.LookPoint.position - _arenacam.BodyPoint.position;
							motion.waypoints[i] = wp;
						}

						break;

					case WorldPoint2.Types.Midpoint:
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			base.OnEnd(forceStopped, skipped);

			if (_bodyMotion)
				_bodyMotion.Stop();

			if (_lookMotion)
				_lookMotion.Stop();
		}
	}
}