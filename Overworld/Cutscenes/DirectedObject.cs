using Anjin.Scripting;
using MoonSharp.Interpreter;
using SplineMesh;
using UnityEngine;

namespace Overworld.Cutscenes
{
	/// <summary>
	/// A generic gameobject that's part of the cutscene.
	///
	/// Was used for the ticket initially to move along a spline,
	/// but we all know how that ended up looking.
	/// </summary>
	[LuaUserdata]
	public class DirectedObject : DirectedBase
	{
		public Directions directions = new Directions
		{
			moveSpeed = 1
		};

		public Spline spline;
		public int    splineIndex;

		public  float _splineProgress;
		private bool  _spawned;

		public DirectedObject(string address, Table options) : base(options)
		{
			this.address = address;
		}

		public override void OnStart(Coplayer coplayer, bool auto_spawn = true)
		{
			// Instantiate the actor (if needed)
			// ----------------------------------------
			if (gameObject == null && loadedPrefab != null)
			{
				gameObject                    = Object.Instantiate(loadedPrefab, coplayer.transform, true);
				gameObject.transform.position = Vector3.zero;
				_spawned                      = true;
			}

			if (initialPosition.AsString(out string str))
			{
				Transform transform = coplayer.transform.Find(str);
				if (transform)
				{
					spline = transform.GetComponent<Spline>();

					gameObject.transform.position = transform.TransformPoint(spline.GetSample(0).location);
				}
			}
		}

		public override void Update()
		{
			if (directions.moving)
			{
				// directions.moveSplineIndex
				CurveSample sample = spline.GetSample(_splineProgress);
				gameObject.transform.position = spline.transform.TransformPoint(sample.location);

				_splineProgress += Time.deltaTime * directions.moveSpeed * 0.15f;
				if (_splineProgress >= splineIndex)
				{
					_splineProgress   = splineIndex;
					directions.moving = false;
				}
			}
		}

		public override void OnStop(Coplayer coplayer)
		{
			base.OnStop(coplayer);

			spline          = null;
			splineIndex     = 0;
			_splineProgress = 0;
			directions = new Directions
			{
				moveSpeed = 1
			};

			base.OnStop(coplayer);
		}

		public override void Release()
		{
			base.Release();
			if (!_spawned)
				return;

			Object.Destroy(gameObject);

			_spawned   = false;
			gameObject = null;
		}

		public struct Directions
		{
			public bool  moving;
			public float moveSpeed;
			public int   moveSplineIndex;
		}
	}
}