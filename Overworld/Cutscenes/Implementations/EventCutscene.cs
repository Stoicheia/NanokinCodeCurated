namespace Overworld.Cutscenes.Implementations
{
	// public class EventCutscene : WorldCutscene
	// {
	// 	[FoldoutGroup("Event Cutscene")]
	// 	public EventTreeInterpreterComponent Interpreter;
	//
	// 	[HideInEditorMode]
	// 	[FoldoutGroup("Event Cutscene")]
	// 	public EventTree RuntimeEncasingTree;
	//
	// 	[FoldoutGroup("Event Cutscene/Camera")] public bool UseCameraConfig;
	// 	[FoldoutGroup("Event Cutscene/Camera")] public bool WaitForBlend = true;
	// 	[PropertyRange(0, 1)]
	// 	[FoldoutGroup("Event Cutscene/Camera")] public float BlendWaitPercentage = 1;
	//
	// 	[FoldoutGroup("Event Cutscene/Camera")]
	// 	//public CameraConfig_OLD CamConfigOld;
	//
	// 	public virtual void Start()
	// 	{
	// 		var go = new GameObject("_Interpreter");
	// 		go.transform.SetParent(transform);
	// 		Interpreter                        = go.AddComponent<EventTreeInterpreterComponent>();
	// 		Interpreter.instantiateImmediately = true;
	// 		Interpreter.startImmediately       = false;
	// 		Interpreter.manualUpdate           = true;
	// 	}
	//
	// 	public virtual void Update()
	// 	{
	// 		if (Running)
	// 		{
	// 			Interpreter.UpdateInterpreter();
	// 		}
	// 	}
	//
	// 	public override bool Begin()
	// 	{
	// 		if (!base.Begin()) return false;
	//
	// 		var tree = BuildOuterTree();
	//
	// 		Interpreter.Interpreter.Start(tree);
	// 		Interpreter.Interpreter.OnTreeEnd.AddListener(OnTreeEnd);
	//
	// 		return true;
	// 	}
	//
	// 	public void OnTreeEnd()
	// 	{
	// 		Interpreter.Interpreter.OnTreeEnd.RemoveListener(OnTreeEnd);
	// 		End();
	// 	}
	//
	// 	public virtual EventTree BuildOuterTree()
	// 	{
	// 		RuntimeEncasingTree = new EventTree();
	// 		var entryNode = RuntimeEncasingTree.AddEntryPointNode(RuntimeEncasingTree.mainEntryPoint);
	//
	// 		var beforeSocket = OnBeforeMainAction(entryNode.socket, RuntimeEncasingTree);
	// 		var mainOutput   = OnMainAction(beforeSocket, RuntimeEncasingTree);
	// 		OnAfterMainAction(mainOutput,RuntimeEncasingTree);
	//
	// 		return RuntimeEncasingTree;
	// 	}
	//
	// 	/// <summary>
	// 	/// Override this to add actions before the interpreter action for the cutscene event tree.
	// 	/// </summary>
	// 	/// <param name="entrySocket">The entry socket of the outer tree.</param>
	// 	/// <returns>The next socket in the chain, which will be connected to the interpreter
	// 	/// action for the cutscene tree.</returns>
	// 	public virtual EventNodeSocket OnBeforeMainAction(EventNodeSocket entrySocket, EventTree tree)
	// 	{
	// 		EventNodeSocket nextOutput = entrySocket;
	//
	// 		if (UseCameraConfig)
	// 		{
	// 			var a            = tree.AddActionNode();
	// 			var configAction = EventAction.Create<OverrideCameraConfigAction>();
	// 			//configAction.ConfigOld = CamConfigOld;
	// 			a.AddHandler(configAction);
	//
	// 			if (WaitForBlend)
	// 			{
	// 				var waitAction = EventAction.Create<WaitForCinemachineBlendAction>();
	// 				waitAction.WaitUntilPercentage = BlendWaitPercentage;
	// 				a.AddHandler(waitAction);
	// 			}
	//
	// 			a.input.TryConnect(nextOutput);
	// 			nextOutput = a.output;
	// 		}
	//
	// 		return nextOutput;
	// 	}
	//
	// 	/// <summary>
	// 	/// Override this to add actions after the interpreter action for the cutscene event tree.
	// 	/// </summary>
	// 	/// <param name="entrySocket">The entry socket of the outer tree.</param>
	// 	public virtual void OnAfterMainAction(EventNodeSocket incomingSocket, EventTree tree)
	// 	{
	// 		EventNodeSocket nextOutput = incomingSocket;
	//
	// 		if (UseCameraConfig)
	// 		{
	// 			var a = tree.AddActionNode();
	// 			a.AddHandler(CameraConfigReturnAction.Create(CameraConfigReturnAction.Mode.Previous));
	// 			a.input.TryConnect(nextOutput);
	// 			nextOutput = a.output;
	// 		}
	// 	}
	//
	// 	public virtual EventNodeSocket OnMainAction(EventNodeSocket incomingSocket, EventTree tree)
	// 	{
	// 		return incomingSocket;
	// 	}
	// }
}