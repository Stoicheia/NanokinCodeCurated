using System;
using System.Collections.Generic;
using System.Linq;
using PathCreation;
using Sirenix.OdinInspector;
using SplineMesh;
using UnityEngine;
using Vexe.Runtime.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Anjin.Nanokin.Map
{
	public interface IMover {
		void RegisterMoveable(IMoveable moveable);
	}

	public interface IMoveable {
		Vector3 Velocity { get; set; }
	}

	public interface IPathTrainCar {
		Vector3 	TargetPos { get; set; }
		Quaternion 	TargetRot { get; set; }
	}

	public class PathMover : SerializedMonoBehaviour, IMover {
		public enum MoverType 		{ Single, Train }
		public enum PluginUsing 	{ SplineMesh, PathCreator, AnjinPath }

		[EnumToggleButtons]
		public PluginUsing Plugin;

		[ShowIf("IsPathCreator")]
		public PathCreator 	Path;

		[ShowIf("IsSplineMesh")]
		public Spline 		Spline;

		[ShowIf("IsAnjinPath")]
		public IAnjinPathHolder PathHolder;

		public bool UsePathRotation = true;

		[EnumToggleButtons]
		public MoverType Type;

		[ShowIf("@this.Type == Anjin.Nanokin.Map.PathMover.MoverType.Train")]
		public TrainConfig Train = new TrainConfig(2);

		[NonSerialized, HideInEditorMode]
		public List<IPathTrainCar> TrainCars;

		public bool IsSplineMesh  => Plugin == PluginUsing.SplineMesh;
		public bool IsPathCreator => Plugin == PluginUsing.PathCreator;
		public bool IsAnjinPath   => Plugin == PluginUsing.AnjinPath;

		public float Speed;

		[HideInEditorMode]
		public float DistanceAlongSpline;

		[ShowInInspector]
		private Matrix4x4 _baseRotation;

		private Vector3 _prevPosition;

		private List<IMoveable> _movables;

		protected virtual void Awake()
		{
			if (_movables == null)
				_movables = new List<IMoveable>();

			TrainCars     = GetComponentsInChildren<IPathTrainCar>().ToList();
			_baseRotation = Matrix4x4.Rotate(transform.rotation);

			_prevPosition = transform.position;
		}

		void Update()
		{
			foreach (IMoveable moveable in _movables) {
				moveable.Velocity = Vector3.zero;
			}

			DistanceAlongSpline += Speed * Time.deltaTime;
			OnMove();
		}

		public void RegisterMoveable(IMoveable moveable)
		{
			if (_movables == null)
				_movables = new List<IMoveable>();

			_movables.AddIfNotExists(moveable);
		}

		public virtual void OnMove()
		{

			if(Type == MoverType.Single) {
				if (GetSplineSample(out var pos, out var rot)) {
					Vector3 vel = pos - _prevPosition;

					transform.position = pos;
					if(UsePathRotation)
						transform.rotation = rot.normalized;

					_prevPosition      = pos;

					foreach (IMoveable moveable in _movables) {
						moveable.Velocity = vel;
					}
				}
			} else if(TrainCars.Count > 0) {
				for (int i = 0; i < TrainCars.Count; i++) {
					if (GetTrainSegment(out var p1, out var p2, i)) {
						DebugDraw.DrawMarker(p1, 0.5f, Color.red, 0, false);
						DebugDraw.DrawMarker(p2, 0.5f, Color.green, 0, false);
						TrainCars[i].TargetPos = Vector3.Lerp(p1, p2, 0.5f);

						if(UsePathRotation)
							TrainCars[i].TargetRot = Quaternion.LookRotation(( p1 - p2 ).normalized);
					}
				}

				/*transform.position = TrainCars[0].position;
				transform.rotation = TrainCars[0].rotation;*/
			}
		}

		public bool GetSplineSample(out Vector3 position, out Quaternion rotation)
		{
			position = Vector3.zero;
			rotation = Quaternion.identity;

			Matrix4x4 rot = _baseRotation;
			//rotation = _baseRotation;

			if(Plugin == PluginUsing.SplineMesh) {
				if (Spline == null) return false;

				DistanceAlongSpline = DistanceAlongSpline % Spline.Length;
				CurveSample sample = Spline.GetSampleAtDistance(DistanceAlongSpline);
				position = Spline.transform.position + sample.location;
				rotation = sample.Rotation;
				return true;
			} else if(IsAnjinPath) {

				if (PathHolder.Path.GetPositionAndRotationAtDistance(out position, out Quaternion pathRotation, DistanceAlongSpline)) {
					rotation = (Matrix4x4.Rotate(pathRotation) * _baseRotation).rotation;
					return true;
				}
				return false;

			} else {
				if (Path == null) return false;

				DistanceAlongSpline = DistanceAlongSpline % Path.path.length;

				position = Path.path.GetPointAtDistance(DistanceAlongSpline);
				rotation = Path.path.GetRotationAtDistance(DistanceAlongSpline);
				return true;
			}
		}

		public bool GetTrainSegment(out Vector3 p1, out Vector3 p2, int segment)
		{
			p1 = Vector3.zero;
			p2 = Vector3.zero;

			var len1 = DistanceAlongSpline + GetLengthToSegment(segment);
			var len2 = DistanceAlongSpline + GetLengthToSegment(segment + 1);

			if(Plugin == PluginUsing.SplineMesh) {
				if (Spline == null) return false;

				len1 %= Spline.Length;
				len2 %= Spline.Length;

				CurveSample sample1 = Spline.GetSampleAtDistance(len1);
				CurveSample sample2 = Spline.GetSampleAtDistance(len2);

				p1 = sample1.location + Spline.transform.position;
				p2 = sample2.location + Spline.transform.position;

				return true;
			} else {
				if (Path == null) return false;

				len1 %= Path.path.length;
				len2 %= Path.path.length;

				p2 = Path.path.GetPointAtDistance(len2);

				var dist_to_end = Mathf.Abs(len2 - Path.path.length);

				if(Path.path.isClosedLoop || len1 >= len2 || dist_to_end < ( Train.SegmentLength / 2 )) {
					p1 = Path.path.GetPointAtDistance(len1);

					if(!Path.path.isClosedLoop ||dist_to_end < ( Train.SegmentLength / 2 ))
						p2 = Path.path.GetPointAtDistance(0) + ( -Path.path.GetDirectionAtDistance(0) * dist_to_end );
				} else {
					var dir = Path.path.GetDirectionAtDistance(Path.path.length, EndOfPathInstruction.Stop);
					p1 = Path.path.GetPointAtDistance(Path.path.length, EndOfPathInstruction.Stop);
					p1 = p1 + dir * len1;
				}

				return true;
			}

		}

		public float GetLengthToSegment(int segment)
		{
			if (!Train.VaryingLengths) {
				return Train.SegmentLength * segment * (Train.Dir == TrainConfig.Direction.Negative ? 1 : -1);
			} else {
				float length = 0;

				for (int i = 0; i < segment; i++)
					length += Train.SegmentLengths[i];

				return length * (Train.Dir == TrainConfig.Direction.Negative ? 1 : -1);
			}
		}

		[InlineProperty, HideLabel, BoxGroup("Train")]
		public struct TrainConfig
		{
			public enum Direction { Positive, Negative }

			[Range(1, 100), OnValueChanged(nameof(UpdateLengths))]
			public int 		Cars;
			public bool 	VaryingLengths;

			[EnumToggleButtons]
			public Direction Dir;

			[ShowIf("@!this.VaryingLengths")]
			public float 	SegmentLength;

			[ShowIf("@this.VaryingLengths")]
			public float[]	SegmentLengths;

			public TrainConfig(int cars)
			{
				Cars 			= cars;
				VaryingLengths 	= false;
				Dir = Direction.Positive;
				SegmentLength = 0;
				SegmentLengths = new float[Cars];
			}

			public void UpdateLengths()
			{
				if(SegmentLengths != null && SegmentLengths.Length == Cars) return;
				SegmentLengths = new float[Cars];
			}
		}

		#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			if (TrainCars == null) return;
			for (int i = 0; i < TrainCars.Count; i++) {
				Gizmos.DrawWireSphere(TrainCars[i].TargetPos, 0.5f);
				Handles.Label(TrainCars[i].TargetPos + Vector3.up * 3, i.ToString());
			}
		}
		#endif
	}
}