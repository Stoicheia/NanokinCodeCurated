using UnityEngine;

namespace Anjin.Actors
{
	/// <summary>
	/// Container for information that is general to all air states. (not on ground)
	/// </summary>
	public class AirMetrics
	{
		/// <summary>
		/// Whether we have just became airborn after being on the ground.
		/// </summary>
		public bool justLifted;

		/// <summary>
		/// Elapsed time spent in the air. (in seconds)
		/// </summary>
		public float airTime;

		/// <summary>
		/// The traveled Y distance during the last frame only.
		/// </summary>
		public float yDelta;

		/// <summary>
		/// The total height traveled while falling.
		/// </summary>
		public float traveledHeightFalling;

		/// <summary>
		/// The total height traveled while rising.
		/// </summary>
		public float traveledHeightRising;

		/// <summary>
		/// The total height traveled in the air. (any direction)
		/// </summary>
		public float traveledHeight;

		/// <summary>
		/// The relative difference in height between the start of the air state and the current position.
		/// </summary>
		public float heightDelta;

		/// <summary>
		/// Whether we have just connected with the ground after being in the air, only for one frame.
		/// </summary>
		public bool justLanded;

		/// <summary>
		/// Elapsed time spent on the ground. (in seconds)
		/// </summary>
		public float groundTime;

		/// <summary>
		/// Elapsed time since we've connected with the ground after being in the air.
		/// </summary>
		public float elapsedSinceLanding;

		private Vector3 _prevPosition;
		private bool    _hasLanded; // Whether we have entered grounding from being previously airborn.

		/// <summary>
		/// Whether or not we are currently airborn.
		/// </summary>
		public bool airborn { get; private set; }

		/// <summary>
		/// Whether or not we are currently grounded.
		/// </summary>
		public bool grounded { get; private set; }

		public bool hasJumpedAgain { get; private set; }

		public void BeforeUpdate()
		{
			justLifted = false;
			justLanded = false;
		}

		public void UpdateAir(Transform transform, float deltaTime)
		{
			BeforeUpdate();

			yDelta                =  (transform.position - _prevPosition).y;
			traveledHeightFalling += Mathf.Abs(Mathf.Min(0, yDelta));
			traveledHeightRising  += Mathf.Max(0, yDelta);
			traveledHeight        += Mathf.Abs(yDelta);
			heightDelta           += yDelta;
			airTime               += deltaTime;

			if (!airborn)
			{
				// Has lifted off the ground. (became airborn)
				airborn = true;

				airTime               = 0;
				yDelta                = 0;
				traveledHeightFalling = 0;
				traveledHeightRising  = 0;
				traveledHeight        = 0;
				heightDelta           = 0;

				if (grounded)
				{
					// We've lifted off the ground since we were previously grounded. (as opposed to spawning mid-air)
					grounded = false;

					justLifted = true;
				}
			}

			_prevPosition = transform.position;
		}

		public void UpdateGround(Transform transform, float deltaTime)
		{
			BeforeUpdate();

			if (_hasLanded) elapsedSinceLanding += deltaTime;
			groundTime += deltaTime;

			if (!grounded)
			{
				// Has landed on the ground. (became grounded)
				grounded = true;

				groundTime          = 0;
				elapsedSinceLanding = 0;

				if (airborn)
				{
					// We've landed on the ground since we were previously airborn. (as opposed to spawning already grounded)
					airborn = false;

					_hasLanded = true;
					justLanded = true;
					hasJumpedAgain = false;
				}
			}
		}

		public void RefreshSecondJump()
		{
			hasJumpedAgain = false;
		}

		public void BeforeSecondJump()
		{
			hasJumpedAgain = true;
		}

		public void OnAfterVelocityUpdate(Transform transform, float deltaTime)
		{
			_prevPosition = transform.position;
		}
	}
}