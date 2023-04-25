namespace Anjin.UI
{
	//[RequireComponent(typeof(HUDElement))]
	/*public class HUDElementSequencer
	{
		public enum SequenceState { Off, Start, Executing, End }

		[ShowInInspector]
		SequenceState state;

		public HUDElement parent;
		public bool Paused = false;

		public List<SequenceType> CurrentSequenceList;
		public List<SequenceType> RunningSequences;
		public int                index;

		public HUDElementSequencer(HUDElement _parent)
		{
			parent = _parent;
			state = SequenceState.Off;
			CurrentSequenceList = new List<SequenceType>();
			RunningSequences = new List<SequenceType>();
		}

		public void UpdateCycle(float dt)
		{
			if (!Paused)
			{
				//SequenceType currentSequence = null;
				if (state != SequenceState.Off)
				{
					//currentSequence = CurrentSequenceList[index];
				}

				switch (state)
				{

				case SequenceState.Start:
					//Start: Loop through and start all sequences until we get to one that blocks

					if (CurrentSequenceList.Count > 0)
					{
						index = 0;
						//First sequence
						var sequence = CurrentSequenceList[index];
							sequence.sequencer = this;
							RunningSequences.Add(sequence);
							sequence._Enter();

						while (!sequence._Blocking && index < CurrentSequenceList.Count)
						{
							sequence = CurrentSequenceList[index];
								sequence.sequencer = this;
								RunningSequences.Add(sequence);
								sequence._Enter();
							index++;
						}

						state = SequenceState.Executing;
					}
					else
					{
						state = SequenceState.Off;
					}

					break;

				case SequenceState.Executing:

					//Execute: Update all running sequences. If
					SequenceType seq;
					bool blocking = false;
					for (int i = 0; i < RunningSequences.Count; i++)
					{
						seq = RunningSequences[i];
						seq.Execute();
						if (seq._Blocking) blocking = true;
					}

					//Remove sequences that are done
					for (int i = 0; i < RunningSequences.Count; i++)
					{
						seq = RunningSequences[i];
						if (seq.Done)
						{
							seq._Exit();
							RunningSequences.Remove(seq);
							i--;
						}
					}

					//If we aren't at the end of the list, we continue down the list
					if(index < CurrentSequenceList.Count -1)
					{
						//If nothing is blocking us
						if (!blocking)
						{
							index++;
							var sequence = CurrentSequenceList[index];
							sequence.sequencer = this;
							RunningSequences.Add(sequence);
							sequence._Enter();

							while (!sequence._Blocking && index < CurrentSequenceList.Count)
							{
								sequence           = CurrentSequenceList[index];
								sequence.sequencer = this;
								RunningSequences.Add(sequence);
								sequence._Enter();
								index++;
							}
						}
					}
					else if(RunningSequences.Count == 0)
					{
						//If we're at the end of the list and no running sequences are left, we end
						index = 0;
						state = SequenceState.Off;
					}



					break;

				case SequenceState.End:

					/*currentSequence._Exit();

					if (index < CurrentSequenceList.Count-1)
					{
						index++;
						currentSequence = CurrentSequenceList[index];

						currentSequence.sequencer = this;
						currentSequence._Enter();

						if(!currentSequence.Done)
							state = SequenceState.Executing;
					}
					else
					{
						//RunningSequence.Clear();
						index = 0;
						state = SequenceState.Off;
					}#1#

					break;
				}
			}
		}

		public void StartSequence() => StartSequence(CurrentSequenceList);

		public void StartSequence(SequenceType seq)
		{
			if (state == SequenceState.Off && seq != null)
			{
				CurrentSequenceList.Clear();
				CurrentSequenceList.Add(seq);
				StartSequence(CurrentSequenceList);
			}
		}
		public void StartSequence(List<SequenceType> SequenceList)
		{
			if (state == SequenceState.Off && SequenceList != null && SequenceList.Count > 0)
			{
				state = SequenceState.Start;
				CurrentSequenceList.Clear();
				CurrentSequenceList.AddRange(SequenceList);
				index = 0;
			}
		}

		public void StopSequence()
		{
			if (state != SequenceState.Off)
			{
				for (int i = 0; i < RunningSequences.Count; i++)
				{
					RunningSequences[i]._Exit();
				}
				RunningSequences.Clear();

				index = 0;
				state = SequenceState.Off;
			}
		}


	}

	public abstract class SequenceType
	{
		public HUDElementSequencer sequencer;

		public Handler OnEnter;
		public Handler OnDone;

		public SequenceType(Handler _OnEnter = null, Handler _OnDone = null) { OnEnter = _OnEnter; OnDone = _OnDone; }

		public abstract bool Done      { get; }
		public abstract bool _Blocking  { get; }
		public void _Enter() { Enter(); OnEnter?.Invoke(); }
		public virtual  void Enter()   {}
		public virtual  void Execute() {}
		public void _Exit() { Exit(); OnDone?.Invoke(); }
		public virtual  void Exit()    {}
	}

	public class TestSequence : SequenceType
	{
		public override bool Done => (timer >= time);
		public override bool _Blocking => true;
		public bool Blocking;
		public string text;

		public float time;
		public float timer;

		public TestSequence(string _text, float _time, bool blocking)
		{
			text = _text;
			time = _time;
			timer = 0;
			Blocking = blocking;
		}

		public override void Enter()
		{
			//Debug.Log(sequencer.parent.gameObject.name + " Enter");
			Debug.Log(text);
		}

		public override void Execute()
		{
			//Debug.Log(sequencer.parent.gameObject.name + " Execute");
			timer += Time.deltaTime;
		}

		public override void Exit()
		{
			//Debug.Log(sequencer.parent.gameObject.name + " Exit");
			Debug.Log(text + "End");
		}
	}

	public class AlphaSequence : SequenceType
	{
		public override bool Done => _done;
		private         bool _done;

		public override bool _Blocking => Blocking;
		public bool Blocking;

		public float time;
		public float timer;

		/// <summary>
		/// Set to -1 to sequence from current alpha
		/// </summary>
		public float startingAlpha;
		public float targetAlpha;

		public AlphaSequence(float _time, float _startingAlpha = -1, float _targetAlpha = 1, bool blocking = true)
		{
			time        = _time;
			startingAlpha = _startingAlpha;
			targetAlpha = _targetAlpha;
			Blocking = blocking;
		}

		public override void Enter()
		{
			base.Enter();
			_done         = false;
			if(startingAlpha == -1)
				startingAlpha = sequencer.parent.alpha;
			else
				sequencer.parent.alpha = startingAlpha;

			timer         = 0;
		}

		public override void Execute()
		{
			base.Execute();
			timer += Time.deltaTime;

			sequencer.parent.alpha = startingAlpha + ( targetAlpha - startingAlpha ) * Mathf.SmoothStep(0, 1, timer / time);

			if (timer >= time)
			{
				_done = true;
			}
		}

		public override void Exit()
		{
			base.Exit();
			sequencer.parent.alpha = targetAlpha;
		}
	}

	public class WorldOffsetSequence : SequenceType
	{
		public override bool Done => _done;
		private         bool _done;
		public override bool _Blocking => Blocking;
		public bool Blocking;

		public float time;
		public float timer;

		public Vector3 StartingOffset;
		public Vector3 TargetOffset;

		public WorldOffsetSequence(float _time, Vector3 _startingOffset, Vector3 _targetOffset, bool blocking)
		{
			time          = _time;
			StartingOffset = _startingOffset;
			TargetOffset = _targetOffset;
			Blocking = blocking;
		}

		public override void Enter()
		{
			base.Enter();
			_done = false;

			sequencer.parent.SequenceOffset = StartingOffset;

			timer = 0;
		}

		public override void Execute()
		{
			base.Execute();
			timer += Time.deltaTime;

			sequencer.parent.SequenceOffset = Vector3.Lerp(StartingOffset, TargetOffset, Mathf.SmoothStep(0, 1, timer / time));

			if (timer >= time)
			{
				_done = true;
			}
		}

		public override void Exit()
		{
			base.Exit();
			sequencer.parent.SequenceOffset = TargetOffset;
		}
	}*/
}