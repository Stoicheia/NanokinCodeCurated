using Combat.Data.VFXs;
using UnityEngine;

namespace Overworld.Cutscenes
{
	public class ManagedVFX : CoroutineManaged
	{
		private readonly GameObject _object;
		private readonly VFX        _vfx;

		public ManagedVFX(GameObject @object, VFX vfx)
		{
			_object = @object;
			_vfx    = vfx;
		}

		public override bool Active => _vfx.IsActive;

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			return !_vfx.IsActive;
		}

		public override void OnStart()
		{
			VFXManager vfxman = _object.GetComponentInChildren<VFXManager>();
			if (vfxman == null)
			{
				Debug.Log($"Adding VFX manager to {_object}.", _object);
				vfxman = _object.AddComponent<VFXManager>();
			}

			vfxman.Add(_vfx);
		}

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			if(_object != null) {
				VFXManager vfxman = _object.GetComponentInChildren<VFXManager>();
				vfxman.Remove(_vfx);
			}
		}

		public override string ToString()
		{
			return $"ManagedVFX({_vfx})";
		}
	}
}