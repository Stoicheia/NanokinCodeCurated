using System;
using System.Collections.Generic;
using Anjin.MP;
using Anjin.Nanokin;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Anjin.Utils;
using Drawing;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld;
using Overworld.Controllers;
using Overworld.Tags;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[SelectionBase]
	[DefaultExecutionOrder(1)]
	public class Actor : ActorBase, IActivable
	{
		// CONFIGURATION
		//--------------------------------------------------------------------------

		/// <summary>
		/// Will register the actor ref by its gameobject's name.
		/// </summary>
		[UsedImplicitly]
		[TitleGroup("Actor", order:-100)]
		[LabelText("Register With Gameobject Name")]
		public bool RegisterByName = false;

		/// <summary>
		/// The actor reference to bind.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("_reference")]
		[HideIf("@RegisterByName")]
		[TitleGroup("Actor", order:-100)]
		public ActorRef Reference = ActorRef.NullRef;

		/// <summary>
		/// The character ID represented by the actor. TODO maybe this could use the CharacterAsset instead?
		/// </summary>
		[Optional]
		[FormerlySerializedAs("character")]
		[TitleGroup("Actor", order:-100)]
		public Character Character;

		/// <summary>
		/// Name of the character to display.
		/// </summary>
		[TitleGroup("Actor", order:-100)]
		public string CharacterName;

		/// <summary>
		/// The billboard used to draw this actor's sprite.
		/// Used for positioning speech bubbles.
		/// </summary>
		[Optional]
		[TitleGroup("Actor", order:-100)]
		public Billboard Billboard;

		/// <summary>
		/// Used for when we need to tilt the actor separate from the billboard,
		/// from the pivot instead of the middle of the sprite.
		/// </summary>
		[Optional]
		[TitleGroup("Actor", order:-100)]
		public Transform PivotTiltRoot;

		/// <summary>
		/// Unused/unimplemented feature, dates back to ~2018.
		/// Possibly unecessary complexity bloat
		/// </summary>
		[HideInInspector]
		[SerializeField, FormerlySerializedAs("_tags")]
		public List<ActorTag> Tags = new List<ActorTag>();


		// Runtime
		// ------------------------------------------------------------
		/// <summary>
		/// The actor's current speech bubble.
		/// </summary>
		// [BoxGroupExt("Actor")]
		[NonSerialized]
		public SpeechBubble currentBubble;

		/// <summary>
		/// Should not be used.
		/// Simply set the initial rotation of the transform to do this, or set the facing.
		/// </summary>
		[HideIf("HideFaceDegreeOffset")]
		[Range(0, 359)]
		[SerializeField]
		[Obsolete]
		protected float FaceDegreesOffset;

		/// <summary>
		/// The ActorRenderer for this actor.
		/// </summary>
		[NonSerialized]
		public new ActorRenderer renderer;

		/// <summary>
		/// The timescale for this actor.
		/// </summary>
		[NonSerialized, ShowInPlay, CanBeNull]
		public TimeScalable timeScale;

		/// <summary>
		/// Whether this actor has a character renderer.
		/// </summary>
		protected bool hasCharRenderer;

		[ShowInPlay] private Stack<ActorBrain> _brainStack  = new Stack<ActorBrain>();
		[ShowInPlay] private List<ActorBrain>  _localBrains = new List<ActorBrain>();
		[ShowInPlay] private ActorBrain        _prevBrain;
		[NonSerialized] public Closure on_interact;
		[NonSerialized] public Closure on_path_begin;
		[NonSerialized] public Closure on_path_end;
		[NonSerialized] public Closure on_path_reach_node;
		[NonSerialized] public Closure on_path_update;

		[NonSerialized, ShowInPlay]
		public Action<Vector3> OnTeleport;

		public string ReferenceID
		{
			get
			{
				if (RegisterByName)
					return gameObject.name;
				else if (!Reference.IsNullID)
					return Reference.Name;
				return gameObject.name;
			}
		}

		public virtual void ClearVelocity(bool x = true, bool y = true, bool z = true) { }

		public virtual void AddForce(Vector3 force, bool setY = false, bool setXZ = false) { }

		[HideInInspector] public UnityEvent OnLand;

		private static readonly List<ActorBrain> _scratchBrains = new List<ActorBrain>();

		public static Action<Actor> OnSpawn;
		public static Action<Actor> OnDespawn;

		protected override void Awake()
		{
			base.Awake();

			timeScale       = GetComponent<TimeScalable>();
			renderer        = GetComponentInChildren<ActorRenderer>();
			hasCharRenderer = renderer != null;

			// Find our local base brains, only activating one when we
			GetComponents(_scratchBrains);

			foreach (ActorBrain b in _scratchBrains)
				AddLocalBrain(b, false);

			UpdateActiveBrain();

			facing        = Quaternion.Euler(0, transform.localEulerAngles.y, 0) * Vector3.forward;
			initialFacing = facing;

			ActorRegistry.Register(this);
			Enabler.Register(gameObject, true);
		}


		protected override void Start()
		{
			actorActive = true;
			ActorRegistry.Register(this);
		}

		protected virtual void OnEnable()
		{
			OnSpawn?.Invoke(this);
		}

		protected virtual void OnDisable()
		{
			OnDespawn?.Invoke(this);
		}

		protected virtual void OnDestroy()
		{
			actorActive = false;
			ActorRegistry.Deregister(this);
		}

		// NOTE: We're doing this to try to prevent GameTags deactivating an actor before it gets deactivated by a script when the level loads.
		public void OnActivate()   { ActorRegistry.Register(this); }
		public void OnDeactivate() { ActorRegistry.Register(this); }

		protected virtual void Update()
		{
			position = transform.position;

			if (_localBrains.Count != 0)
			{
				// We only set activeBrain when adding or removing, it's in sync with _brains
				activeBrain.OnTick(Time.deltaTime);
			}

			if (hasCharRenderer)
			{
				UpdateRenderState(ref renderer.state);
			}

			UpdateFX();
		}

		protected virtual void UpdateFX() { }

		// Brain
		// ----------------------------------------

		public void PushOutsideBrain(ActorBrain brain, bool updateActive = true)
		{
			if (brain == null || _brainStack.Count > 0 && _brainStack.Peek() == brain) {
				return;
			}

			_brainStack.Push(brain);

			if(updateActive)
				UpdateActiveBrain();
		}

		public void PopOutsideBrain(ActorBrain brain = null, bool updateActive = true)
		{
			if (_brainStack.Count == 0 || brain != null && _brainStack.Peek() != brain) {
				return;
			}

			_brainStack.Pop();

			if(updateActive)
				UpdateActiveBrain();
		}

		public void AddLocalBrain(ActorBrain brain, bool updateActive = true)
		{
			// Using binary search in this way will keep the list sorted by priority, always.
			int idx = _localBrains.BinarySearch(brain);
			_localBrains.Insert(idx >= 0 ? idx : ~idx, brain);

			if(updateActive) {
				UpdateActiveBrain();
			}
		}

		public void RemoveLocalBrain(ActorBrain brain, bool updateActive = true)
		{
			if (brain == null)
			{
				DebugLogger.LogError($"Trying to remove null brain from actor '{gameObject.name}'.", gameObject, LogContext.Combat, LogPriority.High);
				return;
			}

			int idx = _localBrains.BinarySearch(brain); // we can do this since the list is kept sorted
			if (idx < 0)
				// We don't have this brain
				return;

			_localBrains.RemoveAt(idx);
			if (updateActive) {
				UpdateActiveBrain();
			}
		}

		void UpdateActiveBrain()
		{
			if (_brainStack.Count == 0) {
				activeBrain = _localBrains.Count > 0 ? _localBrains[_localBrains.Count - 1] : null;
			} else {
				activeBrain = _brainStack.Peek();
			}

			hasBrain    = activeBrain != null;

			if(_prevBrain != activeBrain) {
				//Debug.Log($"{this}: Brain swap from {_prevBrain} to {activeBrain}");
				if (_prevBrain != null) {
					_prevBrain.OnEndControl();
					_prevBrain.SetActor(null);
				}

				if (hasBrain) {
					activeBrain.SetActor(this);
					activeBrain.OnBeginControl();
				}
			}

			_prevBrain = activeBrain;
		}

		protected void PollCharacterBrainInputs(ref CharacterInputs inputs)
		{
			if (activeBrain is ICharacterActorBrain brain)
			{
				brain.PollInputs(this, ref inputs);

				inputs.moveMagnitude = inputs.move.magnitude;
				inputs.hasMove       = inputs.moveMagnitude > Mathf.Epsilon;

				targetFacing = inputs.look ?? facing;
			}
		}

		protected void ResetCharacterBrainInputs(ref CharacterInputs inputs)
		{
			if (activeBrain is ICharacterActorBrain brain)
			{
				brain.ResetInputs(this, ref inputs);
			}
		}

		// Facing
		// ----------------------------------------
		public virtual WorldPoint GetHeadPoint()
		{
			if (Billboard)
				return new WorldPoint(Billboard.gameObject, Vector3.up * (height + 0.2f), true);

			return new WorldPoint(gameObject, Vector3.up * height);
		}


		/// <summary>
		/// Get the rendering information used to draw the animator.
		/// </summary>
		/// <returns></returns>
		public virtual void UpdateRenderState(ref RenderState state) { }

		//	GAME SPECIFIC
		//--------------------------------------------------------------------------
		public virtual void Teleport(Vector3             pos)
		{
			transform.position = pos;
			OnTeleport?.Invoke(pos);
		}

		public         void Teleport(Vector2             pos)                 => Teleport(new Vector3(pos.x, 0, pos.y));
		public         void Teleport(RegionObjectSpatial robj)                => Teleport(robj.Transform.Position);
		public         void Teleport(Transform           trans)               => Teleport(trans.position);
		public         void Teleport(GameObject          obj)                 => Teleport(obj.transform.position);
		public         void Teleport(float               x, float y, float z) => Teleport(new Vector3(x, y, z));

		public void Teleport(SpawnPoint point, int index = 0, bool reorient = true)
		{
			Teleport(point.GetSpawnPointPosition(index));

			if (reorient)
			{
				Vector3 dir = point.GetSpawnPointFacing(index);
				Reorient(dir);

				if (this is PlayerActor)
					ActorController.playerCamera.ReorientInstant(dir);
			}
		}

		public void Teleport(DynValue arg) => Teleport(arg, null, null);

		[MoonSharpHidden]
		public void Teleport(DynValue arg1, DynValue arg2, DynValue arg3)
		{
			if (arg1 == null || arg1.IsNil())
				return;

			if (arg1.Type == DataType.UserData)
			{
				var data = arg1.UserData;
				if (data.TryGet(out Vector3 v3))
					Teleport(v3);
				else if (data.TryGet(out Vector2 v2))
					Teleport(new Vector3(v2.x, 0, v2.y));
				else if (data.TryGet(out RegionObjectSpatial robj))
					Teleport(robj.Transform.Position);
				else if (data.TryGet(out Transform trans))
					Teleport(trans.position);
				else if (data.TryGet(out GameObject obj))
					Teleport(obj.transform.position);
			}
			else
			{
				Teleport(new Vector3(
					(float)arg1.CastToNumber().GetValueOrDefault(0),
					(float)arg2.CastToNumber().GetValueOrDefault(0),
					(float)arg3.CastToNumber().GetValueOrDefault(0)
				));
			}
		}

		public virtual void Reorient(Quaternion rot)
		{
			transform.rotation = rot;
			facing             = rot * Vector3.forward;
		}

		public void Reorient(Vector3 dir)
		{
			dir = dir.Horizontal();

			facing = dir;
			Reorient(Quaternion.LookRotation(dir, Vector3.up));
		}

		public SpeechBubble Say(string message, float seconds)
		{
			var bubble = SetupBubble();

			bubble.SetLine(new GameText(message));
			bubble.Show(seconds);

			return bubble;
		}

		public SpeechBubble SayLines(string[] lines, float seconds)
		{
			var bubble = SetupBubble();

			bubble.SetLines(Array.ConvertAll(lines, s => new GameText(s)));
			bubble.Show(seconds);

			return bubble;
		}

		public SpeechBubble SayManual(string message)
		{
			var bubble = SetupBubble();

			bubble.SetLine(new GameText(message));
			bubble.ShowManual();

			return bubble;
		}

		public SpeechBubble SayManualLines(string[] lines)
		{
			var bubble = SetupBubble();

			bubble.SetLines(Array.ConvertAll(lines, s => new GameText(s)));
			bubble.ShowManual();

			return bubble;
		}

		public virtual SpeechBubble SetupBubble()
		{
			var bubble = GameHUD.SpawnSpeechBubble();

			bubble.hudElement.SetPositionModeWorldPoint(GetHeadPoint(), Vector3.zero);
			bubble.UE_OnDone.AddListener(() => GameHUD.Live.bubblePool.ReturnSafe(bubble));
			currentBubble = bubble;


			return bubble;
		}

		public virtual SpritePopup Reaction(string reaction, float time)
		{
			Sprite sprite = null;

			switch (reaction)
			{
				case "?":
					sprite = GameAssets.Live.Reaction_Question;
					break;
				case "!":
					sprite = GameAssets.Live.Reaction_Exclamation;
					break;
			}

			var popup = GameHUD.SpawnSpritePopup();

			popup.HudElement.SetPositionModeWorldPoint(GetHeadPoint(), Vector3.zero);
			popup.UE_OnDone.AddListener(() => GameHUD.Live.spritePopupPool.ReturnSafe(popup));
			popup.Show(sprite, time);

			return popup;
		}

		public void message(string msg, params DynValue[] inputs)
		{
			OnMessage(msg, inputs);
			activeBrain.Lua_Message(msg, inputs);
		}

		public virtual void OnMessage(string msg, params DynValue[] inputs) { }

		public override void DrawGizmos()
		{
			base.DrawGizmos();

			if (GizmoContext.InSelection(this))
			{
				Gizmos.color = Color.red;
				if (Billboard)
				{
					Draw.WireBox(transform.position + Vector3.up * height, new Vector3(0.5f, 0f, 0.5f), Color.red);
				}
				else
				{
					Draw.WireBox(transform.position + Vector3.up * height, new Vector3(0.5f, 0f, 0.5f), Color.red);
				}

				Color facingColor = ColorUtil.MakeColorHSVA(0.75f, 0.75f, 0.9f, 1f); // A VERY nice purple, perfect selection of pigments

				using (Draw.WithLineWidth(2f))
				using (Draw.WithColor(facingColor))
				{
					Vector3 p = transform.position + Vector3.up * height / 2f;
					if (Application.IsPlaying(gameObject))
						Draw.Arrow(p, p + facing);
					else
						Draw.Arrow(p, p + Quaternion.Euler(0, transform.localEulerAngles.y + FaceDegreesOffset, 0) * Vector3.forward);
				}
			}
		}

		//	LUA
		//--------------------------------------------------------------------------

		public void LUA_OnInteract()       => on_interact?.Call(this);
		public void LUA_OnPathReachBegin() => on_path_begin?.Call(this);
		public void LUA_OnPathReachEnd()   => on_path_end?.Call(this);

		public void LUA_OnPathReachNode(PathUpdateResult result, PathingState state) => on_path_reach_node?.Call(this, result, state);
		public void LUA_OnPathUpdate(PathUpdateResult    result, PathingState state) => on_path_update?.Call(this, result, state);

		[UsedImplicitly]
		protected bool HideFaceDegreeOffset => true;
	}
}