using Anjin.Nanokin;
using Anjin.Scripting;
using Cinemachine;
using UnityEngine;

namespace Overworld {
	public class LuaVCam : MonoBehaviour {

		public async void Start()
		{
			CinemachineVirtualCamera vcam = GetComponent<CinemachineVirtualCamera>();
			//await Lua.initTask;
			await GameController.TillIntialized();
			Lua.RegisterToLevelTable(vcam);
		}

	}
}