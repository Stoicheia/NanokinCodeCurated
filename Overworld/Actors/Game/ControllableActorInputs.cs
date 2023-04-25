using UnityEngine;

namespace Anjin.Actors
{
	#region Debug

	public struct FirstPersonFlightInputs
	{
		public Vector2 RotationDelta;
		public Vector3 MoveDirection;
		public float   ZoomDelta;
		public float   SpeedDelta;
		public bool    FastMode;

		/// <summary>
		/// If this is set, the camera should lock facing direction to this object.
		/// </summary>
		public Transform Target;

		public static FirstPersonFlightInputs DefaultInputs = new FirstPersonFlightInputs
		{
			RotationDelta = Vector2.zero,
			MoveDirection = Vector3.zero,
			ZoomDelta     = 0,
			FastMode      = false,
			Target        = null,
		};
	}

	#endregion

	#region Characters

	/// <summary>
	/// A way for any brain to send inputs to the main actor
	/// </summary>
	public struct CharacterInputs
	{
		public bool processed;

		public Vector3 move;
		public float   moveMagnitude;
		public bool    hasMove;
		public float?  moveSpeed;

		public Vector3? look;		 // When unset, up to the character to decide what's best according to its state.
		public bool     instantLook; // Any state should immediately update the facing direction of the actor if this is set.

		public bool jumpPressed;
		public bool jumpHeld;
		public bool runHeld;

		public bool diveHeld;
		public bool divePressed;

		public bool doubleJumpPressed;
		public bool doubleJumpHeld;

		public bool glidePressed;
		public bool glideHeld;

		public bool pogoPressed;
		public bool pogoHeld;

		public bool swordPressed;
		public bool swordHeld;


		public float LookDirLerp;

		public static readonly CharacterInputs DefaultInputs = new CharacterInputs
		{
			move        = Vector2.zero,
			look        = null,
			instantLook = false,
			LookDirLerp = 1,

			runHeld = false,

			jumpPressed = false,
			jumpHeld    = false,

			doubleJumpPressed = false,
			doubleJumpHeld = false,

			glidePressed = false,
			glideHeld    = false,

			divePressed = false,
			diveHeld    = false,

			swordPressed = false,
			swordHeld =  false,

			moveSpeed = null
		};

		public void NoMovement()
		{
			move          = Vector3.zero;
			moveMagnitude = 0;
			moveSpeed     = null;
			hasMove       = false;
		}

		public void NoLook()
		{
			look = Vector3.zero;
		}

		public void Look2D(Vector2 v)
		{
			look = new Vector3(v.x, 0, v.y).normalized;
		}

		public void LookStripY(Vector3 v)
		{
			look = new Vector3(v.x, 0, v.z).normalized;
		}

		public void OnProcessed()
		{
			processed    = true;
			divePressed  = false;
			glidePressed = false;
			jumpPressed  = false;
			doubleJumpPressed = false;
			pogoPressed  = false;
			swordPressed = false;
		}
	}

	#endregion


	#region Vehicles

	public struct MinecartInputs
	{
		public bool leanLeft;
		public bool leanRight;

		public bool swordPressed;
		public bool jumpPressed;

		public static MinecartInputs Default = new MinecartInputs()
		{

		};
	}
	#endregion
}