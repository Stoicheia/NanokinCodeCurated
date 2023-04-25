using Anjin.Actors;
using Combat.Data.VFXs;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Combat.Toolkit
{
	public class FXVFX : VFX
	{
		private FX _fx;

		public FXVFX()
		{
			_fx = new FX();
		}

		public FXVFX(FX fx)
		{
			_fx = fx;
		}

		public FXVFX(GameObject prefab) : this()
		{
			_fx.prefab = prefab;
		}

		public FXVFX(string address) : this()
		{
			_fx.address = address;
		}

		public FXVFX(string address, Vector3 pos) : this()
		{
			_fx.address = address;
			_fx.onto    = pos;
		}


		internal override void Enter()
		{
			_fx.Start().Forget();
		}

		internal override void Leave()
		{
			base.Leave();
			_fx.Stop();
		}

		public override void Cleanup()
		{
			_fx.Stop();
			_fx.Cleanup();
		}

		public override void OnTimeScaleChanged(float scale)
		{
			base.OnTimeScaleChanged(scale);
			_fx.UpdateTimescale(scale);
		}
	}
}