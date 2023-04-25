/// <summary>
/// A thing that displays a string on screen in some way.
/// </summary>
public interface ITextDisplayer
{
	string Text { get; set; }
	bool DisplayProcessDone { get; set; }

	//UnityEvent UE_OnDisplayNew { get; set; }

	void DisplayNew(GameText text, float displaySpeed = -1);
	void Pause();
	void Unpause();
	void FinishDisplayProcess();
	void StopDisplaying();
}