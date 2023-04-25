using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using UnityEngine;

namespace Anjin.Actors.States
{
	public class SitState : StateKCC
	{
		public  Transform SitRoot;
		private SitPoint  _sitPoint;

		public Vector3		EnteringPosition;
		public Quaternion	EnteringOrientation;

		public override void OnActivate()
		{
			actor.SetPhysicsEnabled(false);

			EnteringPosition    = actor.transform.position;
			EnteringOrientation = actor.transform.rotation;

			actor.transform.position = SitRoot.transform.position;
			actor.transform.rotation = SitRoot.transform.rotation;
			actor.facing             = SitRoot.transform.forward;
		}

		public override void OnDeactivate()
		{
			SitRoot   = null;
			_sitPoint = null;
			actor.SetPhysicsEnabled(true);
		}

		public override void OnUpdate(float dt)
		{
			if (!active) return;

			actor.transform.position = SitRoot.transform.position;
			actor.transform.rotation = SitRoot.transform.rotation;

			if(GameInputs.jump.AbsorbPress(0.05f)) {
				Eject();
			}
		}

		public void Eject(Transform ejectPoint = null)
		{
			actor.ChangeState(actor.GetDefaultState());

			if(ejectPoint) {
				actor.Teleport(ejectPoint.position);
				actor.Reorient(ejectPoint.rotation);
			}

			actor.inputs = CharacterInputs.DefaultInputs;
		}

		public SitState WithSitRoot(Transform point)
		{
			SitRoot   = point.transform;
			_sitPoint = SitRoot.GetComponent<SitPoint>();
			return this;
		}

		public SitState WithSitPoint(SitPoint point)
		{
			SitRoot   = point.transform;
			_sitPoint = point;
			return this;
		}
	}
}