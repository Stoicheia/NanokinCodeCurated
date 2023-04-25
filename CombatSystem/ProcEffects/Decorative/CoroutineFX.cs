using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine;

namespace Combat.Data.Decorative
{
	/// <summary>
	/// An animation that spawns an effect and feeds some info into it.
	/// The VFX can originate [from] an object and affect [onto] another object.
	/// It can spawn [onto] the other object, or move towards it with a MotionBehaviour.
	/// </summary>
	[LuaUserdata]
	public class CoroutineFX : CoroutineManaged, IWorldPoint
	{
		private FX _fx;

		public CoroutineFX()
		{
			_fx = new FX();
		}

		public CoroutineFX(FX fx)
		{
			_fx = fx;
		}

		public CoroutineFX(GameObject prefab) : this()
		{
			_fx.prefab = prefab;
		}

		public CoroutineFX(string address) : this()
		{
			_fx.address = address;
		}

		public CoroutineFX(string address, Vector3 pos) : this()
		{
			_fx.address = address;
			_fx.onto    = pos;
		}

		public override float ReportedDuration => 0;
		public override float ReportedProgress => 0;
		public override bool  Active           => _fx.CheckDeath();

		public Vector3 position => _fx.gameObject.transform.position;

		public override bool CanContinue(bool justYielded, bool isCatchup = false) => !Active || _fx.IsExiting;

		public Vector3 GetPosition() => _fx.GetPosition();

		public GameObject ToGameObject() => _fx.gameObject;
		public ActorBase  ToActor()      => _fx.gameObject.GetComponent<ActorBase>();

		public override void OnStart()
		{
			base.OnStart();

			_fx.battle = costate.battle?.battle;
			_fx.procs  = costate.procs;
			_fx.Start().Forget();
		}

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			_fx.Cleanup();
		}

		public void Despawn(bool immediate = false)
		{
			_fx.Stop(immediate);
			// ParticleSystem[] particles = _fx.instance.GetComponentsInChildren<ParticleSystem>();
			// if (particles.Length > 0)
			// {
			// 	foreach(ParticleSystem particle in particles)
			// 		particle.Stop();
			// }
			//
			// _fx.instance.gameObject.Destroy();
		}

		public void Retract()
		{
			_fx.Retract();
		}

		public void UpdateConfig(Table conf)
		{
			_fx.UpdateConfig(conf);
		}
	}
}