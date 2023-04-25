using System;
using Sirenix.OdinInspector;

namespace Anjin.Scripting {
	[LuaUserdata]
	public class LuaContainerTestComponent : SerializedMonoBehaviour {

		public LuaScriptContainer Container1 = new LuaScriptContainer(true);

		private void Start()
		{
			Container1.OnStart(this, null, "test_api");
		}

		private void Update()
		{
			Container1.OnUpdate();
		}

	}
}