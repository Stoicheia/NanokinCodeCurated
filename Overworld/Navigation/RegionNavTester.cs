using System;
using System.Diagnostics;
using Anjin.MP;
using Anjin.Regions;
using Cysharp.Threading.Tasks;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Overworld.Navigation {
	public class RegionNavTester : SerializedMonoBehaviour {
		public AssetReferenceT<RegionGraphAsset> Graph;
		public string                            PathID;

		private RegionGraphAsset LoadedAsset;

		[NonSerialized, HideInEditorMode, ShowInInspector]
		public MPPath ProcessedPath;

		[Button, HideInEditorMode]
		public async UniTaskVoid RecalcPath()
		{
			if(LoadedAsset == null)
				LoadedAsset = await Graph.LoadAssetAsync();

			var rpath = LoadedAsset.Graph.FindObject<RegionPath>(PathID);
			if(rpath != null) {
				bool ok;
				(ProcessedPath, ok) = await MotionPlanning.CalcRegionPath(rpath);
				if (!ok) DestroyPath();
			}
		}

		[Button, HideInEditorMode]
		public void DestroyPath()
		{
			ProcessedPath = null;
			//RawPath       = null;
		}

		private void OnDrawGizmos()
		{
			if(ProcessedPath != null)
				MotionPlanning.DrawPathInEditor(ProcessedPath, Color.blue);

			/*if(RawPath != null)
				MotionPlanning.DrawPolyLineInEditor(RawPath.vectorPath, Color.red);*/
		}
		/*[NonSerialized]
		public ABPath RawPath;

		[NonSerialized, HideInEditorMode, ShowInInspector]
		public MPPath ProcessedPath;

		TimeSpan lastPathTime;

		[ShowInInspector]
		public string LastPathTime => lastPathTime.ToString();

		[ShowInInspector]
		public string LastPathMS => lastPathTime.TotalMilliseconds.ToString();

		private Stopwatch sw;
		private void      Awake() => sw = new Stopwatch();

		[Button, HideInEditorMode]
		public async UniTaskVoid RecalcPathBoth()
		{
			sw.Start();
			bool ok;
			(RawPath, ok) = await MotionPlanning.CalcRawPath(Point1.position, Point2.position, this);
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



		private void OnDrawGizmos()
		{
			if(ProcessedPath != null)
				MotionPlanning.DrawPathInEditor(ProcessedPath, Color.blue);

			if(RawPath != null)
				MotionPlanning.DrawPolyLineInEditor(RawPath.vectorPath, Color.red);
		}*/
	}
}