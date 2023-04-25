using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;


public class SynthwaveMesh : SerializedMonoBehaviour
{

	public Mesh gridMesh;

	public MeshFilter filter;

	public Vector2 gridSize;
	public Vector2Int gridDivisions;

	public float radius;
	public float bend;
	public AnimationCurve curve;
	public float randVariation;
	public float rotationSpeed = 1;
	public float rotation;

	//Wave vars
	public bool doWaves = true;
	public float _WaveLength;
	public float _WaveHeight;
	public float _WaveSpeed;
	public float _RandomHeight;
	public float _RandomSpeed;


	private void OnEnable()
	{
		if(gridMesh == null)
		{
			gridMesh = new Mesh();
			filter.mesh = gridMesh;
		}

		if(verts == null)
			verts = new List<Vector3>();

		if(tris == null)
			tris = new List<int>();

		if(normals == null)
			normals = new List<Vector3>();

		if(uvs == null)
			uvs = new List<Vector2>();
	}

	/*private void OnDisable()
	{
		Destroy(gridMesh);
	}*/

	private void OnDestroy()
	{
		Destroy(gridMesh);
	}

	private List<Vector3> verts;
	private List<int> tris;
	private List<Vector3> normals;
	private List<Vector2> uvs;

	private void Update()
	{
		rotation += rotationSpeed * Time.deltaTime;

		UpdateMesh();
	}

	static Vector3 randConst1 = new Vector3(12.9898f, 78.233f, 45.5432f);
	static Vector3 randConst2 = new Vector3(19.9128f, 75.2f, 34.5122f);

	float rand(Vector3 co)
	{
		return Mathf.Repeat(Mathf.Sin(Vector3.Dot(co, randConst1)) * 43758.5453f, 1);
	}

	float rand2(Vector3 co)
	{
		return Mathf.Repeat(Mathf.Sin(Vector3.Dot(co , randConst2)) * 12765.5213f, 1);
	}

	[Button]
	public void UpdateMesh()
	{
		verts.Clear();
		tris.Clear();
		normals.Clear();
		uvs.Clear();

		float xx = 0;
		float yy = 0;
		float sx = gridSize.x / gridDivisions.x;
		float sy = gridSize.y / gridDivisions.y;
		int xSize = gridDivisions.x;

		float angle = rotation;
		float angleInc = 360.0f / gridDivisions.y;


		Vector2 dir;
		Vector2 normal;

		float time = Time.realtimeSinceStartup;

		Vector3 point;

		float distFromCenter;

		//Verts
		for (int y = 0; y < gridDivisions.y + 1; y++, angle += angleInc)
		{
			normal = MathUtil.AnglePosition(angle, 1);
			dir    = normal * radius;

			for (int x = 0; x < gridDivisions.x + 1; x++)
			{
				xx = x * sx; //+ (sx / 2) * (y % 2); //+ Random.Range(-randVariation, randVariation);
				yy = y * sy; //+ Random.Range(-randVariation, randVariation);

				distFromCenter = Mathf.Abs( x - gridDivisions.x / 2.0f ) / (gridDivisions.x / 2.0f );

				float phase0 = 0;
				float phase0_1 = 0;

				if(doWaves)
				{
					phase0 = ( _WaveHeight ) *
					               Mathf.Sin(( time * _WaveSpeed ) + ( xx * _WaveLength ) + ( yy * _WaveLength ) +
					                         rand2(new Vector3(xx, yy, yy)));
					phase0_1 = ( _RandomHeight ) * Mathf.Sin(Mathf.Cos(
						                 rand(new Vector3(xx, yy, yy)) * _RandomHeight *
						                 Mathf.Cos(time * _RandomSpeed * Mathf.Sin(rand(new Vector3(xx, xx, yy))))));
				}


				point = new Vector3(xx, dir.y + bend * curve.Evaluate(distFromCenter), dir.x) + ( new Vector3(0, normal.y, normal.x) * ( phase0 + phase0_1 ) );

				verts.Add(point);
				normals.Add( new Vector3(0, normal.y, normal.x));
				uvs.Add(
					new Vector2(
						(float)x/gridDivisions.x,
						(float)y/gridDivisions.y
					)
				);
			}
		}

		//Tris
		for (int triIndex = 0, vi = 0, y = 0; y < gridDivisions.y; y++, vi++)
		{
			/*if (y == gridDivisions.y - 1)
			{
				tris.Add(vi);
				tris.Add(vi + xSize + 1);
				tris.Add(vi         + 1);

				tris.Add(vi         + 1);
				tris.Add(vi + xSize + 1);
				tris.Add(vi + xSize + 2);
				continue;
			}*/


			for (int x = 0; x < gridDivisions.x; x++, triIndex+=6, vi++)
			{
				tris.Add(vi);
				tris.Add(vi + xSize + 1);
				tris.Add(vi + 1);

				tris.Add(vi + 1);
				tris.Add(vi + xSize + 1);
				tris.Add(vi + xSize + 2);
			}
		}

		gridMesh.Clear();

		gridMesh.SetVertices(verts);
		gridMesh.SetTriangles(tris,0);
		gridMesh.SetNormals(normals);
		gridMesh.SetUVs(0, uvs);

		//gridMesh.RecalculateNormals();
		//gridMesh.RecalculateBounds();

		//s.Stop();

		//Debug.Log(s.ElapsedMilliseconds + " " + s.ElapsedTicks);

		/*gridMesh.SetIndices();
		gridMesh.SetNormals();*/

	}

}
