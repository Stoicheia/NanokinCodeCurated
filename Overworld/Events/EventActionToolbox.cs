using System;
//using UnityEditor;

/// <summary>
/// Container for event action tool classes, for use with OnDrawActionControlGUI.
/// Contains EventConnectorsRuntime,
/// </summary>
/*public class EventActionToolbox
{
	public EventActionToolbox(EventNodeWindow _window,RefPickerBridge _picker)
	{
		window = _window;
		ReferencePickerInternal = _picker;
	}

	//Runtime Variables.
	public EventNodeWindow window; 

	//For adding connectors to the GUI
	private EventConnectorsRuntime _connectors = null;
	public EventConnectorsRuntime Connectors
	{
		get
		{
			if(_connectors == null)
			{
				_connectors = new EventConnectorsRuntime();
			} return _connectors;
		}
		private set { }
	}

	public RefPickerGUI _referencePickerGUI;
	public RefPickerGUI ReferencePickerGUI
	{
		get
		{
			if (_referencePickerGUI == null)
			{
				_referencePickerGUI = new RefPickerGUI();
				_referencePickerGUI.bridge = ReferencePickerInternal;
			}
			return _referencePickerGUI;
		}
		private set { }
	}

	public RefPickerBridge ReferencePickerInternal = null;

	public void Reset()
	{
		_connectors = null;
	}
}


public class EventConnectorsRuntime
{
	public class EventInputRuntime
	{
		public EventNodeInput input;
		public Rect controlAttachedTo;
		public Rect windowPos;
		public bool rectRelative;
		public EventInputRuntime(EventNodeInput input, Rect controlAttachedTo, bool relative) { this.input = input; this.controlAttachedTo = controlAttachedTo; this.rectRelative = relative; }
	}

	public class EventOutputRuntime
	{
		public EventNodeOutput output;
		public Rect controlAttachedTo;
		public bool rectRelative;
		public Rect windowPos;
		public EventOutputRuntime(EventNodeOutput output, Rect controlAttachedTo, bool relative) { this.output = output; this.controlAttachedTo = controlAttachedTo; this.rectRelative = relative; }
	}

	public List<EventInputRuntime> inputs;
	public List<EventOutputRuntime> outputs;

	public bool Empty
	{
		get
		{
			return (inputs.Count == 0 ? true : false && outputs.Count == 0 ? true : false);
		}
	}

	public EventConnectorsRuntime()
	{
		inputs = new List<EventInputRuntime>();
		outputs = new List<EventOutputRuntime>();
	}

	/// <summary>
	/// Add an input to the runtime connectors
	/// MUST BE DIRECTLY CALLED AFTER A GUILAYOUT CONTROL DRAW CALL
	/// </summary>
	/// <param name="connectorName">The exact name you used to establish the input</param>
	/// <param name="action">The calling action (just put this)</param>
	public void AddInput(string connectorName, EventAction action)
	{
		//Only works on repaint, because of the code to get the position of the last control
		if (Event.current.type == EventType.Repaint)
		{
			//Find the node
			if (action.inputs.Count > 0)
			{
				EventNodeInput n = action.inputs.Find(x => x.connectorName == connectorName);
				if (n != null)
				{
					EventInputRuntime i = new EventInputRuntime(n, GUILayoutUtility.GetLastRect(), false);
					inputs.Add(i);
				}
			}
		}
	}

	/// <summary>
	/// Add an output to the runtime connectors
	/// MUST BE DIRECTLY CALLED AFTER A GUILAYOUT CONTROL DRAW CALL
	/// </summary>
	/// <param name="connectorName">The exact name you used to establish the input</param>
	/// <param name="action">The calling action (just put this)</param>
	public void AddOutput(string connectorName, EventAction action)
	{
		//Only works on repaint, because of the code to get the position of the last control
		if (Event.current.type == EventType.Repaint)
		{
			//Find the node
			if (action.outputs.Count > 0)
			{
				EventNodeOutput o = action.outputs.Find(x => x.connectorName == connectorName);
				if (o != null)
				{
					EventOutputRuntime i = new EventOutputRuntime(o, GUILayoutUtility.GetLastRect(), false);
					outputs.Add(i);
				}
			}
		}
	}
}*/



//public class RefPickerGUI
//{
//	public RefPickerBridge bridge;
//	enum ActionControlState{closed,open,selected}

//	public bool isInspector = false;
//	public bool isProperty = false;
//	public bool isCompact = false;

//	public Rect PropertyRect;

//	class ControlPair
//	{
//		public ActionControlState state;
//		public string id;
//		public ActorReferencePointer actor;
//	}

//	private Dictionary<string,ControlPair> pairs = new Dictionary<string, ControlPair>();

//	public string currentID = null;
//	private int actionCounter = 0;

//	public void ReadyForAction(string id)
//	{
//		currentID = id;
//		actionCounter = 0;
//	}

