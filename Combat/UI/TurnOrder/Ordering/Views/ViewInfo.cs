using UnityEngine;

namespace Combat.UI.TurnOrder
{
	public struct ViewInfo
	{
		public static ViewInfo Inactive => new ViewInfo { state = ViewStates.Inactive };

		public TurnInfo       info;
		public Vector2        position;
		public float          scale;
		public ViewStates     state;

		/// <summary>
		/// The state of the whole stack. I.E if we're in a round that's in minor state and is scaled down, we want cards in the same stack to use the parent's style
		/// </summary>
		public ViewStates?    stackHeadState;

		public ViewFriendness friendness;
		public bool           stackMerged;

		public ViewInfo(TurnInfo info, Vector2 position, ViewStates state, ViewFriendness friendness)
		{
			this.info        = info;
			this.position    = position;
			this.state       = state;
			this.friendness  = friendness;
			this.scale       = 1;
			this.stackMerged = false;
			stackHeadState   = null;
		}
	}
}