using Anjin.Nanokin;
using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Anjin.Minigames
{
	[LuaUserdata]
	public class FirstPersonRotator : SerializedMonoBehaviour
	{
		public bool Active;

		public float HSensitivity_Mouse = 0.2f;
		public float VSensitivity_Mouse = 0.2f;

		public float HSensitivity_Pad = 1;
		public float VSensitivity_Pad = 1;

		public float HAngle = 0;
		public float VAngle = 0;

		public (float min, float max)? HorLimits = null;
		public (float min, float max)? VerLimits = null;

		public Vector3 StartingRot;

		void Start()
		{
			StartingRot = transform.rotation.eulerAngles;
		}

		void Update()
		{
			if (Active)
			{
				var xdelta = 0f;
				var ydelta = 0f;

				if (GameInputs.ActiveDevice != InputDevices.KeyboardAndMouse ||
				    (GameController.DebugMode && !Mouse.current.rightButton.isPressed)) {
					HAngle += GameInputs.look.Horizontal * HSensitivity_Pad * ( GameOptions.current.InvertFPCamXAxis_Pad ? -1 : 1 );
					VAngle += GameInputs.look.Vertical 	* VSensitivity_Pad * ( GameOptions.current.InvertFPCamYAxis_Pad ? -1 : 1 );
				} else {
					HAngle += (GameInputs.GetCapturedMouseDelta().x) * HSensitivity_Mouse * ( GameOptions.current.InvertFPCamXAxis_Mouse ? -1 : 1 );
					VAngle += (GameInputs.GetCapturedMouseDelta().y) * VSensitivity_Mouse * ( GameOptions.current.InvertFPCamYAxis_Mouse ? -1 : 1 );
				}
			}

			if (HorLimits.HasValue) {
				var v = HorLimits.Value;
				HAngle = Mathf.Clamp(HAngle, v.min, v.max);
			}

			if (VerLimits.HasValue) {
				var v = VerLimits.Value;
				VAngle = Mathf.Clamp(VAngle, v.min, v.max);
			}

			transform.rotation = Quaternion.Euler(StartingRot.x + VAngle, StartingRot.y + HAngle, StartingRot.z);
		}
	}
}