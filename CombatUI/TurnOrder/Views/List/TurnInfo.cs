using System;
using System.Collections.Generic;
using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Features.TurnOrder.Events;
using Action = Combat.Features.TurnOrder.Action;

namespace Combat.UI.TurnOrder
{
	/// <summary>
	/// Contains the information provided by the action list.
	/// It is not the information that should be shown by the card, only the internal supposed data.
	/// </summary>
	public struct TurnInfo : IEquatable<TurnInfo>
	{
		public Action action;
		public Action left;
		public Action right;

		public ActionMarker marker  => action.marker;
		public ITurnActer  acter  => action.acter;
		public Trigger     trigger => action.trigger;

		public int  listIndex;
		public int  turnIndex;
		public int  roundIndex;
		public int  groupIndex;
		public int  groupCount;
		public bool firstInGroup;

		public TurnInfo(Action action)
		{
			this.action    = action;
			listIndex    = 0;
			turnIndex    = 0;
			roundIndex   = 0;
			groupIndex   = 0;
			groupCount   = 0;
			firstInGroup = false;
			left         = Action.Null;
			right        = Action.Null;
		}

		public TurnInfo(Trigger trig)
		{
			action         = new Action(0, null);
			listIndex    = 0;
			turnIndex    = 0;
			roundIndex   = 0;
			groupCount   = 0;
			groupIndex   = 0;
			firstInGroup = false;
			left         = Action.Null;
			right        = Action.Null;
		}

		private sealed class TurnEqualityComparer : IEqualityComparer<TurnInfo>
		{
			public bool Equals(TurnInfo x, TurnInfo y)
			{
				return x.action.Equals(y.action);
			}

			public int GetHashCode(TurnInfo obj)
			{
				return obj.action.GetHashCode();
			}
		}

		public static IEqualityComparer<TurnInfo> TurnComparer { get; } = new TurnEqualityComparer();

		public bool Equals(TurnInfo other) => action.Equals(other.action);

		public override bool Equals(object obj) => obj is TurnInfo other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = action.GetHashCode();
				hashCode = (hashCode * 397) ^ left.GetHashCode();
				hashCode = (hashCode * 397) ^ right.GetHashCode();
				return hashCode;
			}
		}
	}
}