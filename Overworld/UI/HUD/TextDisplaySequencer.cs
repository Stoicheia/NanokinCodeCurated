using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sequences an ITextDisplayer through a list of text lines based on outside input.
/// </summary>
public class TextDisplaySequencer : SerializedMonoBehaviour
{
	public enum State {
		Off,
		Sequencing,
		Paused,
	}

	public ITextDisplayer Displayer;
	public List<GameText> Lines;
	public UnityEvent     OnDisplayNext;
	public UnityEvent     OnDoneSequencing;

	[NonSerialized] public int  index;
	//[NonSerialized] public bool sequencing;
	[NonSerialized] public State state;
	public                 bool  Sequencing => state == State.Sequencing;
	public                 bool  IsActive   => state >= State.Sequencing;

	public static Action OnStartTextSequenceUI;

	/// <summary>
	/// Indicates that the next Advance() call will finish this sequencer,
	/// since all lines have been fully displayed to completion.
	/// </summary>
	public bool NextAdvanceFinishes => index >= Lines.Count && Displayer.DisplayProcessDone;

	private void Awake()
	{
		if (Displayer == null) Displayer = GetComponent<ITextDisplayer>();
	}

	public void Restart()
	{
		if (!IsActive)
			return;

		index = 0;
		Displayer.DisplayNew(Lines[0]);
	}

	public void StartSequence(List<GameText> lines)
	{
		if (IsActive || lines.Count <= 0)
			return;

		state = State.Sequencing;

		Lines = lines;
		index = 0;
		Displayer.DisplayNew(Lines[0]);
		OnStartTextSequenceUI?.Invoke();
	}

	public void StopSequencing(bool callback = true)
	{
		if (!IsActive)
			return;

		state = State.Off;
		Displayer.StopDisplaying();

		if(callback)
			OnDoneSequencing.Invoke();
	}

	public void Advance()
	{
		if (!IsActive)
			return;

		if (!Displayer.DisplayProcessDone)
		{
			Displayer.FinishDisplayProcess();
			return;
		}

		index++;
		if (NextAdvanceFinishes)
		{
			// End
			StopSequencing();
		}
		else
		{
			// Next line
			Displayer.DisplayNew(Lines[index]);
			OnDisplayNext.Invoke();
		}
	}

	public void Pause()
	{
		if (state != State.Sequencing) return;
		state = State.Paused;

		Displayer.Pause();
	}

	public void Unpause()
	{
		if (state != State.Paused) return;
		state = State.Sequencing;

		Displayer.Unpause();
	}
}