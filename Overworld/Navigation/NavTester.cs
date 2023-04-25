using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Anjin.MP;
using Cysharp.Threading.Tasks;
using Drawing;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Navigation {
	public class NavTester : SerializedMonoBehaviour {

		public bool AutoUpdate = true;

		[OnValueChanged("DoUpdate")]
		public GraphLayer Layer;

		public Color ProcessedColor = ColorsXNA.Goldenrod;
		public Color RawColor		= ColorsXNA.Red;

		public Transform Point1;
		public Transform Point2;

		public Vector3    _prevPoint1;
		public Vector3    _prevPoint2;

		[NonSerialized]
		public ABPath RawPath;

		[NonSerialized, HideInEditorMode, ShowInInspector]
		public MPPath ProcessedPath;

		TimeSpan lastPathTime;

		[ShowInInspector]
		public string   LastPathTime => lastPathTime.ToString();

		[ShowInInspector]
		public string   LastPathMS => lastPathTime.TotalMilliseconds.ToString();

		private Stopwatch sw;
		private void      Awake()
		{
			sw = new Stopwatch();
		}

		private void Start()
		{
			DoUpdate();
		}

		private void Update()
		{
			if (AutoUpdate && (_prevPoint1 != Point1.position || _prevPoint2 != Point2.position)) {
				DoUpdate();
			}
		}

		private void DoUpdate()
		{
			_prevPoint1 = Point1.position;
			_prevPoint2 = Point2.position;

			RecalcPathBoth();
		}

		[Button, HideInEditorMode]
		public async UniTaskVoid RecalcPathBoth()
		{
			sw.Start();
			bool ok;
			(RawPath, ok) = await MotionPlanning.CalcRawPath(Point1.position, Point2.position, this, new CalcSettings{layer = Layer});
			if(ok) (ProcessedPath, ok) = MotionPlanning.ProcessRawPath(RawPath);
			if(!ok) DestroyPath();
			sw.Stop();
			lastPathTime = sw.Elapsed;
			sw.Reset();
		}

		[Button, HideInEditorMode]
		public async UniTaskVoid RecalcPath()
		{
			sw.Start();
			(MPPath p, bool ok) = await MotionPlanning.CalcPath(Point1.position, Point2.position);
			if (ok) {
				ProcessedPath = p;
				RawPath       = null;
			} else DestroyPath();
			sw.Stop();
			lastPathTime = sw.Elapsed;
			sw.Reset();
		}

		[Button, HideInEditorMode]
		public void DestroyPath()
		{
			ProcessedPath = null;
			RawPath       = null;
		}

		private void OnDrawGizmos()
		{
			if(Point1 != null)
				Draw.WireSphere(Point1.position, 0.15f, Color.magenta);


			if(Point2 != null)
				Draw.WireSphere(Point2.position, 0.15f, Color.green);

			if(ProcessedPath != null)
				MotionPlanning.DrawPathInEditor(ProcessedPath, ProcessedColor);

			if(RawPath != null)
				MotionPlanning.DrawPolyLineInEditor(RawPath.vectorPath, RawColor);
		}

	}
}