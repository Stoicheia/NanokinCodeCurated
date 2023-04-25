using System;
using System.Collections.Generic;
using System.Text;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using ImGuiNET;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;
using Util.Components.Timers;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using Random = UnityEngine.Random;
using g = ImGuiNET.ImGui;


#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Anjin.UI {

	[LuaUserdata]
	public struct BustAnimConfig {
		public string mode;
		public string state;
		public string state_eyes;
		public string state_mouth;
		public string state_brows;

		public static BustAnimConfig Default = new BustAnimConfig {
			mode        = null,
			state       = null,
			state_eyes  = null,
			state_mouth = null,
			state_brows = null,
		};
	}

	public class CharacterBustAnimator : SerializedMonoBehaviour {

		// LAYERS
		public enum BustLayer {
			None = 0,
			Base = 1,
			Eyes = 2,
			Brows = 3,
			Mouth = 4,
		}

		[Title("Modes")]
		public CharacterBustModeAsset                     BaseMode;
		public Dictionary<string, CharacterBustModeAsset> AltModes = new Dictionary<string, CharacterBustModeAsset>();

		[Title("Base Layers")]
		[SerializeField] [Inline(keepLabel:true)] public Layer Layer_Base  = new Layer{LayerID = BustLayer.Base,  IsBase = true};
		[SerializeField] [Inline(keepLabel:true)] public Layer Layer_Eyes  = new Layer{LayerID = BustLayer.Eyes,  IsBase = true};
		[SerializeField] [Inline(keepLabel:true)] public Layer Layer_Brows = new Layer{LayerID = BustLayer.Brows, IsBase = true};
		[SerializeField] [Inline(keepLabel:true)] public Layer Layer_Mouth = new Layer{LayerID = BustLayer.Mouth, IsBase = true};

		[Title("Custom Layers")]
		public Layer[] CustomLayers = new Layer[0];


		[Title("Animation")]

		[Required] public BustAnimationConfig AnimConfig;
		[Required] public LipFlapConfig LipFlapConfig;

		[FormerlySerializedAs("Blinking")]     public bool BlinkEnabled;
		[FormerlySerializedAs("BrowWiggling")] public bool BrowWiggleEnabled;

		public bool           OverrideBlinkRate;
		[ShowIf("$OverrideBlinkRate")]
		public AnimationCurve BlinkRate;

		public bool           OverrideBlinkDuration;
		[ShowIf("$OverrideBlinkDuration")]
		public FloatRange     BlinkDuration;

		public bool           OverrideBrowWiggleRate;
		[ShowIf("$OverrideBrowWiggleRate")]
		public AnimationCurve BrowWiggleRate;

		public bool       OverrideBrowWiggleDuration;
		[ShowIf("$OverrideBrowWiggleDuration")]
		public FloatRange BrowWiggleDuration;

		[NonSerialized, ShowInPlay] private ValTimer _blinkTimer;
		[NonSerialized, ShowInPlay] private ValTimer _blinkEyesClosedTimer;

		[NonSerialized, ShowInPlay] private ValTimer _browWiggleTimer;
		[NonSerialized, ShowInPlay] private ValTimer _browsActiveTimer;

		[NonSerialized, ShowInPlay] public int      LipFlapIndexOverride = -1; // Mainly for testing
		[NonSerialized, ShowInPlay] public ValTimer TalkTimer;

		[NonSerialized, ShowInPlay] private ValTimer _nextLipFlapTimer;
		[NonSerialized, ShowInPlay] private ValTimer _lipFlapTimer;
		[NonSerialized, ShowInPlay] private AnimationCurve _lipFlapCurve;

		// Runtime
		//======================================================================

		[NonSerialized, ShowInPlay] private bool _talking;

		[NonSerialized, ShowInPlay] private CharacterBustModeAsset _currentMode;

		[NonSerialized, ShowInPlay] private BustState _currentStateMouth;
		[NonSerialized, ShowInPlay] private BustState _currentStateEyes;
		[NonSerialized, ShowInPlay] private BustState _currentStateBrows;

		[NonSerialized, ShowInPlay] private BustState _currentState;

		[NonSerialized, ShowInPlay] public Dictionary<CharacterBustModeAsset, Dictionary<string, BustState>> StateRegistry;
		[NonSerialized, ShowInPlay] private Dictionary<string, Layer>                                         _layers;

		//[NonSerialized, ShowInPlay] private Vector2 _currentImageSize;
		[NonSerialized, ShowInPlay] private Vector2 _baseImageSize;

		// Blinking
		/*[ShowInPlay] private bool _hasBlink;
		[ShowInPlay] private bool _blink;
		*/

		// Frame sequence
		[ShowInPlay] private int		_frameSeqIndex;
		[ShowInPlay] private ValTimer	_frameSeqTimer;

		public bool DebugImGuiWindow = false;

		public static bool BustsDebugEnabled = false;
		// Properties
		//======================================================================

		private string _mode;
		[ShowInPlay]
		public string Mode {
			get => _mode;

			set {
				if (_mode == value) return;

				_mode = value;
				UpdateModeAndState();
				OnStateChange();
			}
		}

		private string _state;
		[ShowInPlay] public string State { get => _state;
			set {
				if (_state == value) return;

				_state = value;
				UpdateModeAndState();
				OnStateChange();
			}
		}

		private string _stateOverride_Mouth;
		private string _stateOverride_Eyes;
		private string _stateOverride_Brows;

		[ShowInPlay] public string StateOverrideMouth { get => _stateOverride_Mouth;
			set {
				if (_stateOverride_Mouth == value) return;

				_stateOverride_Mouth = value;
				UpdateModeAndState();
				OnStateChange();
			}
		}

		[ShowInPlay] public string StateOverrideEyes { get => _stateOverride_Eyes;
			set {
				if (_stateOverride_Eyes == value) return;

				_stateOverride_Eyes = value;
				UpdateModeAndState();
				OnStateChange();
			}
		}

		[ShowInPlay] public string StateOverrideBrows { get => _stateOverride_Brows;
			set {
				if (_stateOverride_Brows == value) return;

				_stateOverride_Brows = value;
				UpdateModeAndState();
				OnStateChange();
			}
		}

		void UpdateModeAndState()
		{
			if (_mode == null || !AltModes.TryGetValue(_mode, out var altMode)) {
				_currentMode = BaseMode;
			} else {
				_currentMode = altMode;
			}

			Dictionary<string, BustState> reg = StateRegistry[_currentMode];

			{
				if (_state == null || !reg.TryGetValue(_state, out BustState state)) {
					_currentState = _currentMode.Neutral;
				} else {
					_currentState = state;
				}
			}

			_currentStateMouth = null;
			_currentStateEyes  = null;
			_currentStateBrows = null;

			{ if (_stateOverride_Mouth != null && reg.TryGetValue(_stateOverride_Mouth, out BustState s)) _currentStateMouth = s; }
			{ if (_stateOverride_Eyes  != null && reg.TryGetValue(_stateOverride_Eyes,  out BustState s)) _currentStateEyes  = s; }
			{ if (_stateOverride_Brows != null && reg.TryGetValue(_stateOverride_Brows, out BustState s)) _currentStateBrows = s; }
		}


		private void Awake()
		{
			// TODO: Custom layers
			//_layers = new Dictionary<string, Layer>();

			/*if (DebugImGuiWindow) {
				GameInputs.mouseUnlocks.Add("BustTemp");
			}*/

			StateRegistry = new Dictionary<CharacterBustModeAsset, Dictionary<string, BustState>>();

			RegisterMode(BaseMode);
			foreach (CharacterBustModeAsset asset in AltModes.Values) {
				RegisterMode(asset);
			}

			_mode = null;
			_state = null;

			_stateOverride_Mouth = null;
			_stateOverride_Eyes  = null;
			_stateOverride_Brows = null;

			_baseImageSize = Layer_Base.image.rectTransform.sizeDelta;

			UpdateModeAndState();
			OnStateChange();

			_blinkTimer           = new ValTimer();
			_blinkEyesClosedTimer = new ValTimer();

			void RegisterMode(CharacterBustModeAsset asset)
			{
				if (asset == null) return;

				var dic = new Dictionary<string, BustState>();
				StateRegistry[asset] = dic;

				foreach (BustState state in asset.States) {
					if(!state.ID.IsNullOrWhitespace())
						dic[state.ID] = state;
					else {
						this.LogError($"Bust state has empty ID. Asset: {asset.name}");
					}
				}
			}
		}

		private void OnEnable()
		{
			//if(DebugImGuiWindow)
				ImGuiUn.Layout += ImGuiUnOnLayout;

		}

		private void OnDisable()
		{
			//if(DebugImGuiWindow)
				ImGuiUn.Layout -= ImGuiUnOnLayout;
		}

		private void OnDestroy()
		{
			//if(DebugImGuiWindow)
				ImGuiUn.Layout -= ImGuiUnOnLayout;
		}

		private float _debugTalkTime = 10;

		private static StringBuilder _sb = new StringBuilder();

		private void ImGuiUnOnLayout()
		{
			if (GameController.DebugMode && BustsDebugEnabled && ImGui.Begin($"Bust Debug ({name})")) {

				g.InputFloat("", ref _debugTalkTime);
				g.SameLine();
				if (g.Button("Talk")) DoTalk(_debugTalkTime);
				g.SameLine();
				if (g.Button("Stop")) StopTalking();
				g.Separator();


				// MODE
				//==============================================

				AImgui.Text($"Mode: {Mode ?? "null"}");

				if (g.Button("Base (null)"))
					Mode = null;

				g.SameLine();

				foreach (KeyValuePair<string, CharacterBustModeAsset> pair in AltModes) {
					if (pair.Value == null || pair.Key == null) continue;

					g.SameLine();
					if (g.Button(pair.Key))
						Mode = pair.Key;
				}

				g.Separator();



				string _state(string label, string state)
				{
					g.PushID(label);

					AImgui.Text($"{label}:", ColorsXNA.Goldenrod);
					g.SameLine();
					AImgui.Text($"{state ?? "null"}", ColorsXNA.CornflowerBlue);
					g.SameLine();

					/*bool colChange = state == null;
					if(colChange) g.PushStyleColor(ImGuiCol.Button, ColorsXNA.LimeGreen);
					if (g.Button("null")) state = null;
					if(colChange) g.PopStyleColor();*/

					if (AImgui.ToggleButton("null", state == null, ColorsXNA.Tomato, ColorsXNA.MidnightBlue))
						state = null;


					//g.SameLine();

					foreach (KeyValuePair<CharacterBustModeAsset,Dictionary<string,BustState>> pair in StateRegistry) {
						if (pair.Key == null) continue;
						g.PushID(pair.Key.name);

						AImgui.Text(pair.Key.name + ":", ColorsXNA.OrangeRed);
						g.Indent(16);

						foreach (KeyValuePair<string, BustState> states in pair.Value) {
							if (states.Value == null) continue;

							g.PushID(states.Key);

							g.SameLine();

							if (AImgui.ToggleButton(states.Key, state == states.Key, ColorsXNA.Tomato, ColorsXNA.MidnightBlue))
							//if (g.Button(states.Key))
								state = states.Key;

							g.PopID();
						}

						g.Unindent(16);
						g.PopID();
					}

					g.PopID();

					AImgui.VSpace(16);

					return state;
				}

				State				= _state("BASE",  State);
				StateOverrideMouth	= _state("MOUTH", StateOverrideMouth);
				StateOverrideEyes	= _state("EYES",  StateOverrideEyes);
				StateOverrideBrows	= _state("BROWS", StateOverrideBrows);

				AImgui.VSpace(32);

				_sb.Clear();
				_sb.Append("{ ");

				if(Mode != null) {
					_sb.Append("mode = \"");
					_sb.Append(Mode);
					_sb.Append("\", ");
				}

				if(State != null) {
					_sb.Append("state = \"");
					_sb.Append(State);
					_sb.Append("\", ");
				}

				if(StateOverrideMouth != null) {
					_sb.Append("mouth = \"");
					_sb.Append(StateOverrideMouth);
					_sb.Append("\", ");
				}

				if(StateOverrideEyes != null) {
					_sb.Append("eyes = \"");
					_sb.Append(StateOverrideEyes);
					_sb.Append("\", ");
				}

				if(StateOverrideBrows != null) {
					_sb.Append("brows = \"");
					_sb.Append(StateOverrideBrows);
					_sb.Append("\", ");
				}

				_sb.Append("}");

				string str = _sb.ToString();
				if(str != null) {
					AImgui.Text("Table: " + str, ColorsXNA.CornflowerBlue);

					if (g.Button("Copy")) {
						GUIUtility.systemCopyBuffer = str;
					}
				}


				/*AImgui.Text("Base State:", ColorsXNA.Goldenrod);
				g.SameLine();
				if(State == null)
					AImgui.Text("null", ColorsXNA.OrangeRed);
				else {
					string str = State;
					g.InputText("", ref str, 32);
					State = str;
				}

				if (g.Button("null"))
					State = null;

				foreach (KeyValuePair<CharacterBustModeAsset,Dictionary<string,BustState>> pair in StateRegistry) {
					if (pair.Key == null) continue;

					g.Text(pair.Key.name + ":");
					g.Indent(16);

					foreach (KeyValuePair<string, BustState> states in pair.Value) {
						if (states.Value == null) continue;

						g.SameLine();
						if (g.Button(states.Key))
							State = states.Key;
					}

					g.Unindent(16);
					g.Separator();
				}

				AImgui.Text("Mouth Override:", ColorsXNA.Goldenrod);
				g.PushID("mouthOverride");
				g.SameLine();
				if(StateOverrideMouth == null)
					AImgui.Text("null", ColorsXNA.OrangeRed);
				else {
					string str = StateOverrideMouth;
					g.InputText("", ref str, 32);
					StateOverrideMouth = str;
				}

				if (g.Button("null"))
					StateOverrideMouth = null;

				foreach (KeyValuePair<CharacterBustModeAsset,Dictionary<string,BustState>> pair in StateRegistry) {
					if (pair.Key == null) continue;

					g.Text(pair.Key.name + ":");
					g.Indent(16);

					foreach (KeyValuePair<string, BustState> states in pair.Value) {
						if (states.Value == null) continue;

						g.SameLine();
						if (g.Button(states.Key))
							StateOverrideMouth = states.Key;
					}

					g.Unindent(16);
					g.Separator();
				}
				g.PopID();


				AImgui.Text("Eyes Override:", ColorsXNA.Goldenrod);
				g.PushID("eyesOverride");
				g.SameLine();
				if(StateOverrideEyes == null)
					AImgui.Text("null", ColorsXNA.OrangeRed);
				else {
					string str = StateOverrideEyes;
					g.InputText("", ref str, 32);
					StateOverrideEyes = str;
				}

				if (g.Button("null"))
					StateOverrideEyes = null;

				foreach (KeyValuePair<CharacterBustModeAsset,Dictionary<string,BustState>> pair in StateRegistry) {
					if (pair.Key == null) continue;

					g.Text(pair.Key.name + ":");
					g.Indent(16);

					foreach (KeyValuePair<string, BustState> states in pair.Value) {
						if (states.Value == null) continue;

						g.SameLine();
						if (g.Button(states.Key))
							StateOverrideEyes = states.Key;
					}

					g.Unindent(16);
					g.Separator();
				}
				g.PopID();

				AImgui.Text("Brows Override:", ColorsXNA.Goldenrod);
				g.PushID("browsOverride");
				g.SameLine();
				if(StateOverrideBrows == null)
					AImgui.Text("null", ColorsXNA.OrangeRed);
				else {
					string str = StateOverrideBrows;
					g.InputText("", ref str, 32);
					StateOverrideBrows = str;
				}

				if (g.Button("null"))
					StateOverrideBrows = null;

				foreach (KeyValuePair<CharacterBustModeAsset,Dictionary<string,BustState>> pair in StateRegistry) {
					if (pair.Key == null) continue;

					g.Text(pair.Key.name + ":");
					g.Indent(16);

					foreach (KeyValuePair<string, BustState> states in pair.Value) {
						if (states.Value == null) continue;

						g.SameLine();
						if (g.Button(states.Key))
							StateOverrideBrows = states.Key;
					}

					g.Unindent(16);
					g.Separator();
				}
				g.PopID();*/

			}

			ImGui.End();
		}

		public void Update()
		{
			// Update animation

			// Eyes


			// Brows
			// Mouth

			if(_currentState is NormalState normal) {

				NormalState baseNormal = _currentMode.Neutral as NormalState;

				NormalState eyeState	= (_currentStateEyes	as NormalState) ?? normal;
				NormalState mouthState	= (_currentStateMouth	as NormalState) ?? normal;
				NormalState browsState	= (_currentStateBrows	as NormalState) ?? normal;

				bool eyes_closed = false;
				bool brows_alt   = false;
				float mouth_state = 0;


				if (BlinkEnabled/* && eyeState.CanBlink*/) {

					if (!_blinkEyesClosedTimer.done) {
						_blinkEyesClosedTimer.Tick();
						eyes_closed = true;
					}else if (_blinkTimer.Tick()) {

						_blinkEyesClosedTimer.Set((OverrideBlinkDuration ? BlinkDuration : AnimConfig.BlinkDuration).RandomInclusive);
						_blinkTimer.Set((OverrideBlinkRate ? BlinkRate : AnimConfig.BlinkRate).Evaluate(Random.value));
					}
				}

				if (BrowWiggleEnabled && browsState.CanBrowWiggle) {

					if (!_browsActiveTimer.done) {
						_browsActiveTimer.Tick();
						brows_alt = true;
					}else if (_browWiggleTimer.Tick()) {

						_browsActiveTimer.Set((OverrideBrowWiggleDuration ? BrowWiggleDuration : AnimConfig.BrowWiggleDuration).RandomInclusive);
						_browWiggleTimer.Set((OverrideBrowWiggleRate ? BrowWiggleRate : AnimConfig.BrowWiggleRate).Evaluate(Random.value));
					}
				}

				if (_talking && TalkTimer.Tick() && _lipFlapTimer.done) {
					_talking = false;
				}

				// Note(C.L.): This is not the most extensible way to do talking (only closed, open, and half open frames),
				// but I'm leaving that till we have voice support.
				if (_talking && LipFlapConfig != null) {
					if (!_lipFlapTimer.done) {
						_lipFlapTimer.Tick();
						mouth_state = _lipFlapCurve.Evaluate(_lipFlapTimer.norm_0to1);
					} else if (_nextLipFlapTimer.Tick()) {

						if (LipFlapConfig.LipFlaps == null || LipFlapConfig.LipFlaps.Count == 0)
							_lipFlapCurve = LipFlapConfig.DefaultCurve;
						else
							_lipFlapCurve = LipFlapIndexOverride >= 0 ? LipFlapConfig.LipFlaps.SafeGet(LipFlapIndexOverride) : LipFlapConfig.LipFlaps.RandomElement();

						_lipFlapTimer.Set(_lipFlapCurve[_lipFlapCurve.length - 1].time, true);

						_nextLipFlapTimer.Set(LipFlapConfig.LipFlapInbetweenTime.Evaluate(Random.value));
					}

				}

				// Note(C.L. 5-19-22): This may not be the cleanest way to support overriding but it gets the job done (I think)

				{
					Sprite eyes = (eyeState.Eyes_Open ?? normal.Eyes_Open ?? baseNormal?.Eyes_Open);
					if(eyes_closed) {
						var possible_eyes       = (eyeState.Eyes_Closed ?? normal.Eyes_Closed ?? baseNormal?.Eyes_Closed);
						if (possible_eyes) eyes = possible_eyes;
					}

					Layer_Eyes.Set(eyes);
				}

				//Layer_Brows.Set(brows_alt ?		browsState.Brows_Alt	: browsState.Brows);

				Layer_Brows.Set(
					brows_alt ?
						(browsState.Brows_Alt ?? normal.Brows_Alt ?? baseNormal?.Brows_Alt) :
						(browsState.Brows     ?? normal.Brows     ?? baseNormal?.Brows)
				);

				int mouth_state_rounded = Mathf.FloorToInt(mouth_state);

				if (mouthState.Mouth_HalfOpen == null && mouth_state_rounded == 1) {
					mouth_state_rounded = mouth_state > 0.5f ? 2 : 0;
				}

				switch (mouth_state_rounded) {

					case 2:  Layer_Mouth.Set(mouthState.Mouth_Open     ?? normal.Mouth_Open     ?? baseNormal?.Mouth_Open); break;
					case 1:  Layer_Mouth.Set(mouthState.Mouth_HalfOpen ?? normal.Mouth_HalfOpen ?? baseNormal?.Mouth_HalfOpen); break;
					default: Layer_Mouth.Set(mouthState.Mouth_Closed   ?? normal.Mouth_Closed   ?? baseNormal?.Mouth_Closed); break;
				}

			} else if(_currentState is FrameSequenceState frameSequence) {

				if (frameSequence.Frames.Count > 0) {

					if (!_frameSeqTimer.done) {
						_frameSeqTimer.Tick();
					} else {
						FrameSequenceState.Frame frame = frameSequence.Frames.WrapGet(_frameSeqIndex);
						if (frame != null) {
							_frameSeqTimer.Set(frame.DurationOverride > 0 ? frame.DurationOverride : frameSequence.OverallFrameDuration);
							Layer_Base.Set(frame.Sprite);
						} else {
							Layer_Base.Set(null);
						}

						_frameSeqIndex++;
					}

				} else {
					Layer_Base.Set(null);
				}
			}
		}

		public void OnStateChange()
		{
			_frameSeqIndex = 0;
			_frameSeqTimer = new ValTimer();

			UpdateLayerVisibility();

		}

		public void UpdateLayerVisibility()
		{
			Layer_Base.SetActive(true);
			Layer_Eyes.SetActive(true);
			Layer_Brows.SetActive(true);
			Layer_Mouth.SetActive(true);

			for (int i = 0; i < CustomLayers.Length; i++) {
				CustomLayers[i].SetActive(false);
			}

			switch (_currentState) {
				case NormalState normal:
					if(normal.Base)
						Layer_Base.Set(normal.Base);
					else if (_currentMode.Neutral is NormalState baseNormal) {
						Layer_Base.Set(baseNormal.Base);
					}

					Vector2 size = _baseImageSize;
					if (normal.ImageSizeOverride.HasValue)
						size = normal.ImageSizeOverride.Value;

					Layer_Base.Size(size);
					Layer_Eyes.Size(size);
					Layer_Brows.Size(size);
					Layer_Mouth.Size(size);

					for (int i = 0; i < CustomLayers.Length; i++) {
						CustomLayers[i].Size(size);
					}

					break;

				case FrameSequenceState state:
					//Layer_Base.SetActive(false);
					Layer_Eyes.SetActive(false);
					Layer_Brows.SetActive(false);
					Layer_Mouth.SetActive(false);
					break;

			}

		}

		public void Reset()
		{
			_talking = true;
			TalkTimer.Set(0);

			_mode  = null;
			_state = null;

			_stateOverride_Mouth = null;
			_stateOverride_Eyes  = null;
			_stateOverride_Brows = null;

			UpdateModeAndState();
			OnStateChange();
		}

		[ShowInPlay]
		public void DoTalk(float duration = 3)
		{
			_talking = true;
			TalkTimer.Set(duration);
		}

		[ShowInPlay]
		public void StopTalking()
		{
			_talking = false;
			TalkTimer.Set(0);
		}


		[Serializable]
		public struct Layer {

			[HideIf("$IsBase")]
			public bool IsBase;

			public BustLayer LayerID;

			[ValidateInput("@id.IsNullOrWhitespace()")]
			[HideIf("$IsBase")]
			[SerializeField]
			public string CustomID;

			public Image image;

			public void SetActive(bool active) { if (image) image.gameObject.SetActive(active); }

			public void Set(Sprite sprite)
			{
				if(image) {
					if (sprite) {
						SetActive(true);
						image.sprite = sprite;
					} else {
						SetActive(false);
					}
				}
			}

			public void Size(Vector2 size)
			{
				if (image) {
					image.rectTransform.sizeDelta = size;
					image.SetAllDirty();
				}
			}

			#if UNITY_EDITOR
			public class Drawer : OdinValueDrawer<Layer> {
				protected override void DrawPropertyLayout(GUIContent label)
				{
					Layer layer = ValueEntry.SmartValue;

					EditorGUI.BeginChangeCheck();

					GUILayout.BeginHorizontal();

					if(label != null)
						GUILayout.Label(label);

					if (layer.IsBase) {
						layer.LayerID = (BustLayer)EditorGUILayout.EnumPopup(layer.LayerID);
					} else {
						layer.CustomID = EditorGUILayout.TextField(layer.CustomID);
					}
					layer.image = EditorGUILayout.ObjectField(GUIContent.none, layer.image, typeof(Image)) as Image;
					GUILayout.EndHorizontal();

					if (EditorGUI.EndChangeCheck()) {

						UnityEditor.EditorUtility.SetDirty(Property.Tree.UnitySerializedObject.targetObject);
						PrefabUtility.RecordPrefabInstancePropertyModifications(Property.Tree.UnitySerializedObject.targetObject);
					}


					ValueEntry.SmartValue = layer;
				}
			}
			#endif
		}

	}
}