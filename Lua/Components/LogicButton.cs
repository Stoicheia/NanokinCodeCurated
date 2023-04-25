using System;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Util.Components;
using Util.Odin.Attributes;

namespace Anjin.Scripting
{
	[AddComponentMenu("Anjin: Game Building/Logic Button")]
	[LuaUserdata]
	public class LogicButton : MonoBehaviour
	{
		public const float DEBOUNCE_TIME = 0.125f;

		public static string OnPressName = "on_pressed";

		[SerializeField, Optional]            private Animator  Animator;
		[SerializeField, HideIf("@Animator")] private Transform AnimationObject;
		[SerializeField, HideIf("@Animator")] private Vector3   AnimationOffset;
		[SerializeField]                      private Collider  Collider;
		[SerializeField]                      private AudioDef  SFX_Down;
		[SerializeField]                      private AudioDef  SFX_Up;

		[Tooltip("When in auto-actuate mode, the button will automatically fire events as soon as it is pressed and released. In Manual mode, events must be fired through animation.")]
		[SerializeField] private bool AutoActuate = false;

		[InfoBox("The Lua function will be invoked once when the button is actuated.", "IsSingleEvent")]
		[InfoBox("The Lua function will be invoked twice: once when it is pressed down and once when released. A single string argument is passed to detect which it is. ('press' or 'release')", "IsPressReleaseEvent")]
		[SerializeField] private Actuations Type;

		[Title("Lua")]
		[SerializeField, Optional] private LuaAsset Script;
		[SerializeField] private string FunctionName;

		public int id;

		private static readonly int AnimIDPressed = Animator.StringToHash("Pressed");

		private AnimationModes _animMode;
		private Vector3        _animBasePos;

		public enum Actuations { Single, PressAndRelease }

		public enum AnimationModes { Mecanim, Script }

		// new stuff
		public string  ScriptID = "";

		public bool Pressed;	// What state the button is in.
		public bool Locked;		// If the button is locked in it's current state. This will keep the player from interacting with it either way.


		[NonSerialized] public Table   Container;
		[NonSerialized] public Table   Config;
		[NonSerialized] public Closure OnPressLua;

		[NonSerialized, ShowInPlay]
		public bool IsPlayerCurrentlyInteracting;

		private async void Start()
		{
			IsPlayerCurrentlyInteracting = false;

			await GameController.TillInitAndLevelLoaded();

			string id = ScriptID;
			if (id.IsNullOrWhitespace())
				id = name;

			if (Lua.FindFirstGlobal(id, out DynValue val, out Table script)) {
				if (val.AsFunction(out Closure clsr)) {
					OnPressLua = clsr;
				} else if (val.AsTable(out Table tbl)) {
					Config = tbl;
				}

				Container = script;
			}

			if (Config != null) {
				if (Config.TryGet("pressed", out bool pressed)) {
					Set(pressed);
				}

				if (Config.TryGet("locked", out bool locked) && locked) {
					Lock();
				}

				if (Config.TryGet("id", out int _id)) {
					this.id = _id;
				}

				Lua.glb_import_index(Config, this);
				//Lua.Invoke(Lua.envGlobals, "import_index", new object[] { Config, this });
			}

			// TODO(C.L.): Make button register itself as a symbol in the Lua global table if no appropriate table is found

			UpdateAnimationState();
		}

		private void OnValidate() {
			if (Animator == null) Animator = GetComponent<Animator>();
		}

		private void OnEnable()
		{
			if (Animator != null)
			{
				_animMode = AnimationModes.Mecanim;
			}
			else
			{
				_animMode    = AnimationModes.Script;
				_animBasePos = AnimationObject.localPosition;
			}

			// REGISTER EVENTS
			// ----------------------------------------
			if (Collider.isTrigger)
			{
				TriggerEvents events = Collider.AddComponent<TriggerEvents>();
				events.onTriggerEnter = onTriggerEnter;
				events.onTriggerExit  = onTriggerExit;
			}
			else
			{
				CollisionEvents events = Collider.AddComponent<CollisionEvents>();
				events.onCollisionEnter += onCollisionEnter;
				events.onCollisionExit  += onCollisionExit;
			}
		}

