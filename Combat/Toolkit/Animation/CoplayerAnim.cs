using System.Linq;
using Anjin.Cameras;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Components;
using Combat.Data;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine;

namespace Combat.Toolkit
{
	/// <summary>
	/// A battle animation which plays a function using a coplayer.
	/// </summary>
	public class CoplayerAnim : BattleAnim
	{
		[CanBeNull] public object[]          arguments;
		[CanBeNull] public CoroutineInstance coroutine;

		private          Table   _env;
		private readonly string  _functionName;
		private readonly Closure _function;

		private Coplayer  _coplayer;
		private bool      _active;
		private ProcTable _proctable;

		public AnimSM animsm;
		public LuaEnv env;

		protected override bool AnimationActive => _active && !cts.IsCancellationRequested;

		public CoplayerAnim(Table env, string functionName, [CanBeNull] object[] arguments = null)
		{
			_env           = env;
			_functionName  = functionName;
			this.arguments = arguments;
		}

		public CoplayerAnim(Table env, Closure function, [CanBeNull] object[] arguments = null)
		{
			_env           = env;
			_function      = function;
			this.arguments = arguments;
		}

		public CoplayerAnim(string scriptname, string function, [CanBeNull] object[] parameters = null)
		{
			CreateEnv(scriptname);
			_functionName = function;
		}

		public CoplayerAnim(Table env, string functionName, [NotNull] Fighter fighter, [CanBeNull] object[] extraArgs = null) : base(fighter)
		{
			_env          = env;
			_functionName = functionName;
			SetFighterArgs(fighter, extraArgs);
		}

		public CoplayerAnim(string scriptName, string functionName, Fighter fighter, [CanBeNull] object[] extraArgs = null) : base(fighter)
		{
			CreateEnv(scriptName);
			_functionName = functionName;
			SetFighterArgs(fighter, extraArgs);
		}

		private void CreateEnv(string scriptname)
		{
			LuaChangeWatcher.BeginCollecting();
			_env = Lua.NewScript(scriptname);
			LuaUtil.LoadBattleRequires(_env);
			LuaChangeWatcher.EndCollecting(this, () => CreateEnv(scriptname));
		}

		private void SetFighterArgs(Fighter fighter, [CanBeNull] object[] extraArgs = null)
		{
			if (extraArgs != null)
			{
				arguments    = new object[extraArgs.Length + 1];
				arguments[0] = fighter;
				for (var i = 0; i < extraArgs.Length; i++)
				{
					arguments[1 + i] = extraArgs[i];
				}
			}
			else
			{
				arguments = new object[] { fighter };
			}
		}

		public override async UniTask RunAnimated()
		{
			OnStart();
			await UniTask.WaitUntil(() => !AnimationActive);
			OnStop();
		}

