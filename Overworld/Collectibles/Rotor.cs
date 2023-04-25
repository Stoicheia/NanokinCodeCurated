using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Utils;
using Assets.Scripts.Utils;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;
using Util;

namespace Anjin.Nanokin.Map
{
	public class Rotor : MonoBehaviour
	{

		[SerializeField] private float _intensity;
		[SerializeField] [Range(0,1)] private float _dampening;
		[SerializeField] private Vector3 _rotationAxis;

		private Vector3 _targetRotation;

		public void Spin()
		{
			_targetRotation += _rotationAxis * _intensity;
		}

		public void Update()
		{
			transform.Rotate(_targetRotation * Time.deltaTime, Space.World);
			_targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, (1-_dampening) * Time.deltaTime);
		}
	}
}
