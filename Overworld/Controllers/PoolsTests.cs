using System.Collections.Generic;
using Core.Debug;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Overworld.Controllers
{
	public class PoolsTests : SerializedMonoBehaviour, IDebugDrawer
	{
		public string TestAddress;
		public string TestAddress2;

		public int count = 10;
		public List<PooledRef<GameObject>> TestRefs;
		public List<PooledRef<GameObject>> TestRefs2;

		void Start()
		{
			TestRefs = new List<PooledRef<GameObject>>();
			TestRefs2 = new List<PooledRef<GameObject>>();
			DebugSystem.Register(this);
		}

		[Button]
		public void Test()
		{
			if (TestRefs.Count > 0 || TestRefs2.Count > 0) return;

			for (int i = 0; i < count; i++) {
				var new_ref = Pools.Get<GameObject>(TestAddress);
				new_ref.OnInstantiate = obj => {
				};

				TestRefs.Add(new_ref);
			}

			for (int i = 0; i < count; i++) {
				var new_ref = Pools.Get<GameObject>(TestAddress2);
				TestRefs2.Add(new_ref);
			}
		}

		[Button]
		public void Return()
		{
			for (int i = 0; i < TestRefs.Count; i++)
				TestRefs[i].Return();

			for (int i = 0; i < TestRefs2.Count; i++)
				TestRefs2[i].Return();

			TestRefs.Clear();
			TestRefs2.Clear();
		}

		void Update()
		{
			for (int i = 0; i < TestRefs.Count; i++) {
				var r = TestRefs[i];
				if (r.InstantiateTrigger) {
					var obj = r.Object;
					obj.transform.rotation = Random.rotation;
					obj.transform.position = Random.insideUnitSphere * 10f + Vector3.right * 20;
				}
			}

			for (int i = 0; i < TestRefs2.Count; i++) {
				var r = TestRefs2[i];
				if (r.InstantiateTrigger) {
					var obj = r.Object;
					obj.transform.rotation = Random.rotation;
					obj.transform.position = Random.insideUnitSphere * 10f;
				}
			}
		}

		public void OnLayout(ref DebugSystem.State state)
		{
			/*if (ImGui.Begin("PoolsTests")) {
				ImGui.InputInt("Count", ref count);
				if (ImGui.Button("Spawn")) Test();
				if (ImGui.Button("Return")) Return();
			}
			ImGui.End();*/
		}
	}
}