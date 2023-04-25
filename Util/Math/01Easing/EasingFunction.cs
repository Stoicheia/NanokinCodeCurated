using System.Collections;
using System.Collections.Generic;
using Anjin.Scripting;
using DG.Tweening;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using Anjin.Scripting;
using Anjin.Util;
using Anjin.Util.Asset_Reference_Storage;
using API.PropertySheet.PipelineCache;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Util
{
	[LuaUserdata]
	//[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public abstract class EasingFunction : SerializedScriptableObject, IEvaluable
	{
		public abstract float Evaluate(float t);
		public float DummyEvaluate(float t, float d, float a, float ___) => Evaluate(t/d);
		protected float _customParameter = 1.0f;

		public EaseFunction GetFunction()
		{
			return DummyEvaluate;
		}

		public static EaseFunction ConstantZero => (time, duration, amplitude, period) => 0;

		public static EaseFunction GetFunctionPolarToAxis(IEvaluable rFunc, IEvaluable thetaFunc, PolarToAxis.EvalAxis axis, float rOff, float tOff)
		{
			return PolarToAxis.DummyEvaluate(rFunc, thetaFunc, axis, rOff, tOff);
		}

		public EasingFuncMod mod()
		{
			return new EasingFuncMod(this, f => f);
		}

		public EasingFuncMod mod(float cus)
		{
			_customParameter = cus;
			return new EasingFuncMod(this, f => f);
		}
	}
}
