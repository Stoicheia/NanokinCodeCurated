using System.Collections.Generic;
using Anjin.Nanokin.Core;
using Anjin.Util;
using Anjin.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace API.Spritesheet.Indexing.Runtime
{
	public class SimpleAnimationsSystem
	{
		public static List<RotateOverTime>    rotateOverTime;
		public static List<MoveOverTime>      moveOverTime;
		public static List<OscillateOverTime> oscillateOverTime;

		public static List<MoveAlongCurve>   moveAlongCurve;
		public static List<RotateAlongCurve> rotateAlongCurve;
		public static List<ScaleAlongCurve>  scaleAlongCurves;

		public static bool                        dirty;
		private static TransformAccessArray        _transforms;
		private static NativeArray<AnimatedObject> _objects;

		struct AnimatedObject {

			public bool       RotatesOverTime;
			public Vector3    DeltaPerSecond;
			public Quaternion baseRotation;

			public bool    MoveOverTime;
			public Vector3 UnitPerSecond;

			public bool    OscillateOverTime;
			public Vector3 Amplitude;
			public float   Duration;
			public Vector3 basePosition;
			public float   t;

		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			PlayerLoopInjector.Inject<SimpleAnimationsSystem>(PlayerLoopTiming.PreLateUpdate, Update);

			rotateOverTime    = new List<RotateOverTime>();
			moveOverTime      = new List<MoveOverTime>();
			oscillateOverTime = new List<OscillateOverTime>();

			moveAlongCurve   = new List<MoveAlongCurve>();
			rotateAlongCurve = new List<RotateAlongCurve>();
			scaleAlongCurves = new List<ScaleAlongCurve>();

			dirty = true;
		}

		private static void Rebuild()
		{
			if(_transforms.isCreated) 	_transforms.Dispose();
			if(_objects.IsCreated) 		_objects.Dispose();

			var totalSize = rotateOverTime.Count + moveOverTime.Count + oscillateOverTime.Count;

			_transforms = new TransformAccessArray(totalSize, 64);
			_objects    = new NativeArray<AnimatedObject>(totalSize, Allocator.Persistent);

			int j = 0;
			for (int i = 0; i < rotateOverTime.Count; i++) {
				var comp = rotateOverTime[i];

				_transforms.Add(comp.transform);
				_objects[j++] = new AnimatedObject {
					RotatesOverTime = true,
					DeltaPerSecond = comp.DeltaPerSecond,
					baseRotation   = comp.baseRotation,
				};
			}

			for (int i = 0; i < moveOverTime.Count; i++) {
				var comp = moveOverTime[i];

				_transforms.Add(comp.transform);
				_objects[j++] = new AnimatedObject {
					MoveOverTime 	= true,
					UnitPerSecond  	= comp.UnitPerSecond,
				};
			}

			for (int i = 0; i < oscillateOverTime.Count; i++) {
				var comp = oscillateOverTime[i];

				_transforms.Add(comp.transform);
				_objects[j++] = new AnimatedObject {
					OscillateOverTime 	= true,
					Amplitude 		= comp.Amplitude,
					Duration  		= comp.Duration,
					basePosition 	= comp.basePosition,
					t 				= comp.t,
				};
			}
		}

		private static void Update()
		{
			if (dirty) {
				Rebuild();
				dirty = false;
			}

			/*foreach (MoveOverTime mov in moveOverTime)
			{
				mov.transform.Translate(mov.UnitPerSecond * Time.deltaTime, Space.Self);
			}

			foreach (RotateOverTime rot in rotateOverTime)
			{
				rot.transform.Rotate(Time.deltaTime * rot.DeltaPerSecond, Space.Self);
			}

			foreach (OscillateOverTime osc in oscillateOverTime)
			{
				float cos = Mathf.Cos(osc.t * osc.Duration);
				float sin = Mathf.Sin(osc.t * osc.Duration);

				osc.transform.localPosition =  osc.basePosition + Vector3.Scale(new Vector3(cos, sin, cos), osc.Amplitude);
				osc.t                       += Time.deltaTime;
			}*/

			var job = new UpdateJob {
				dt 		= Time.deltaTime,
				Objects = _objects,
			};

			job.Schedule(_transforms).Complete();

			// TODO: This may be harder to jobify?
			foreach (MoveAlongCurve mov in moveAlongCurve)
			{
				var offset = Vector3.Scale(
					new Vector3(
						mov.X.Evaluate(mov.elapsed),
						mov.Y.Evaluate(mov.elapsed),
						mov.Z.Evaluate(mov.elapsed)
					),
					mov.Amplitude
				);

				mov.transform.localPosition =  offset + mov.startPosition;
				mov.elapsed                 += Time.deltaTime;
			}

			foreach (RotateAlongCurve rot in rotateAlongCurve)
			{
				rot.transform.localEulerAngles = Vector3.Scale(
					new Vector3(
						rot.X.Evaluate(rot.elapsed),
						rot.Y.Evaluate(rot.elapsed),
						rot.Z.Evaluate(rot.elapsed)
					),
					rot.Amplitude);

				rot.elapsed += Time.deltaTime;
			}

			foreach (ScaleAlongCurve scale in scaleAlongCurves)
			{
				scale.transform.localScale = Vector3.Scale(
					new Vector3(
						scale.X.Evaluate(scale.elapsed),
						scale.Y.Evaluate(scale.elapsed),
						scale.Z.Evaluate(scale.elapsed)
					),
					scale.Amplitude.Multiply(scale.UseSetScale ? scale._startingScale : Vector3.one));

				scale.elapsed += Time.deltaTime;
			}
		}

		[BurstCompile]
		struct UpdateJob : IJobParallelForTransform {

			[ReadOnly]
			public float dt;

			[NativeDisableParallelForRestriction]
			public NativeArray<AnimatedObject> Objects;

			public void Execute(int index, TransformAccess transform)
			{
				AnimatedObject obj = Objects[index];

				if (obj.MoveOverTime)
					transform.position += obj.UnitPerSecond * dt;

				if (obj.RotatesOverTime)
					transform.rotation *= Quaternion.Euler(dt * obj.DeltaPerSecond);

				if (obj.OscillateOverTime) {
					float cos = math.cos(obj.t * obj.Duration);
					float sin = math.sin(obj.t * obj.Duration);

					transform.localPosition =  obj.basePosition + Vector3.Scale(new Vector3(cos, sin, cos), obj.Amplitude);
					obj.t                   += dt;
				}

				Objects[index] = obj;
			}
		}
	}
}