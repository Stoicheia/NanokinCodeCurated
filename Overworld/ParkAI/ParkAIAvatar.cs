using System;
using Overworld.Rendering;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.ParkAI {

	public class ParkAIAvatar : MonoBehaviour {

		public SpriteRenderer HeadRenderer;
		public SpriteRenderer BodyRenderer;

		public ColorReplacementSetter HeadSetter;
		public ColorReplacementSetter BodySetter;

		public Transform GroundPivotTransform;
		public Transform BillboardTransform;

		public float starting_y_offset;
		public float sitting_y_offset;

		[NonSerialized, ShowInInspector]
		public Vector3 originalHeadLocalPos;

		private void Awake()
		{
			originalHeadLocalPos = HeadRenderer.transform.localPosition;
		}

		public void Hide()
		{
			HeadRenderer.enabled = false;
			BodyRenderer.enabled = false;
		}

		public void Show()
		{
			HeadRenderer.enabled = true;
			BodyRenderer.enabled = true;
		}
	}
}