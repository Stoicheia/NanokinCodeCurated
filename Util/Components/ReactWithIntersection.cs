using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityUtilities;
using Util;

namespace Overworld.Rendering
{
	[AddComponentMenu("Anjin: Transform/React With Intersection")]
	public class ReactWithIntersection : SerializedMonoBehaviour
	{
		[Title("Logic")]
		public LayerMask ActivationMask;
		[MinValue(0)] public float   Length;
		public               Vector3 BaseOffset;

		[Title("Visual")]
		[Required] public Transform VisualTransform;
		public               float IntersectionOffset = 1;
		[MinValue(1)] public float ChangeSpeed        = 0.25f;
		public               bool  SnapDown;
		public               bool  SnapUp;

		[Title("Audio")]
		public AudioSource AudioSource;
		public float BasePitch = 1;
		public float EndPitch  = 1;

		public  bool     _hasAudio;
		private Vector3  _basePosition;
		private Collider _currentCollider;
		private float    _currentIntersection; // intersection from the end point
		private bool     _active;

		private void Start()
		{
			_basePosition = VisualTransform.position;
			_hasAudio     = AudioSource;
		}

		private void OnTriggerEnter(Collider collider)
		{
			if (!ActivationMask.ContainsLayer(collider.gameObject.layer))
				return;

			_currentCollider = collider;
			_active          = true;
		}

		// private void OnTriggerStay(Collider collider)
		// {
		// 	if (!ActivationMask.ContainsLayer(collider.gameObject.layer))
		// 		return;
		//
		// 	_currentCollider = collider;
		// 	_active = true;
		// }

		private void OnTriggerExit(Collider collider)
		{
			if (_currentCollider == collider)
			{
				// this is gonna be wack if we want multiple objects to be able to interact with this component at once
				// one thing at a time tho, this may be fine for our purposes
				_currentCollider = null;
				_active          = false;
			}
		}

		// private void OnTriggerStay(Collider collider)
		// {
		// 	if (!ActivationMask.ContainsLayer(collider.gameObject.layer))
		// 		return;
		//
		// 	_currentCollider = collider;
		// }

		private void LateUpdate()
		{
			// NOTE: The intersection is calculated from the END point, not the base.
			//       This means that the intersection float will be 0 when the collider
			//       is at the very tip/tail end of the column.


			// Calculate the intersection
			// ----------------------------------------

			float targetIntersection = 0;

			if (_active)
			{
				Vector3 p1 = _basePosition + BaseOffset;
				Vector3 p2 = _basePosition + transform.up * Length;

				Vector3 intersection = Math3d.ProjectPointOnLineSegment(p1, p2, _currentCollider.transform.position);

				targetIntersection = Vector3.Distance(intersection, p2) + IntersectionOffset;
				targetIntersection = Mathf.Clamp(targetIntersection, 0, Length);
			}


			// Move towards the target
			// ----------------------------------------

			if (_currentIntersection < targetIntersection)
			{
				if (SnapDown) _currentIntersection = targetIntersection;
				_currentIntersection = (_currentIntersection + ChangeSpeed).Maximum(targetIntersection);
			}

			if (_currentIntersection > targetIntersection)
			{
				if (SnapUp) _currentIntersection = targetIntersection;
				_currentIntersection = (_currentIntersection - ChangeSpeed).Minimum(targetIntersection);
			}


			// Update the visual
			// ----------------------------------------

			VisualTransform.transform.position = _basePosition - transform.up * _currentIntersection;


			// Update the audio
			// ----------------------------------------

			float intersectionPercent = _currentIntersection / Length;

			if (AudioSource)
				AudioSource.pitch = Mathf.Lerp(BasePitch, EndPitch, intersectionPercent);

			//
			// // Reset the current collider to use
			// // ----------------------------------------
			// _currentCollider = null;
		}

		private void OnDrawGizmosSelected()
		{
			Vector3 pos = Vector3.zero;

			if (Application.isPlaying) pos        = _basePosition;
			else if (VisualTransform != null) pos = VisualTransform.position;

			if (VisualTransform != null)
			{
				Draw2.DrawTwoToneSphere(pos, 0.15f, Color.cyan);
				Draw2.DrawLineTowards(pos, transform.up * Length, Color.cyan);
			}
		}
	}
}