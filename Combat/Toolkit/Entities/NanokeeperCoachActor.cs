using Anjin.Actors;

namespace Combat
{
	public class NanokeeperCoachActor : CoachActor
	{
		//private ActorRenderer _renderer;
		//private AnimID _idle;

		[UnityEngine.SerializeField] private System.Collections.Generic.List<AnimID> idleIDs;
		[UnityEngine.SerializeField] private System.Collections.Generic.List<AnimID> actionIDs;
		[UnityEngine.SerializeField] private System.Collections.Generic.List<AnimID> hurtIDs;
		[UnityEngine.SerializeField] private System.Collections.Generic.List<AnimID> winIDs;
		[UnityEngine.SerializeField] private System.Collections.Generic.List<AnimID> winGoofIDs;
		[UnityEngine.SerializeField] private System.Collections.Generic.List<AnimID> lossIDs;

		private AnimID idleID;
		private AnimID actionID;
		private AnimID hurtID;
		private AnimID winID;
		private AnimID winGoofID;
		private AnimID lossID;

		public AnimID Idle => idleID;
		public AnimID Action => actionID;
		public AnimID Hurt => hurtID;
		public AnimID Win => winID;
		public AnimID WinGoof => winGoofID;
		public AnimID Loss => lossID;

		private System.Random idleRandomizer;
		private System.Random actionRandomizer;
		private System.Random hurtRandomizer;
		private System.Random winRandomizer;
		private System.Random winGoofRandomizer;
		private System.Random lossRandomizer;

		protected override void Awake()
		{
			base.Awake();

			idleRandomizer = new System.Random();
			actionRandomizer = new System.Random();
			hurtRandomizer = new System.Random();
			winRandomizer = new System.Random();
			winGoofRandomizer = new System.Random();
			lossRandomizer = new System.Random();
		}

		protected override void Start()
		{
			idleID = ((idleIDs.Count > 0) ? idleIDs[idleRandomizer.Next(0, idleIDs.Count)] : AnimID.None);
			actionID = ((actionIDs.Count > 0) ? actionIDs[actionRandomizer.Next(0, actionIDs.Count)] : AnimID.None);
			hurtID = ((hurtIDs.Count > 0) ? hurtIDs[hurtRandomizer.Next(0, hurtIDs.Count)] : AnimID.None);
			winID = ((winIDs.Count > 0) ? winIDs[winRandomizer.Next(0, winIDs.Count)] : AnimID.None);
			winGoofID = ((winGoofIDs.Count > 0) ? winGoofIDs[winGoofRandomizer.Next(0, winGoofIDs.Count)] : AnimID.None);
			lossID = ((lossIDs.Count > 0) ? lossIDs[lossRandomizer.Next(0, lossIDs.Count)] : AnimID.None);

			SetAnim(idleID);
		}

		public override void SetAnim(AnimID id)
		{
			state = id;

			// Update rendering state
			// ----------------------------------------
			ref RenderState rstate = ref _renderer.state;
			_renderer.state = new RenderState(id);

			if (id != AnimID.None)
			{
				if ((id == idleID) || (id == hurtID))
				{
					_idle = id;
				}
				else if ((id == actionID) || (id == winID) || (id == winGoofID))
				{
					rstate.animRepeats = 1;
					_renderer.animRepeats = 0;
				}
			}

			//switch (id)
			//{
			//	case AnimID.Stand:
			//	case AnimID.Sit:
			//		_idle = id;
			//		break;
			//}
		}

		protected override void Update()
		{
			ref RenderState state = ref _renderer.state;
			state.animPercent = -1;

			if ((state.animID == actionID) && (_renderer.animRepeats >= 1))
			{
				SetAnim(_idle);
			}

			//switch (state.animID)
			//{
			//	case AnimID.CombatAction when _renderer.animRepeats >= 1:
			//		// Return to idle after attacking
			//		SetAnim(_idle);
			//		break;
			//}
		}
	}
}