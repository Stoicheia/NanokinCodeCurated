using Anjin.Scripting;
using API.PropertySheet;
using API.PropertySheet.Elements;
using API.PropertySheet.Runtime;
using Combat.Data;
using MoonSharp.Interpreter;
using UnityEngine;
using Util.Animation;
using Util.PropertyStores;

namespace Combat.Toolkit
{
	/// <summary>
	/// Play a puppet animation on an object with a PuppetPlayer a TimeScalable component.
	/// </summary>
	[LuaUserdata]
	public class CoroutinePuppetAnim : CoroutineManagedObj
	{
		public PuppetAnimation animation;

		// config
		public string from = "start";
		public string to   = "end";
		public bool   holding;
		public bool   procing = true;
		public bool   NoStop  = false; // TODO remove this, useless

		private PuppetPlayer    _pupPlayer;
		private bool            _ended;
		private IPuppetAnimable _pupAnimable;

		public override bool Active => !_ended;

		public override float ReportedProgress => _pupPlayer.Progress; // TODO fix percent in player. (when playing segment)

		public override float ReportedDuration => animation.Data.FrameCount * animation.Data.FrameDuration;

		public CoroutinePuppetAnim(GameObject obj, PuppetAnimation animation, Table conf) : base(obj)
		{
			this.animation = animation;
			NoStop         = false;

			from = conf.TryGet("start", from);
			if (conf.TryGet("seg", out string markerseg))
			{
				//from = markerseg;
				to = this.animation.Data.MarkerTimeline.GetNextMarker(this.animation.Data, markerseg);
			}

			conf.TryGet("hold", out holding, holding);
			conf.TryGet("proc", out procing, procing);
		}

		public override void OnStart()
		{
			if (animation == null)
			{
				Debug.Log("WARNING: No property sheet to play.");
				_ended = true;
				return;
			}

			_pupPlayer   = self.GetComponent<PuppetPlayer>();
			_pupAnimable = self.GetComponent<IPuppetAnimable>();
			if (_pupPlayer != null)
			{
				_pupAnimable.Play(animation, from, to);
				_pupPlayer.ElementEntered += OnElementEnter;
				_pupPlayer.Ending         += OnPlayerOnEnding;

				// TODO link proc and remove ElementEntered
				// _pupPlayer.linker.LinkPuppet();
			}
			else
			{
				_ended = true;
			}
		}

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			base.OnEnd(forceStopped, skipped);

			if (_pupPlayer && _pupPlayer.IsPlaying && (forceStopped || !holding) && !NoStop && !_pupPlayer.IsLooping)
			{
				_pupPlayer.Stop();
			}
		}

		public void start()
		{
			_pupPlayer = self.GetComponent<PuppetPlayer>();
			_pupPlayer.Play(animation, from, to);

			_pupPlayer.ElementEntered += OnElementEnter;
			_pupPlayer.Ending         += OnPlayerOnEnding;
		}

		public void stop()
		{
			if (_pupPlayer != null)
				_pupPlayer.Stop();
		}

		private void OnPlayerOnEnding()
		{
			if (!holding)
				_pupPlayer.FreeExtensions();

			_pupPlayer.Ending -= OnPlayerOnEnding;
			_ended            =  true;
		}

		private void OnElementEnter(IElement elem)
		{
			if (elem is ProcElement procElem)
			{
				if (!procing)
				{
					Debug.Log("[TRACE] skipping proc firing in anim because procing is disabled.");
					return;
				}

				BattleRunner runner = costate.battle;
				ProcTable    procs  = costate.procs;
				if (runner != null && procs != null)
				{
					if (procs.Remaining == 0)
					{
						Debug.LogError("No procs remaining in the proc table to fire proc element from puppet animation.");
						return;
					}

					if (procs.PopNext(out Proc proc))
					{
						runner.battle.Proc(proc);
					}
				}
			}
		}

		public struct ProcElemLink : IPuppetLinked
		{
			public Battle    battle;
			public ProcTable table;

			public void OnPuppetAnimLinked(PuppetAnimation propertySheet, int elementID) { }

			public void OnPuppetAnimControled(ISceneSpace sceneSpace)
			{

			}

			public void OnPuppetAnimExit(ISceneSpace sceneSpace) { }

			public void OnPuppetAnimFrame(ISceneSpace sceneSpace, int idxFrame) { }

			public void OnPuppetAnimProperties(ISceneSpace sceneSpace, PropertyStore properties) { }

			public void OnPuppetAnimFreed() { }
		}

		public override void OnCoplayerUpdate(float dt)
		{
			_pupPlayer.speed = costate.timescale.current;
		}
	}
}