//	public void OnPickerChange(ActorReference reference, string controlID)
//	{
//		//actor = ActorReference.ReferenceToPointer(reference);
//		//Debug.Log("OnChanged Actor Reference: "+reference+":"+controlID);
//		pairs[controlID].state = ActionControlState.selected;
//		pairs[controlID].actor = ActorReference.ReferenceToPointer(reference);
//	}

//	public void OnPickerClose(ActorReference reference, string controlID)
//	{
//		pairs[controlID].state = ActionControlState.closed;
//	}

//#if UNITY_EDITOR
//    //Show a picker control
//    public static ActorReferencePointer Control(ActorReferencePointer actor,ref bool fieldsChanged)
//	{
//		return Control(actor, ref fieldsChanged, ReferenceInfoDisplay);
//	}

//	public static ActorReferencePointer Control(ActorReferencePointer actor, ref bool fieldsChanged, System.Func<ActorReferencePointer,RefPickerGUI,bool> drawFunction)
//	{
//		ActorReferencePointer returnActor = actor;

//		if (returnActor != null && returnActor.GetReference() == null)
//			returnActor = null;

//		string id = currentID + "Control" + actionCounter++.ToString();

//		if (!pairs.ContainsKey(id))
//		{
//			pairs[id] = new ControlPair() { actor = actor, id = id, state = ActionControlState.closed };
//		}

//		switch (pairs[id].state)
//		{
//			case ActionControlState.closed:
//			{
//				if (drawFunction(actor, this))
//				{
//					bridge.OpenPicker(actor, OnPickerChange, OnPickerClose, id);
//					pairs[id].state = ActionControlState.open;
//				}
//			}
//			break;

//			case ActionControlState.open:
//			{
//				GUI.enabled = false;
//				if (drawFunction(actor, this))
//				{

//				}
//				GUI.enabled = true;
//			}
//			break;

//			case ActionControlState.selected:
//			{
//				if (Event.current.type == EventType.Repaint)
//					pairs[id].state = ActionControlState.open;
//				returnActor = pairs[id].actor;
//			}
//			break;
//		}

//		//ActorReference.DrawActorReferenceDisplay(actor, false, false,ref fieldsChanged);
//		//ReferenceInfoDisplay(returnActor);

//		return returnActor;
//	}


////#if UNITY_EDITOR
//    public static bool ReferenceInfoDisplay(ActorReferencePointer pointer,RefPickerGUI self)
//	{
//		const bool IS_COMPACT = true;

//		string name = "No Actor Selected";
//		string dbName = "Test Database";
//		string pageName = "Test Page";

//		if (pointer != null && pointer.GetReference() != null)
//		{
//			pageName = pointer.GetReference().holdingPage.name;
//			dbName = pointer.GetReference().holdingDatabase.name;
//			name = pointer.GetReference().name;
//		}

//		bool buttonPressed = false;

//		GUISkin s = GUI.skin;
//		GUI.skin = null;

//		GUIStyle boxStyle = new GUIStyle((GUIStyle)"ShurikenEffectBg");
//		boxStyle.padding.right = 1;
//		boxStyle.padding.top = 0;
//		boxStyle.padding.bottom = -20;
//		boxStyle.margin.left = -3;
//		boxStyle.margin.right = -3;

//		GUIStyle buttonStyle = new GUIStyle((GUIStyle)"LargeButtonRight");
//		buttonStyle.margin.right = -3;
//		buttonStyle.margin.top = -4;
//		buttonStyle.margin.bottom = 0;
//		buttonStyle.clipping = TextClipping.Overflow;

//		GUIStyle headerStyle = new GUIStyle(UnityEditor.EditorStyles.whiteLargeLabel);
//		headerStyle.fontStyle = FontStyle.Bold;
//		headerStyle.margin.bottom = -6;
//		headerStyle.fontSize = 14;
//		headerStyle.clipping = TextClipping.Overflow;
//		headerStyle.normal.textColor = Color.HSVToRGB(0.15f,0.7f,0.9f);
		

//		GUIStyle footNoteStyle = new GUIStyle(UnityEditor.EditorStyles.whiteMiniLabel);
//		footNoteStyle.fontStyle = FontStyle.Bold;
//		footNoteStyle.margin.top = -6;
//		footNoteStyle.margin.bottom = -16;
//		footNoteStyle.contentOffset = new Vector2(1,0);
//		footNoteStyle.normal.textColor = Color.black;

//		GUIStyle footNoteRightStyle = new GUIStyle(footNoteStyle);
//		footNoteRightStyle.alignment = TextAnchor.UpperRight;

//		//Becase you can't use GUILayout inside of 
//		//god damn property drawers for some reason.
//		if (!self.isProperty)
//		{
//			GUILayout.BeginHorizontal(boxStyle, IS_COMPACT ? GUILayout.Height(18) : GUILayout.Height(43));

