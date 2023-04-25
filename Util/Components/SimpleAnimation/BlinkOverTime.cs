using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;

namespace Anjin.Utils
{
	public class BlinkOverTime : MonoBehaviour
	{
		private readonly bool[] TRUE_FALSE = {true, false};

		[SerializeField] public List<Renderer> _toBlink;
		[SerializeField] public float _blinkPeriodSeconds;

		private int _blinkCount;
		private float _nextBlinkTime;

		private void OnEnable()
		{
			EnableAll();
		}

		private void OnDisable()
		{
			EnableAll();
		}

		private void Update()
		{
			if (Time.time >= _nextBlinkTime)
			{
				_toBlink.ForEach(x => x.enabled = TRUE_FALSE[_blinkCount % TRUE_FALSE.Length]);
				while (_nextBlinkTime <= Time.time + Time.deltaTime)
					_nextBlinkTime += _blinkPeriodSeconds;
				_blinkCount++;
			}
		}

		private void EnableAll()
		{
			_toBlink.ForEach(x => x.enabled = true);
		}
	}
}
