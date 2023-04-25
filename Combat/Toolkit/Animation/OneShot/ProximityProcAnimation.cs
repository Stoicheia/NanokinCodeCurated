using Anjin.Actors;
using Combat.Data;
using Overworld.Cutscenes;
using UnityEngine;

namespace Combat.Toolkit
{
	// a dark side of me wants to call this ProcximityAnimation
	public class ProximityProcAnimation : CoroutineManaged
	{
		private readonly GameObject _self;
		private readonly int        _procIndex;
		private readonly Transform  _proximity;
		private readonly float      _distance;

		private ActorBase _actor;
		private bool      _awaitingFire;

		public ProximityProcAnimation(GameObject self, int procIndex, Transform proximity, float distance = 2f)
		{
			_self      = self;
			_procIndex = procIndex;
			_proximity = proximity;
			_distance  = distance;
		}

		public override float ReportedDuration => 0;

		public override float ReportedProgress => 1;
		public override bool  Active           => _awaitingFire;

		public override void OnStart()
		{
			base.OnStart();

			_actor        = _self.GetComponent<ActorBase>();
			_awaitingFire = true;
		}

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			return true;
		}

		public override void OnCoplayerUpdate(float dt)
		{
			base.OnCoplayerUpdate(dt);

			if (!_awaitingFire) return;

			if (_proximity == null)
			{
				_awaitingFire = false;
				return;
			}

			if (Vector3.Distance(_actor.transform.position, _proximity.position) - _actor.radius < _distance)
			{
				_awaitingFire = false;

				ProcTable procs = costate.procs;
				if (procs == null) return;

				BattleRunner runner = costate.battle;
				if (runner == null) return;

				if (procs.PopNext(out Proc proc))
				{
					runner.battle.Proc(proc);
				}
			}
		}
	}
}