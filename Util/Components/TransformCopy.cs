using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util
{
	/// <summary>
	/// Copies another transform.
	/// </summary>

	[AddComponentMenu("Anjin: Utility/Copy Transform")]
	public class TransformCopy : SerializedMonoBehaviour
	{
		[Optional] public Transform CopiedTransform;
		private           Transform thisTransform;

		/// <summary>
		/// Unity's Awake event.
		/// </summary>
		private void Awake()
		{
			thisTransform = GetComponent<Transform>();
		}

		/// <summary>
		/// Unity's LateUpdate event.
		/// Copies the other camera at the end of every update.
		/// </summary>
		private void LateUpdate()
		{
			Copy(CopiedTransform);
		}

		/// <summary>
		/// Copies the transform and lens property of another unity camera.
		/// </summary>
		/// <param name="other">The other camera to copy.</param>
		public void Copy(Transform other)
		{
			if (other == null)
				return;

			thisTransform.position   = other.position;
			thisTransform.rotation   = other.rotation;
			thisTransform.localScale = other.localScale;
		}
	}
}