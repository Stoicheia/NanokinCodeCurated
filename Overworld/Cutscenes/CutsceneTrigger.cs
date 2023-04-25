using System;
using Anjin.Nanokin.Map;

namespace Overworld.Cutscenes {
	public class CutsceneTrigger : Trigger
	{

		public static Action OnEnterCutscene;
		public Cutscene Cutscene;

		public override void OnTrigger()
		{
			base.OnTrigger();

			if (!Cutscene.playing)
			{
				Cutscene.Play();
				OnEnterCutscene?.Invoke();
			}
		}
	}
}