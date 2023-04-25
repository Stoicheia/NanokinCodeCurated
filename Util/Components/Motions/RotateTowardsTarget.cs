using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Utils;
using UnityEngine;


namespace Anjin.Utils
{
	[RequireComponent(typeof(MotionBehaviour))]
	public class RotateTowardsTarget : MonoBehaviour
	{
		private MotionBehaviour _motion;
		private Transform _transform;
		private void Awake()
		{
			_motion = GetComponent<MotionBehaviour>();
			_transform = GetComponent<Transform>();
		}

		private void LateUpdate()
		{
			_transform.rotation = Quaternion.LookRotation(_motion.TargetPos - _transform.position, Vector3.forward);
		}
	}
}