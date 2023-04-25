using UnityEngine;

namespace Anjin.MP
{
	public interface IPathFollower
	{
		void MP_SetDirection(Vector3 dir);
		void MP_SetSpeed(float speed);
		void MP_SetPosition(Vector3 pos);

		bool AbleToPath();

		void MP_DoMovement();

		void MP_OnPathDone();
		void MP_OnPathStart();

		//void MP_DoJumpUpwards(...)
		//void MP_DoJumpForwards(...)
	}
}