		private void OnStart()
		{
			if (_function == null && (_functionName == null || _env == null))
			{
				DebugLogger.LogError("Either a function or table+name is required to run a CoplayerAnimation.", LogContext.Coplayer, LogPriority.High);
				RunInstant();
				return;
			}

			this.LogVisual("--", $"OnStart (me={fighter}, procs={string.Join(",", procs.Select(p => p.ID))})");

			_proctable = new ProcTable(procs);

			// Setup coroutine
			// ----------------------------------------
			coroutine = _function != null
				? Lua.CreateCoroutine(_env, _function, arguments)
				: Lua.CreateCoroutine(_env, _functionName, arguments);

			if (coroutine == null)
			{
				this.LogError("Couldn't get coroutine.");
				return;
			}

			coroutine.onBeforeResume += () =>
			{
				_env[LuaEnv.OBJ_BATTLE] = battle;
				_env[LuaEnv.OBJ_ARENA]  = battle.arena;
				_env[LuaEnv.OBJ_PROCS]  = _proctable;
				_env[LuaEnv.OBJ_ANIMSM] = animsm;
				animsm?.Start(env);
			};

			coroutine.onAfterResume += () =>
			{
				_env[LuaEnv.OBJ_ANIMSM] = null;
				_env[LuaEnv.OBJ_PROCS]  = null;
				animsm?.End();
			};

			_active = true;
			coroutine.onEnding += () =>
			{
				_active = false;
			};

			// Setup coplayer
			// ----------------------------------------
			_coplayer = Lua.RentPlayer();

			// _coplayer.baseState.SetFighterSelf(fighter);
			// _coplayer.baseState.selfObject = fighter?.actor?.gameObject;
			_coplayer.baseState.battle = runner;
			_coplayer.baseState.procs  = _proctable;

			_coplayer.afterStoppedTmp = () =>
			{
				_active = false;
			};

			if (skipflags != null) _coplayer.skipflags.AddRange(skipflags);
			if (animflags != null) _coplayer.animflags.AddRange(animflags);

			// ----------------------------------------
			// Proc setup
			foreach (Proc proc in procs)
			{
				this.LogVisual("--", $"Hooking {proc}");
				proc.onAnimating = OnAnimatingProc;
			}


			// Occlusion setup
			SetOccContext();

			// Invert the camera azimuth if we're on the player side.
			if (fighter != null && fighter.actor != null)
			{
				Vector3 cam = GameCams.Live.UnityCam.transform.right.Horizontal();
				runner.camera.SetFlip(Vector3.Dot(cam, fighter.home?.facing ?? fighter.actor.facing) < 0);
			}

			// Start playing!!!
			// ----------------------------------------
			_coplayer.Play(_env, coroutine).Forget();
		}

		private void OnStop()
		{
			_active = false;

			if (_proctable != null)
			{
				// Fire remaining procs (we may want an option to make this optional?)
				while (_proctable.PopNext(out Proc p))
				{
					if (!p.fired)
					{
						DebugLogger.LogWarning($"Proc {p} was not fired during CoplayerAction (env: {_env.GetEnvName()}, func: {(object)_function ?? _functionName}). Applying now...", LogContext.Combat, LogPriority.Low);
						battle.Proc(p);
					}
				}

				// Unhook procs (just for good measure, each proc shouldn't be fired more than once anyway)
				foreach (Proc proc in procs)
					proc.onAnimating = null;
			}

			if (_coplayer != null)
			{
				_coplayer.Stop();
				Lua.ReturnPlayer(ref _coplayer);
			}

			ClearOccContext();
			LuaChangeWatcher.ClearWatches(this);
		}

		private void SetOccContext()
		{
			foreach (Proc proc in procs)
			{
				if (proc.dealer != null)
					ContextualOccluderSystem.AddContext(proc.dealer.actor.gameObject);

				foreach (Fighter victim in proc.fighters)
				{
					ContextualOccluderSystem.AddContext(victim.actor.gameObject);
				}
			}
		}

		private void ClearOccContext()
		{
			foreach (Proc proc in procs)
			{
				if ((proc.dealer != null) && (proc.dealer.actor != null))
				{
					ContextualOccluderSystem.RemoveContext(proc.dealer.actor.gameObject);
				}
				else
				{
					ContextualOccluderSystem.RemoveContext(null);
				}

				foreach (Fighter victim in proc.fighters)
				{
					if (victim.actor != null)
					{
						ContextualOccluderSystem.RemoveContext(victim.actor.gameObject);
					}
					else
					{
						ContextualOccluderSystem.RemoveContext(null);
					}
				}
			}
		}

		private void OnAnimatingProc(ProcContext proc, Coplayer procplayer, ProcAnimation anim)
		{
			if (animflags != null)
				procplayer.animflags.AddRange(animflags);

			if (_active) // A proc could fire _after_ the CoplayerAnimation has ended (e.g. projectile that moves too slow)
			{
				// Base off of current coplayer state
				procplayer.baseState = _coplayer.state;

				// Add to our subplayers
				_coplayer.subplayers.Add(procplayer);
			}
		}
	}
}