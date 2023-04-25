using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Drawing;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Overworld.Interactables;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Util;
using Util.Components;
using Util.Odin.Attributes;
using Object = UnityEngine.Object;

[AddComponentMenu("Anjin: Game Building/Interactable")]
public class Interactable : AnjinBehaviour, ILuaAddon
{
	[LuaEnum("interactable_type")]
	public enum Type { Action, Talk }

	[HorizontalGroup]
	[HideLabel]
	[SuffixLabel("Priority", true)]
	public int Priority = 0;

	[HorizontalGroup]
	[HideLabel]
	[EnumToggleButtons]
	public Type ShowType = Type.Talk;

	[Tooltip("Cutscene to play when interacting.")]
	[HideLabel]
	public Cutscene Cutscene;

	// TODO This should be replaced with C# event which is unserializable, to promote use of Lua and streamline the workflow
	[HideInInspector]
	public UnityEvent OnInteract;

	[DebugVars]
	[PropertyOrder(10)]
	[NonSerialized]
	public int locks;

	[NonSerialized] public Closure on_interact_with;
	[NonSerialized] public bool    disable;

	[UsedImplicitly]
	public bool CanInteract => !disable && locks == 0;

	private Actor _actor;
	private bool  _actorChecked;

	private IInteractable[] _recievers;

	public bool Disabled => disable || locks > 0;

	private void Awake()
	{
		OnInteract = OnInteract ?? new UnityEvent();
		if (Cutscene == null)
			Cutscene = GetComponent<Cutscene>();

		_recievers = GetComponents<IInteractable>();
	}

	private void Start()
	{
		_actor = GetComponent<Actor>();

		if (!gameObject.HasComponent<Collider>())
		{
			SphereCollider collider = gameObject.AddComponent<SphereCollider>();
			collider.radius    = 0.1f;
			collider.isTrigger = true;
			if (_actor != null)
			{
				collider.center = Vector3.up * _actor.height * 0.5f;
			}
		}
	}

	public bool CanInteractWith(Actor actor)
	{
		for (var i = 0; i < _recievers.Length; i++) {
			if (_recievers[i].IsBlockingInteraction(actor))
				return false;
		}

		return CanInteract;
	}

	[DisableInEditorMode]
	[GUIColor(0, 0.85f, 0, 1)]
	[Button(ButtonSizes.Large)]
	[PropertyOrder(-1)]
	[DisableIf("CanInteract")]
	// [DRAW_IN_HIER]
	public void Interact(Actor actor = null)
	{
		if (locks > 0) return;



		OnInteract.Invoke();
		on_interact_with?.Call();

		for (var i = 0; i < _recievers.Length; i++)
		{
			IInteractable interactable = _recievers[i];
			interactable.OnInteract(actor);
		}

		// Allows actors to be interactable simply by placing Interactable on them (implicit LuaOnInteract)
		// ----------------------------------------
		if (_actor != null)
		{
			List<Coplayer> coplayers = Lua.RunGlobal(gameObject.name, manual_start: true, optional: true);
			foreach (Coplayer coplayer in coplayers)
			{
				locks++;
				coplayer.sourceInteractable = this;
				coplayer.sourceObject       = gameObject;
				coplayer.Play().Forget();
			}

			if (Cutscene != null)
			{
				Cutscene.Play();
			}
		}
	}

	public string NameInTable => "interactable";


	protected override void OnRegisterDrawer() => DrawingManagerProxy.Register(this);
	private            void OnDestroy()        => DrawingManagerProxy.Deregsiter(this);

	public override void DrawGizmos()
	{
		if (_actor == null && !_actorChecked)
		{
			_actorChecked = true;
			_actor        = GetComponent<Actor>();
		}
		//
		// using (Draw.WithColor(Color.black))
		// using (Draw.InLocalSpace(transform))
		// using (Draw.WithLineWidth(8f))
		// {
		// 	Vector3 center = Vector3.zero;
		// 	if (_actor != null)
		// 	{
		// 		center = Vector3.up * _actor.HeadHeight / 2f;
		// 	}
		//
		// 	Draw2.Asterisk(center, 0.2f);
		// }


		using (Draw.WithMatrix(Matrix4x4.Translate(transform.position)))
		{
			// using (Draw.WithMatrix(Matrix4x4.Scale(Vector3.one * 2f)))
			// using (Draw.WithLineWidth(20f))
			// using (Draw.WithColor(Color.black))
			// 	Draw2.Asterisk(GetCenter(), 0.235f);

			using (Draw.WithLineWidth(10f))
			using (Draw.WithColor(new Color(255 / 255f, 235 / 255f, 0, 1)))
				Draw2.Asterisk(GetCenter(), 0.235f);
		}
	}

	private Vector3 GetCenter()
	{
		Vector3 center = Vector3.zero;
		if (_actor != null)
		{
			center = Vector3.up * _actor.height / 2f;
		}

		return center;
	}

	[LuaGlobalFunc("make_interactable")]
	public static Interactable MakeInteractable(Object obj, string name = "Interact")
	{
		Interactable interactable;

		if (obj is Component comp) interactable     = comp.GetOrAddComponent<Interactable>();
		else if (obj is GameObject go) interactable = go.GetOrAddComponent<Interactable>();
		else return null;

		interactable.name = name;

		if (interactable.TryGetComponent<LuaComponentBase>(out var lua))
			lua.RegisterAddon(interactable);

		return interactable;
	}

	[LuaGlobalFunc]
	public static void interact_enable(DynValue obj) => interact_set_enabled(obj, true);

	[LuaGlobalFunc]
	public static void interact_disable(DynValue obj) => interact_set_enabled(obj, false);


	[LuaGlobalFunc]
	public static void interact_set_enabled(DynValue obj, bool val)
	{
		Interactable interactable = null;

		if(obj.UserData.TryGet(out Actor actor))
			interactable = actor.GetComponent<Interactable>();
		else if(obj.UserData.TryGet(out DirectedActor dactor))
			interactable = dactor.actor.GetComponent<Interactable>();

		if (interactable == null) return;

		interactable.disable = !val;
	}

#if UNITY_EDITOR
	public Color ColorLocks => locks > 0 ? Color.grey : Color.white;
#endif


	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class InteractableProxy : MonoLuaProxy<Interactable>
	{
		public string            name             { get => proxy.name;             set => proxy.name = value; }
		public Closure           on_interact_with { get => proxy.on_interact_with; set => proxy.on_interact_with = value; }
		public Interactable.Type show_type        { get => proxy.ShowType; }
		public bool              disable          { get => proxy.disable; set => proxy.disable = value; }

		public void interact_with() => proxy.Interact();
	}
}