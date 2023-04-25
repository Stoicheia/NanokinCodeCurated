using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util
{
	/// <summary>
	/// Copies another camera.
	/// </summary>
	[AddComponentMenu("Anjin: Utility/Camera Copy")]
	public class CameraCopy : SerializedMonoBehaviour
	{
		[Optional] public Camera CopiedCamera;
		private           Camera _thisCamera;

		/// <summary>
		/// Unity's Awake event.
		/// </summary>
		private void Awake()
		{
			_thisCamera = GetComponent<Camera>();
		}

		/// <summary>
		/// Unity's LateUpdate event.
		/// Copies the other camera at the end of every update.
		/// </summary>
		private void LateUpdate()
		{
			Copy(CopiedCamera);
		}

		/// <summary>
		/// Copies the transform and lens property of another unity camera.
		/// </summary>
		/// <param name="other">The other camera to copy.</param>
		public void Copy(Camera other)
		{
			if (other == null)
				return;

			_thisCamera.fieldOfView   = other.fieldOfView;
			_thisCamera.nearClipPlane = other.nearClipPlane;
			_thisCamera.farClipPlane  = other.farClipPlane;
		}
	}
}