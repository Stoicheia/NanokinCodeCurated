using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Combat.Features.TurnOrder;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.UI.TurnOrder
{
	/// <summary>
	/// A static ordering of the turns for facilitating querying, comparison and data transformation.
	/// where, semantically, action 0 would be the current action.
	/// </summary>
	public class ViewOrder : List<ViewTurn>
	{
		public readonly List<TurnInfo> info;

		private readonly Dictionary<ViewTurn, ViewInfo> _informations = new Dictionary<ViewTurn, ViewInfo>();

		public ViewOrder(List<TurnInfo> order)
		{
			info = order;
		}

		public void AddInformation(ViewTurn turn, ViewInfo information)
		{
			_informations.AddIfMissing(turn, information);
		}

		public ViewInfo  GetInfo([NotNull]    ViewTurn vc) => _informations[vc];
		public ViewInfo? TryGetInfo([NotNull] ViewTurn vc) => _informations.TryGetValue(vc, out ViewInfo vt) ? (ViewInfo?) vt : null;

		/// <summary>
		/// Find a list of all removed views in the newer version.
		/// </summary>
		[NotNull]
		public List<ViewTurn> FindRemovedViews(ViewOrder newviews)
		{
			// Iterate updated
			var removed = new List<ViewTurn>();

			foreach (ViewTurn oldview in this)
			{
				if (!newviews.Contains(oldview))
				{
					removed.Add(oldview);
				}
			}

			return removed;
		}

		/// <summary>
		/// Find a list of all added views in the newer version.
		/// </summary>
		[NotNull]
		public List<ViewTurn> FindAddedViews([NotNull] ViewOrder newviews)
		{
			// Iterate updated
			var added = new List<ViewTurn>();

			foreach (ViewTurn newview in newviews)
			{
				if (!Contains(newview))
				{
					added.Add(newview);
				}
			}

			return added;
		}

		// public List<ViewTurn> FindMovedViews([NotNull] ViewOrder now)
		// {
		// 	// Find the turnviews which have moved (their info.turnIndex has changed by more than one)
		// 	var moved = new List<ViewTurn>();
		//
		// 	int max = Mathf.Min(Count, now.Count);
		// 	for (int i = 0; i < max; i++)
		// 	{
		// 		var oldview = this[i];
		// 		var newview = now[i];
		//
		// 		if (Mathf.Abs(oldview.Info.turnIndex - newview.Info.turnIndex) > 1)
		// 		{
		// 			moved.Add(oldview);
		// 		}
		// 	}
		//
		// 	return moved;
		// }
	}
}