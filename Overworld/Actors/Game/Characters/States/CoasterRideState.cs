using Anjin.Minigames;
using Anjin.Nanokin;
using UnityEngine;

namespace Anjin.Actors.States
{
	public class CoasterRideState : StateKCC
	{
		public CoasterCarActor Car;

		public Vector3    EnteringPosition;
		public Quaternion EnteringOrientation;

		public override void OnActivate()
		{
			actor.SetPhysicsEnabled(false);

			EnteringPosition    = Car.RiderTiltRoot.position;
			EnteringOrientation = Car.RiderTiltRoot.rotation;

			actor.transform.position = Car.RiderTiltRoot.position;
			actor.transform.rotation = Car.RiderTiltRoot.rotation;
			actor.facing             = Car.RiderTiltRoot.forward;
		}

		public override void OnDeactivate()
		{
			Car       = null;
			actor.SetPhysicsEnabled(true);

			if(actor.PivotTiltRoot)
				actor.PivotTiltRoot.localEulerAngles = Vector3.zero;
		}

		public override void OnUpdate(float dt)
		{
			if (!active) return;

			actor.transform.position = Car.RiderTiltRoot.position;
			actor.transform.rotation = Car.RiderTiltRoot.rotation;
			actor.facing             = Car.RiderTiltRoot.forward;

			if(actor.PivotTiltRoot)
				actor.PivotTiltRoot.localEulerAngles = new Vector3(0, 0, Car.transform.rotation.eulerAngles.z);

			if(GameInputs.jump.AbsorbPress(0.05f))
			{
				CoasterCarActor car = Car;
				Eject();
				car.Controller.OnActorExit(actor);
			}
		}

		public void Eject(Transform ejectPoint = null)
		{

			if(ejectPoint)
			{
				actor.transform.position += Vector3.up * 0.3f;
				actor.Reorient(ejectPoint.rotation);
				actor.ChangeState(player._eject.ToPosition(ejectPoint.transform.position));

				/*actor.Teleport(ejectPoint.position);
				*/
				//actor.Jump();
			} else {
				actor.Teleport(actor.transform.position + Vector3.up * 0.3f);
				actor.ChangeState(actor.GetDefaultState());
				actor.Jump(3);
			}

			actor.inputs = CharacterInputs.DefaultInputs;
		}

		public CoasterRideState EnterCar(CoasterCarActor car)
		{
			Car = car;
			return this;
		}
	}
}