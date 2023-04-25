using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Drawing
{

	[ExecuteAlways]
	public class BezierEditor : MonoBehaviour
	{
		public List<Vector3> SetPoints;
		[HideInInspector] public List<Vector3> ControlPoints;
		[SerializeField] private GameObject pointPrefab;
		[SerializeField] private LineRenderer curveRenderer;
		[SerializeField] private LineRenderer guideRenderer;
		[SerializeField] public bool renderGuides;
		[SerializeField] public bool render;
		[SerializeField] private List<GameObject> pointObjects;

		public int PointsCount => pointObjects.Count;

		public Vector3 StartPoint => ControlPoints.First();
		public Vector3 EndPoint => ControlPoints.Last();

		public bool Render
		{
			get => render;
			set => render = value;
		}

		public bool RenderGuides
		{
			get => renderGuides;
			set => renderGuides = value;
		}

		private void Awake()
		{
			SetPoints = new List<Vector3>();
		}

		private void Update()
		{
			Draw();
		}

		public void ProcessPoints()
		{
			pointObjects.Clear();
			foreach (SphereCollider col in GetComponentsInChildren<SphereCollider>())
			{
				pointObjects.Add(col.gameObject);
			}
		}

		public void ResetPoints()
		{
			foreach (var v in pointObjects)
			{
				DestroyImmediate(v.gameObject);
			}
			pointObjects.Clear();
		}

		public void SpawnPoint()
		{
			GameObject obj = Instantiate(pointPrefab, Vector3.zero, Quaternion.identity);
			obj.transform.parent = transform;
			obj.name = "ControlPoint";
			pointObjects.Add(obj);
		}

		private void Draw()
		{
			for (int i = 0; i < Math.Min(SetPoints.Count, pointObjects.Count); i++)
			{
				pointObjects[i].transform.localPosition = SetPoints[i];
			}

			ControlPoints.Clear();
			for (int i = 0; i < pointObjects.Count; i++)
			{
				ControlPoints.Add(pointObjects[i].transform.localPosition);
			}
			guideRenderer.positionCount = ControlPoints.Count;
			for (int i = 0; i < ControlPoints.Count; i++)
			{
				guideRenderer.SetPosition(i, ControlPoints[i]);
			}

			List<Vector3> curve = BezierCurve.PointList3(ControlPoints, 0.01f);

			curveRenderer.positionCount = curve.Count;
			for (int i = 0; i < curve.Count; ++i)
			{
				curveRenderer.SetPosition(i, curve[i]);
			}

			guideRenderer.gameObject.SetActive(renderGuides);
			curveRenderer.gameObject.SetActive(render);
		}

	}
}