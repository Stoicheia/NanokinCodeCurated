using Anjin.UI;
using TMPro;

namespace Overworld.Cutscenes {
	public class PartyChatHUD : StaticBoy<PartyChatHUD>  {

		public DialogueTextbox _textbox;
		public TMP_Text        _tmp_name;

		public static DialogueTextbox Textbox  => Live._textbox;
		public static TMP_Text        TMP_Name => Live._tmp_name;

		protected override void OnAwake()
		{
			Textbox.gameObject.SetActive(true);
		}


	}
}