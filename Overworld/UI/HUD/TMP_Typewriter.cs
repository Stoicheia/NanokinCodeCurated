using System;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Util.Odin.Attributes;


/// <summary>
/// A simple controller that makes a Text Mesh Pro component type out its text at a set speed.
/// </summary>
public class TMP_Typewriter : SerializedMonoBehaviour, ITextDisplayer
{
	public const float DEFAULT_TYPING_SPEED = 45;

	public enum State
	{
		Off,
		Typing,
		Paused,
		End
	}

	public TMP_Text      TMPText;
	public RectTransform RootRT;

	//[Multiline, VerticalLabel]
	private string _text;

	public string Text
	{
		get { return _text; }
		set { _text = value; }
	}

	public UnityEvent           UE_OnDisplayNew;
	public Action<int, char>    OnShowNewCharacter;
	public Func<TMP_Typewriter, string, string> OnDisplayLine;

	[NonSerialized, ShowInPlay]
	public bool InstantFlag = false;

	public void DisplayNew(GameText text, float speed)
	{
		StartTyping(text.GetString(), speed != -1 ? speed : DEFAULT_TYPING_SPEED);
	}

	public void Pause()
	{
		if (state != State.Typing) return;
		state = State.Paused;
	}

	public void Unpause()
	{
		if (state != State.Paused) return;
		state = State.Typing;
	}

	public void FinishDisplayProcess()
	{
		TMPText.maxVisibleCharacters = TMPText.textInfo.characterCount;
		state                        = State.End;
		DisplayProcessDone           = true;
	}

	[ShowInInspector]
	public bool DisplayProcessDone { get; set; }

	public void StopDisplaying()
	{
		DisplayProcessDone = true;

		state = State.Off;
		// TMPText.text                 = "";
		// characterCount               = 0;
		prevCharacterCount           = 0;
		TMPText.maxVisibleCharacters = 9999999;
		InstantFlag                  = false;
	}

	public State state;

	public float characterCount;
	public int   prevCharacterCount;
	public float typeSpeed;

	// Start is called before the first frame update
	void Awake()
	{
		if(TMPText == null) TMPText = GetComponent<TMP_Text>();
		if (RootRT == null) RootRT  = GetComponent<RectTransform>();

		TMPText.text = "";

		state              = State.Off;
		DisplayProcessDone = false;
	}

	[Button]
	public void StartTyping(string text, float speed = DEFAULT_TYPING_SPEED)
	{

		TMPText.maxVisibleCharacters = 0;

		typeSpeed          = speed;
		characterCount     = 0;
		prevCharacterCount = 0;
		DisplayProcessDone = false;
		state              = State.Typing;

		var txt             = Text = text;
		if (OnDisplayLine != null)
			txt = OnDisplayLine.Invoke(this, txt);

		TMPText.text = Text = txt;


		if (TMPText is TextMeshProUGUI)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(RootRT);
		}

	}

	[Button]
	public void Restart()
	{
		TMPText.maxVisibleCharacters = 0;
		characterCount               = 0;
		prevCharacterCount           = 0;
		state                        = State.Typing;
		DisplayProcessDone           = false;
		InstantFlag                  = false;
		UE_OnDisplayNew.Invoke();
	}

	private static float GetDurationForCharacter(char currentChar)
	{
		switch (currentChar)
		{
			case '.': return 10f;
			case '!': return 10f;
			case ';': return 5f;
			//case ',': return 5f;
		}

		return 1;
	}

	// Update is called once per frame
	void Update()
	{
		if (TMPText != null)
		{
			switch (state)
			{
				case State.Off:
					//Nothing
					break;

				case State.Typing:

					if (characterCount < TMPText.text.Length) {
						char  currentChar = TMPText.text[((int) characterCount - 1).Minimum(0)];
						float duration    = GetDurationForCharacter(currentChar);
						characterCount += typeSpeed * Time.deltaTime / duration;
					}

					TMPText.maxVisibleCharacters = characterCount.Floor();
					if (prevCharacterCount != TMPText.maxVisibleCharacters)
					{
						//Play Sound
						prevCharacterCount = TMPText.maxVisibleCharacters;

						GameSFX.PlayGlobal(GameAssets.Live.Sfx_Typewriter_Tick);

						if(characterCount < TMPText.text.Length - 1)
							OnShowNewCharacter?.Invoke(characterCount.Floor(), TMPText.text[characterCount.Floor()]);
					}

					if (characterCount >= TMPText.text.Length || InstantFlag)
					{
						TMPText.maxVisibleCharacters = TMPText.textInfo.characterCount;
						state                        = State.End;
						DisplayProcessDone           = true;
						InstantFlag                  = false;
					}

					break;

				case State.End:
					//Nothing yet
					break;
			}
		}
	}
}