//			if (self.isInspector)
//				GUILayout.BeginVertical(GUILayout.MinWidth(144));
//			else
//				GUILayout.BeginVertical(GUILayout.MinWidth(144), GUILayout.MaxWidth(100000), GUILayout.ExpandHeight(false));

//			GUILayout.BeginHorizontal(GUILayout.Height(18));

//			GUILayout.FlexibleSpace();
//			GUILayout.BeginVertical(GUILayout.ExpandHeight(false));

//			GUILayout.FlexibleSpace();
//			GUILayout.Label(name, headerStyle);
//			GUILayout.FlexibleSpace();

//			if (!IS_COMPACT)
//			{
//				GUILayout.BeginHorizontal();
//				GUILayout.Label("Page:", footNoteStyle);
//				GUILayout.FlexibleSpace();
//				GUILayout.Label(pageName, footNoteStyle);
//				GUILayout.EndHorizontal();

//				GUILayout.BeginHorizontal();
//				GUILayout.Label("DB:", footNoteStyle);
//				GUILayout.FlexibleSpace();
//				GUILayout.Label(dbName, footNoteStyle);
//				GUILayout.EndHorizontal();
//			}

//			GUILayout.EndVertical();
//			GUILayout.FlexibleSpace();

//			GUILayout.EndHorizontal();

//			GUILayout.EndVertical();

//			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));
//			if (!self.isInspector)
//			{
//				if (GUILayout.Button(new GUIContent(""), buttonStyle, GUILayout.Width(32), GUILayout.Height(21),
//					GUILayout.ExpandHeight(false)))
//				{
//					buttonPressed = true;
//				}
//			}
//			else
//			{
//				if (GUILayout.Button(new GUIContent("||\n||"), buttonStyle, GUILayout.Width(22), GUILayout.Height(18),
//					GUILayout.ExpandHeight(false)))
//				{
//					buttonPressed = true;
//				}
//			}
//			GUILayout.EndHorizontal();
//			GUILayout.EndHorizontal();
//		}
//		else
//		{
//			Rect box = new Rect(self.PropertyRect.position, self.PropertyRect.size - new Vector2(0, 4));
//			int bwidth = 22;
//			Rect contents1 = new Rect(5, 3, self.PropertyRect.width - bwidth-9, 20);
//			Rect contents2 = new Rect(5, 16, self.PropertyRect.width - bwidth-9, 20);
//			Rect contents3 = new Rect(5, 26, self.PropertyRect.width - bwidth-9, 20);
//			//Assumes Property Rect is set
//			if (self.PropertyRect != null)
//			{
//				GUI.BeginGroup(box,boxStyle);
//				GUI.Label(contents1, name,headerStyle);

//				GUI.Label(contents2, "Page:", footNoteStyle);
//				GUI.Label(contents2, pageName, footNoteRightStyle);

//				GUI.Label(contents3, "DB:", footNoteStyle);
//				GUI.Label(contents3, dbName, footNoteRightStyle);

//				if(GUI.Button(new Rect(box.width - bwidth+1, 1, bwidth,box.height), new GUIContent("||\n||"), buttonStyle))
//				{
//					buttonPressed = true;
//				}
//				GUI.EndGroup();
//			}
//		}
		

//		GUI.skin = s;

//		return buttonPressed;
//	}
//#endif
//}

/// <summary>
/// Acts as a bridge to the picker window. Use this if you actually want to open
/// </summary>
public class RefPickerBridge
{
	private static int controlGroupCounter;

	Action<ActorReference, Action<ActorReference, string>, Action<ActorReference, string>, string> _openPickerAction;

	public RefPickerBridge(Action<ActorReference, Action<ActorReference, string>, Action<ActorReference, string>, string> _openPickerAction)
	{
		this._openPickerAction = _openPickerAction;
	}
#if UNITY_EDITOR
    public void OpenPicker(ActorReferencePointer selectedActor, Action<ActorReference, string> onPickerChanged, string id)
	{
		ActorReference aref = null;
		if (selectedActor != null)
			aref = selectedActor.GetReference();
		OpenPicker(aref, onPickerChanged, (_, __) => { }, id);
	}

	public void OpenPicker(ActorReferencePointer selectedActor, Action<ActorReference, string> onPickerChanged, Action<ActorReference, string> onPickerClosed, string id)
	{
		OpenPicker((selectedActor!=null) ? selectedActor.GetReference() : null, onPickerChanged, onPickerClosed, id);
	}
#endif

    public void OpenPicker(ActorReference selectedActor, Action<ActorReference, string> onPickerChanged, Action<ActorReference, string> onPickerClosed, string id)
	{
		if (_openPickerAction != null)
			_openPickerAction(selectedActor, onPickerChanged, onPickerClosed, id);
	}
}

/*public class TreeEditorBridge
{
	public RefPickerBridge pickerBridge = null;

	public Handler<EventActionNode> copyNode = null;

	public void CopyNode(EventActionNode node)
	{
		copyNode(node);
	}
}*/