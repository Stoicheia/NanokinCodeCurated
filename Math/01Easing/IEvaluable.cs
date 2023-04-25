using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Util
{
	public interface IEvaluable
	{
		float Evaluate(float t);
		EaseFunction GetFunction();
	}
}
