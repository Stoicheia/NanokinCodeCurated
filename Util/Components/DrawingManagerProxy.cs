using System;
using System.Collections.Generic;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Util.Components {
	public class DrawingManagerProxy : StaticBoy<DrawingManagerProxy>, IDrawGizmos {

		[ShowInInspector]
		private static List<IDrawGizmos>       Subdrawers = new List<IDrawGizmos>();

		[ShowInInspector]
		private static List<IShouldDrawGizmos> ExclusiveSubdrawers = new List<IShouldDrawGizmos>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void Init()
		{
			Subdrawers          = new List<IDrawGizmos>();
			ExclusiveSubdrawers = new List<IShouldDrawGizmos>();
		}

		public DrawingManagerProxy() => DrawingManager.Register(this);

		public void DrawGizmos()
		{
			try {

				var cnt = Subdrawers.Count;
				for (int i = 0; i < cnt; i++) {
					IDrawGizmos drawer = Subdrawers[i];
					drawer.DrawGizmos();
				}

				cnt = ExclusiveSubdrawers.Count;
				for (int i = 0; i < cnt; i++) {
					IShouldDrawGizmos drawer = ExclusiveSubdrawers[i];
					if(drawer.ShouldDrawGizmos())
						drawer.DrawGizmos();
				}
			} catch (Exception e) {
				if (e is NullReferenceException) {
					for (int i = Subdrawers.Count - 1; i >= 0; i--) {
						if (Subdrawers[i] == null)
							Subdrawers.RemoveAt(i);
					}

					for (int i = ExclusiveSubdrawers.Count - 1; i >= 0; i--) {
						if (ExclusiveSubdrawers[i] == null)
							ExclusiveSubdrawers.RemoveAt(i);
					}
				}
			}
		}

		public static void Register(IDrawGizmos   drawer)
		{
			if(drawer is IShouldDrawGizmos)
				ExclusiveSubdrawers.Add(drawer as IShouldDrawGizmos);
			else
				Subdrawers.Add(drawer);
		}

		public static void Deregsiter(IDrawGizmos drawer)
		{
			if(drawer is IShouldDrawGizmos)
				ExclusiveSubdrawers.Remove(drawer as IShouldDrawGizmos);
			else
				Subdrawers.Remove(drawer);
		}
	}
}