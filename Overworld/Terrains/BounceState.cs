using Anjin.Actors;
using UnityEngine;

namespace Overworld.Terrains
{
	public class BounceState : StateKCC
	{
		private BounceInfo   _currentInfo;
		private InertiaForce _inertia;
		private bool         _hasStarted;

		protected override float   TurnSpeed   => 5;
		protected override Vector3 TurnDirection => _currentInfo.direction;
		public override    bool    IsAir         => true;

		public override void OnActivate()
		{
			base.OnActivate();
			_hasStarted = false;
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			base.UpdateVelocity(ref currentVelocity, deltaTime);

			if (_hasStarted)
			{
				_hasStarted     = true;
				currentVelocity = _currentInfo.AffectVelocity(currentVelocity, actor);
			}
		}

		public BounceState BeginBounce(BounceInfo info)
		{
			_currentInfo = info;
			return this;
		}
	}
}