		private void OnDisable()
		{
			Collider.RemoveComponent<CollisionEvents>();
			Collider.RemoveComponent<TriggerEvents>();
		}

		private void onCollisionEnter([NotNull] Collision collision) {
			if (collision.gameObject.HasComponent<PlayerActor>()) OnPressedByPlayer();
		}

		private void onCollisionExit([NotNull] Collision collision) {
			if (collision.gameObject.HasComponent<PlayerActor>()) OnReleasedByPlayer();
		}

		private void onTriggerEnter(Collider other) {
			if (other.HasComponent<PlayerActor>()) OnPressedByPlayer();
		}

		private void onTriggerExit(Collider other) {
			if (other.HasComponent<PlayerActor>()) OnReleasedByPlayer();
		}

		private void OnPressedByPlayer()
		{
			IsPlayerCurrentlyInteracting = true;

			if (Pressed || Locked) return;
			GameSFX.Play(SFX_Down, transform);
			Set(true);

		}

		private void OnReleasedByPlayer()
		{
			IsPlayerCurrentlyInteracting = false;

			if (!Pressed || Locked) return;
			GameSFX.Play(SFX_Up, transform);
			Set(false);
		}

		public void Set(bool state, bool forceChange = false)
		{
			bool changed = state != Pressed || forceChange;

			Pressed = state;

			if (changed) {
				if (Pressed) {
					Debug.Log("Pressed");

					if(OnPressLua != null)
						Lua.Invoke(OnPressLua, new [] {this});

					if (Container != null && Config != null && Config.TryGet(OnPressName, out Closure on_press))
						Lua.RunPlayer(Container, on_press, new [] {this});

				} else {
					// Call release
					Debug.Log("Release");
				}
			}

			if (AutoActuate)
				Actuate(Pressed);

			UpdateAnimationState();
		}

		[ShowInPlay]
		public void Lock() {
			Locked = true;
		}

		[ShowInPlay]
		public void Lock(bool state)
		{
			if(state != Pressed)
				Set(state);

			Locked = true;
		}

		[ShowInPlay]
		public void Unlock()
		{
			Locked = false;
			if (Pressed && !IsPlayerCurrentlyInteracting) {
				Set(false, true);
			} else if(!Pressed && IsPlayerCurrentlyInteracting) {
				Set(true, true);
			}
		}

		public void UpdateAnimationState()
		{
			switch (_animMode) {
				case AnimationModes.Mecanim: Animator.SetBool(AnimIDPressed, Pressed); break;
				case AnimationModes.Script:  AnimationObject.transform.localPosition = _animBasePos + (Pressed ? AnimationOffset : Vector3.zero); break;
			}
		}

		[UsedImplicitly]
		public void Actuate(int i)
		{
			Actuate(i > 0);
		}

		[UsedImplicitly]
		public void Actuate(bool state)
		{
			if (Type == Actuations.Single && state)
			{
				Lua.RunScriptOrGlobal(FunctionName, Script);
			}
			else if (Type == Actuations.PressAndRelease)
			{
				Lua.RunScriptOrGlobal(FunctionName, Script, new object[] { state ? "press" : "release" });
			}
		}

		/*public class LogicButtonProxy : MonoLuaProxy<LogicButton> {
			public int id { get => proxy.ID; set => proxy.ID = value; }

			public void set(bool state) => proxy.Set(state);

		}*/

#if UNITY_EDITOR
		private bool IsSingleEvent       => Type == Actuations.Single;
		private bool IsPressReleaseEvent => Type == Actuations.PressAndRelease;
#endif




	}
}