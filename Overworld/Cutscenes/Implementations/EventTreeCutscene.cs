namespace Overworld.Cutscenes.Implementations
{
	// public class EventTreeCutscene : EventCutscene
	// {
	// 	[BoxGroup("Event Cutscene/Tree Cutscene")]
	// 	public List<EventTreeReference> Trees;
	//
	// 	public override EventNodeSocket OnMainAction(EventNodeSocket incomingSocket, EventTree tree)
	// 	{
	// 		EventNodeSocket nextOutput = incomingSocket;
	//
	// 		for (int i = 0; i < Trees.Count; i++)
	// 		{
	// 			if (Trees[i].FilePath.fileValid)
	// 			{
	// 				var mainNode = tree.AddActionNode();
	// 				mainNode.input.TryConnect(nextOutput);
	// 				var intAction = InterpreterAction.Create(Trees[i]);
	// 				mainNode.AddHandler(intAction);
	// 				nextOutput = mainNode.output;
	// 			}
	// 		}
	//
	// 		return nextOutput;
	// 	}
	// }
}