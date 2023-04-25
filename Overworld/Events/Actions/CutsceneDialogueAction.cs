using System;
using System.Collections.Generic;
using Anjin.EventSystemNS.Actions;
using Anjin.UI;
using Anjin.Util;
using UnityEngine;

namespace Anjin.Nanokin.Actions
{
	[Serializable]
	[EventActionMetadata("Cutscene Dialogue")]
	public class CutsceneDialogueAction : EventAction
	{
		public enum TextboxMode { HUD, SpeechBubble }

		public List<GameText> Lines;
		public WorldPoint     Point;

		public TextboxMode Mode;

		//Settings
		public bool   PlayerControlled = true;
		public float  CharsPerSecond   = 125;
		public string CharacterName;

		/*
		public float LineAdvanceDuration = 1.0f;
		public float EndWaitDuration     = 2.0f;
		*/

		public Action DialogueFinished;

		public override void Init()
		{
			Lines            = new List<GameText>();
			Point            = new WorldPoint();
			PlayerControlled = true;
			Mode             = TextboxMode.SpeechBubble;
			CharacterName    = "";
		}

		public override void InitSockets() { }

		[Newtonsoft.Json.JsonIgnore] public bool EditorSettingsExpand = false;

		public static CutsceneDialogueAction Create(List<GameText> _Lines)
		{
			var a = Create<CutsceneDialogueAction>();
			a.Lines = _Lines;
			return a;
		}
	}

	[EventActionImplementation(typeof(CutsceneDialogueAction), "Nanokin")]
	public class CutsceneDialogueActionImpl : NanokinActionImpl<CutsceneDialogueAction>
	{
		public SpeechBubble Bubble;
		public bool         Started;

		public override bool Blocking => true;
		public          bool _done = false;
		public override bool Done => _done;

		public override void OnDestroy()
		{
			//GameObject.DestroyImmediate(balloon.gameObject, false);
		}

		public override void OnEnter()
		{
			if (Action.Lines.Count == 0)
			{
				_done = true;
				return;
			}

			if (Action.Mode == CutsceneDialogueAction.TextboxMode.HUD)
			{
				//DialogueTextHUD.Live.ShowDialogueLines(Handler.Lines, OnTextboxFinished);
			}
			else if (Action.Mode == CutsceneDialogueAction.TextboxMode.SpeechBubble)
			{
				var bubble = GameHUD.SpawnSpeechBubble();

				Bubble = bubble;
				Bubble.SetLines(Action.Lines);

				Bubble.UE_OnDone.AddListener(OnTextboxFinished);
				Bubble.hudElement.SetPositionModeWorldPoint(Action.Point, Vector3.up * 1.5f);

				Bubble.StartActivation();
				Bubble.CloseOnDone = true;
			}
		}

		public override void OnUpdate()
		{
			if (Action.Mode == CutsceneDialogueAction.TextboxMode.HUD) { }
			else if (Action.Mode == CutsceneDialogueAction.TextboxMode.SpeechBubble)
			{
				//Advance
				/*if (balloon.Done && lines.Count > 0)
				{
					_current = lines.Dequeue();

					//balloon.ConfigTextRenderer.Text = currentLine.GetLine(AnjinTextLine.LineLanguage.English);

					GameObject obj = GameObject.Instantiate(Config.Balloons.SpeechBase);
					balloon                                 = obj.GetComponent<Balloon>();
					balloon.ConfigTextRenderer.Text         = _current.GetLine(LocalizedText.Langauge.English);
					balloon.textTypewriter.playerControlled = Handler.PlayerControlled;
					balloon.Actor                           = actor;
					obj.transform.SetParent(actor.transform);
				}*/
			}
		}

		public void OnTextboxFinished() => _done = true;

		public override void OnExit()
		{
			if (Action.Mode == CutsceneDialogueAction.TextboxMode.SpeechBubble && Bubble != null)
			{
				GameHUD.Live.Elements.Remove(Bubble.hudElement);
				Bubble.gameObject.Destroy();
				Action.DialogueFinished?.Invoke();
			}
		}
	}
}