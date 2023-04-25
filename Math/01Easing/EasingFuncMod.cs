using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Scripting;
using DG.Tweening;
using UnityEngine;

namespace Util
{
	[LuaUserdata]
	public class EasingFuncMod : IEvaluable
	{
		private Func<float, float> _transformation;
		private Func<float, float> _inputmod;
		private IEvaluable _easing;

		public EasingFuncMod(IEvaluable func, Func<float, float> tr)
		{
			_transformation = tr;
			_inputmod = f => f;
			_easing = func;
		}

		public EasingFuncMod(IEvaluable func, Func<float, float> ytr, Func<float, float> xtr)
		{
			_transformation = ytr;
			_inputmod = xtr;
			_easing = func;
		}

		public float Evaluate(float t)
		{
			return _transformation(_easing.Evaluate(_inputmod(t)));
		}

		public EaseFunction GetFunction()
		{
			return (float t, float d, float a, float ___) => Evaluate(t / d);
		}

		public EasingFuncMod scale(int y)
		{
			return new EasingFuncMod(this, f => f*y);
		}

		public EasingFuncMod scale(int x, int y)
		{
			return new EasingFuncMod(this, f => f * y, f => f * x);
		}

		public EasingFuncMod translate(float x)
		{
			return new EasingFuncMod(this, f => f + x);
		}

		public EasingFuncMod pow_ease(float y, float cap = 1)
		{
			return new EasingFuncMod(this, f => y * Mathf.Pow(f/cap, y));
		}

		public EasingFuncMod flip(float axis = 1)
		{
			return new EasingFuncMod(this, f => axis - f);
		}

		public EasingFuncMod delay(float t)
		{
			return new EasingFuncMod(this, f => f > t ? 1 / (1 - t) * (f - t) : Evaluate(0));
		}

		public EasingFuncMod piecewise(IEvaluable other, float midpoint = 0.5f)
		{
			return new EasingFuncMod(this, f => f > midpoint ? other.Evaluate(f) : Evaluate(f));
		}

		public EasingFuncMod compose(IEvaluable other, float midpoint = 0.5f)
		{
			Debug.Log(other.Evaluate(0.5f) + "DDDDDD");
			return new EasingFuncMod(this, f => f > midpoint ? other.Evaluate(1/(1-midpoint)*(f-midpoint)) : Evaluate(1/midpoint*f));
		}

		public EasingFuncMod compose_cont(IEvaluable other, float midpoint = 0.5f)
		{
			return new EasingFuncMod(this, f => f > midpoint ? other.Evaluate(1/(1-midpoint)*(f-midpoint)) + Evaluate(midpoint) : Evaluate(1/midpoint*f));
		}
	}
}
