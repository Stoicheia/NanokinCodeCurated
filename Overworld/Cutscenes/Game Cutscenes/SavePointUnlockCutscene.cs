using Anjin.Scripting;
using Anjin.UI;
using Cysharp.Threading.Tasks;
using Overworld.Park_Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Cutscenes.SavePointUnlockCutscene
{
	[LuaUserdata(staticAuto: true)]
	public class SavePointUnlockCutscene : StaticBoy<SavePointUnlockCutscene>
	{
		public Camera     Camera;
		public Cutscene   Cutscene;
		public HUDElement Text;
		public AnjinSDF   TextEffect;

		public override void Awake()
		{
			Text.gameObject.SetActive(true);
			Camera.gameObject.SetActive(false);

			Lua.OnReady(async () => {
				await UniTask2.Frames(10);
				if (Cutscene.coplayer == null) {
					this.LogError("Cutscene coplayer missing! (Waited 10 frames after Lua was ready)");
				} else {
					Cutscene.coplayer.afterComplete = () => {
						Camera.gameObject.SetActive(false);
					};
				}
			});
		}

		[Button, ShowInInspector]
		public static void StartCutscene(SavePoint point, bool unlocking)
		{
			Live.Camera.gameObject.SetActive(true);
			Live.Cutscene.coplayer.afterStoppedTmp += () => Live.Camera.gameObject.SetActive(false);

			Live.Cutscene.runningScript["save_point"]  = point;
			Live.Cutscene.runningScript["text"]        = Live.Text;
			Live.Cutscene.runningScript["text_effect"] = Live.TextEffect;
			Live.Cutscene.runningScript["unlocking"]   = unlocking;
			Live.Cutscene.Play();
		}
	}
}