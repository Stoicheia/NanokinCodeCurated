using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.UI {

	// NOTE: Some of these are aliased in LuaUtil.RegisterUserdata()
	[LuaEnum]
	public enum Character {

		None	= 0,

		// Party members
		//==============================
		Nas     = 1,
		Jatz    = 2,
		Serio   = 3,
		Peggie  = 4,
		David   = 5,
		NeAi    = 6,
		Koa     = 7,
		Anthony = 8,

		// Key NPCs
		//==============================
		Saanvi	= 10,
		Brohmer = 11,
		Feng	= 12,
		Dolma	= 13,

		// Nanothanes
		Boltbeard			= 20,
		Brianna				= 21,
		GeneralMillennial	= 22,
		Maria				= 23,

		Luli = 30,
		Ted  = 31,


		// Side NPCs
		//==============================
		BennyBroomer = 50,
		CooperTheSwift = 51,
		ChalkyLeBarron = 52,

		Judy = 70,
		Phil = 71,

		// Event-specific NPCs
		//==============================
		Nelson = 200,
		Marisa = 201,

		// The Aristocrats
		WittCassedy      = 210,
		JeffryMontgomery = 211,
		ChesterMcLarge 	 = 212,

		TestDummy = 1000
	}


	public class CharacterBust : SerializedMonoBehaviour {

		public CharacterBustAnimator       Animator;
		public TMP_Typewriter Typer;

		[ValidateInput("@Character == Character.None")]
		public Character Character;

		public bool   WaitingForTicker;

		private RectTransform _rt;
		private Vector2?      _pivot;
		private Vector2?      _anchorMin;
		private Vector2?      _anchorMax;

		/*private string _mode;
		[ShowInPlay]
		public string Mode {
			get => _mode;
			set {
				_mode = value;
			}
		}*/

		[ShowInPlay] private    bool _hasExpression;
		[ShowInPlay] private    bool _hasBlink;
		[ShowInPlay] private    bool _hasBrows;
		[ShowInPlay] private    bool _hasTalking;

		private void Awake()
		{
			// Note(C.L.): This may not be the most performant but it gets the job done as long as the layers are named correctly.
			Animator = GetComponent<CharacterBustAnimator>();

			_rt        = GetComponent<RectTransform>();
			_anchorMin = _rt.anchorMin;
			_anchorMax = _rt.anchorMax;
			_pivot     = _rt.pivot;
		}

		private void Start()
		{
			if(Typer != null)
				Typer.OnShowNewCharacter += OnShowNewCharacter;
		}

		[ShowInPlay]
		public void DoTalk(float duration = 3) => Animator.DoTalk(duration);

		public void DoTalkForString(string str)
		{
			if (str.Length == 0) return;
			float? time = GetTalkTimeForString(str);
			if (time == null) return;

			Animator.DoTalk(time.Value);

			if (Typer != null) {
				WaitingForTicker = true;
			}
		}

		private void OnShowNewCharacter(int index, char c)
		{
			if (WaitingForTicker && c != '.' && c != ' ') {
				WaitingForTicker = false;
			}
		}

		public void Reset()
		{
			Animator.Reset();

			//Mode             = "";

			if (_pivot.HasValue)	 _rt.pivot     = _pivot.Value;
			if (_anchorMin.HasValue) _rt.anchorMin = _anchorMin.Value;
			if (_anchorMax.HasValue) _rt.anchorMax = _anchorMax.Value;

		}

		public static float? GetTalkTimeForString(string str)
		{
			var just_letters = str.Replace(".", "").Replace("?", "").Replace(",", "").Replace(" ", "");

			if (just_letters.Length == 0)
				return null;

			return Mathf.Clamp((float)just_letters.Length / 20, 0.5f, 6f);
		}
	}
}