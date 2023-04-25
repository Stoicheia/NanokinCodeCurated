using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Anjin.Util;
using Combat.Components;
using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Features.TurnOrder.Events;
using Combat.Features.TurnOrder.Sampling.Operations;
using DG.Tweening;
using JetBrains.Annotations;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI.Extensions;
using UnityUtilities;
using Util;
using Util.Collections;
using Util.UniTween.Value;
using Util.UniTween.Value.Blending;
using Action = Combat.Features.TurnOrder.Action;

namespace Combat.UI.TurnOrder
{
	/// <summary>
	/// Root component of a action order UI.
	/// IDEA dynamic scaling depending on round length.
	/// We want to keep at least a whole round in view if possible.
	/// </summary>
	public class TurnUI : StaticBoy<TurnUI>
	{
		// Refs
		// ----------------------------------------
		[Title("References")]
		[SerializeField] public GameObject Root;
		[SerializeField, SceneObjectsOnly]     public Transform  TurnRoot;
		[SerializeField, AssetsOnly, Required] public GameObject TurnPrefab;
		[SerializeField, AssetsOnly, Required] public GameObject TriggerPrefab;

		// Config
		// ----------------------------------------
		[Title("Config")]
		[SerializeField] public float TurnSpacing = 6;
		[SerializeField] public float StackedTurnSpacing      = 6;
		[SerializeField] public float RoundSpacing            = 12;
		[SerializeField] public int   DisplayCount            = 12;
		[SerializeField] public int   MaxCardsVisuallyStacked = 3;
		[Space]
		[SerializeField] private SelectUIStyle SelectStyle = SelectUIStyle.Default;

		//
		// ----------------------------------------
		[Title("SFX")]
		[SerializeField]
		public AudioDef SFX_Advance;

		// Animation
		// ----------------------------------------
		[Title("Animation")]
		[SerializeField] private Vector2 SwapHeight = new Vector2(24, 8);
		[SerializeField] private float SwapScale       = 0.875f;
		[SerializeField] private Easer AddEase         = Easer.Linear;
		[SerializeField] private Easer StateEase       = Easer.Linear;
		[SerializeField] private Easer IntroEase       = Easer.Linear;
		[SerializeField] private Easer IntroStateEase  = Easer.Linear;
		[SerializeField] private Easer SwapMoveEase    = Easer.Linear;
		[SerializeField] private Easer SwapUnmoveEase  = Easer.Linear;
		[SerializeField] private Easer SwapHeightIn    = Easer.Linear;
		[SerializeField] private Easer SwapScaleIn     = Easer.Linear;
		[SerializeField] private Easer SwapHeightOut   = Easer.Linear;
		[SerializeField] private Easer SwapScaleOut    = Easer.Linear;
		[SerializeField] private Easer AdvanceMoveEase = Easer.Linear;

		[SerializeField] private Easer UnstackRotationease = Easer.Linear;

		[SerializeField] private AnimationCurve SwapMoveHeightCurve;

		[SerializeField] private float OpTiming1         = 0.15f;
		[SerializeField] private float OpTiming2         = 0;
		[SerializeField] private float OpTiming3         = 0;
		[SerializeField] private float OpTiming4         = 0;
		[SerializeField] private float AdvanceOutroDelay = 0.25f;
		[SerializeField] private float FlashPower        = 0.1f;
		[SerializeField] private Easer TurnFlashEaseIn   = new Easer(0.2f, Ease.InSine);
		[SerializeField] private Easer TurnFlashEaseOut  = new Easer(0.2f, Ease.InSine);


		/// <summary>
		/// The selection manager used for highlighting turns visually during selection.
		/// </summary>
		public static SelectUIManager selection;

		private static int _autoincViewID = 0;

		[ShowInInspector] private static Map<TurnInfo, ViewTurn> _locviewmap;

		// Trigger tracking
		private static Dictionary<Trigger, int> _triggerTracker; // track how many times a trigger has been used, reset every action. We use this to skip the trigger that many times for the next internal state update.
		private static ITurnActer               _lastEvent;      // used for start_turns

		private static ViewPool _viewPool;

		private static bool        _isInitialized;
		private static Battle _battle;
		private static TurnSystem  _turnsys;

		private static TweenableFloat _roundSpacing;

		[ShowInInspector] public static List<TurnInfo> PreviousInfos { get; private set; }
		[ShowInInspector] public static List<TurnInfo> CurrentInfos  { get; private set; }
		[ShowInInspector] public static ViewOrder      CurrentViews  { get; private set; }


		protected override void OnAwake()
		{
			_locviewmap = new Map<TurnInfo, ViewTurn>();
			_viewPool   = new ViewPool();

			// Trigger tracking
			_triggerTracker = new Dictionary<Trigger, int>();
			_lastEvent      = null;

			CurrentInfos = null;
			CurrentViews = null;

			_isInitialized = false;
			_battle        = null;
			_turnsys       = null;

			if (TurnRoot == null)
				TurnRoot = transform;

			_viewPool.ObjectParent =  TurnRoot;
			_viewPool.Allocated    += OnViewAllocated;

			selection = new SelectUIManager();

			_roundSpacing = new TweenableFloat(RoundSpacing);
		}

		private static void OnViewAllocated(PooledView poolee)
		{
			poolee.vc.gameObject.name = $"{poolee.prefab.name} {_autoincViewID++}";
		}

		/// <summary>
		/// Initialize the action order for a battle.
		/// </summary>
		/// <param name="battle"></param>
		public static void Initialize([NotNull] Battle battle)
		{
			_isInitialized = true;
			_battle        = battle;
			_turnsys       = battle.turns;

			_battle.TriggerAdded          += trigger => Sync(); // TODO use anim with proper insertion
			_battle.FighterRemoved        += fter => Sync();    // TODO use anim
			_battle.TurnOperationApplied  += OnBattleTurnOperation;
			_battle.TurnOperationsApplied += OnBattleTurnOperations;
		}

		private static void OnBattleTurnOperation(Battle.TurnOperationType type, TurnSM op)
		{
			Animate(op);
		}

		private static void OnBattleTurnOperations(Battle.TurnOperationType type, [NotNull] List<TurnSM> ops)
		{
			foreach (TurnSM op in ops)
			{
				Animate(op); // TODO decide if we want to batch them or what
			}
		}

		/// <summary>
		/// Set the action order visible or not.
		/// </summary>
		/// <param name="b"></param>
		public static void SetVisible(bool b)
		{
			Live.Root.SetActive(b);
		}

		public static void Reset()
		{
			// Release existing action views
			List<ViewTurn> pool = ListPool<ViewTurn>.Claim();
			ReleaseViews(pool);
			ListPool<ViewTurn>.Release(ref pool);

			// Reset
			_viewPool.ReleaseAll();
			_locviewmap.Clear();

			_battle      = null;
			_turnsys     = null;
			CurrentInfos = null;

			_autoincViewID = AutoIncrementInt.Zero;
		}


		/// <summary>
		/// Update the cached state so it matches the current info, advancing to the specified action.
		/// </summary>
		/// <param name="goalEvent"></param>
		/// <returns></returns>
		[CanBeNull]
		[SuppressMessage("ReSharper", "RedundantCast")]
		private static ViewOrder UpdateStateTo([CanBeNull] ITurnActer goalEvent)
		{
			UpdateInfoInternal();
			_triggerTracker.Clear();

			int startIndex = -1;

			// Search through CachedInfo
			for (var i = 0; i < CurrentInfos.Count; i++)
			{
				if (CurrentInfos[i].action.acter == goalEvent)
				{
					startIndex = i;
					break;
				}
			}

			if (startIndex == -1)
				return null;

			_lastEvent = goalEvent;
			return UpdateStateInternal(startIndex);
		}

		/// <summary>
		/// Update the cached state so it matches the current info, advancing to the specified trigger.
		/// </summary>
		/// <param name="goalEvent"></param>
		/// <returns></returns>
		[CanBeNull]
		[SuppressMessage("ReSharper", "RedundantCast")]
		private static ViewOrder UpdateStateTo([CanBeNull] Trigger goalTrigger)
		{
			UpdateInfoInternal();

			int startIndex = -1;

			// Search through CachedInfo
			for (var i = 0; i < CurrentInfos.Count; i++)
			{
				if (CurrentInfos[i].trigger == goalTrigger)
				{
					startIndex = i;
					break;
				}
			}

			if (startIndex == -1)
				return null;

			return UpdateStateInternal(startIndex);
		}

		/// <summary>
		/// Update the cached order so it matches the current action order.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		private static ViewOrder UpdateState(int startIndex = 0)
		{
			UpdateInfoInternal();
			return UpdateStateInternal(startIndex);
		}

		private static void UpdateInfoInternal()
		{
			PreviousInfos = CurrentInfos;
			CurrentInfos  = new List<TurnInfo>();

			CurrentInfos.Clear();

			// Iterate the action order and build this UI info order
			// ---------------------------------------------------------

			var        iround    = 0; // Round index
			var        iturn     = 0; // Index within the current round (resets to 0 at the end of each round)
			var        igroup    = 0; // Index within the current group (resets to 0 whenever the event changes)
			ITurnActer lastEvent = null;

			for (int isys = 0; isys < _turnsys.decoratedOrder.Count; isys++)
			{
				Action action = _turnsys.decoratedOrder[isys];

				switch (action.marker)
				{
					case ActionMarker.RoundHead:
						iround++;
						iturn     = 0;
						lastEvent = null;
						igroup    = 0;
						break;

					case ActionMarker.TriggerDeco:
					case ActionMarker.Action:
					{
						// Ignore/merge consecutive triggers with the same ID (e.g. could be from a state assigned to multiple fighters)
						if (action.marker == ActionMarker.TriggerDeco && isys > 0)
						{
							Action prevturn = _turnsys.decoratedOrder[isys-1];
							if (prevturn.marker == ActionMarker.TriggerDeco && action.trigger.ID == prevturn.trigger.ID)
								continue;
						}

						bool isFirst = lastEvent != action.acter;
						if (isFirst)
						{
							igroup = 0;
						}

						var info = new TurnInfo(action)
						{
							listIndex    = CurrentInfos.Count, // index in list
							turnIndex    = iturn,              // index in round
							roundIndex   = iround,             // round index
							groupIndex   = igroup,
							firstInGroup = isFirst || action.marker == ActionMarker.TriggerDeco
						};

						CurrentInfos.Add(info);

						lastEvent = action.acter;
						iturn++;
						igroup++;

						// Increase the group count
						for (var i = CurrentInfos.Count - 1; i >= 0; i--)
						{
							TurnInfo ti = CurrentInfos[i];
							if (ti.roundIndex != info.roundIndex || ti.acter != lastEvent)
								break;

							ti.groupCount++;
							CurrentInfos[i] = ti;
						}

						break;
					}
				}
			}


			for (var i = 0; i < CurrentInfos.Count; i++)
			{
				TurnInfo ci = CurrentInfos[i];
				if (ci.marker == ActionMarker.TriggerDeco)
				{
					if (i > 0)
					{
						bool leftContext = ci.trigger.signal == Signals.end_turn ||
						                   ci.trigger.signal == Signals.end_turns ||
						                   ci.trigger.signal == Signals.end_skill ||
						                   ci.trigger.signal == Signals.start_round;
						if (leftContext && FindPrevNonDeco(CurrentInfos, i, out TurnInfo left))
							ci.left = left.action;
					}

					if (i < CurrentInfos.Count - 1)
					{
						bool rightContext = ci.trigger.signal == Signals.start_turn ||
						                    ci.trigger.signal == Signals.start_turns ||
						                    ci.trigger.signal == Signals.start_skill ||
						                    ci.trigger.signal == Signals.end_round;
						if (rightContext && FindNextNonDeco(CurrentInfos, i, out TurnInfo right))
							ci.right = right.action;
					}

					CurrentInfos[i] = ci;
				}
			}
		}

		private static bool FindPrevNonDeco(List<TurnInfo> order, int from, out TurnInfo ret)
		{
			for (int i = from; i >= 0; i--)
			{
				if (order[i].action.marker != ActionMarker.TriggerDeco)
				{
					ret = order[i];
					return true;
				}
			}

			ret = new TurnInfo();
			return false;
		}

		private static bool FindNextNonDeco(List<TurnInfo> order, int from, out TurnInfo ret)
		{
			for (int i = from; i < order.Count; i++)
			{
				if (order[i].action.marker != ActionMarker.TriggerDeco)
				{
					ret = order[i];
					return true;
				}
			}

			ret = new TurnInfo();
			return false;
		}

		[SuppressMessage("ReSharper", "RedundantCast")]
		private static ViewOrder UpdateStateInternal(int startIndex = 0)
		{
			CurrentViews = new ViewOrder(CurrentInfos);
			Vector2 pos = Vector2.zero;

			ViewInfo? currentStackHeadInfo = null;
			ViewInfo? currentStackHeadView = null;

			// Calculate positioning for the infos
			for (int i = startIndex; i < CurrentInfos.Count; i++)
			{
				TurnInfo  info = CurrentInfos[i];
				TurnInfo? prev = i > 0 ? CurrentInfos[i - 1] : (TurnInfo?)null;
				TurnInfo? next = i < CurrentInfos.Count - 1 ? CurrentInfos[i + 1] : (TurnInfo?)null;

				bool enableGroupMerge = GameOptions.current.combat_merge_groupturns;
				info.listIndex -= startIndex;

				if (i - startIndex > 0)
				{
					if (prev?.roundIndex < info.roundIndex)
						pos.x += Live.RoundSpacing;
					else if (!enableGroupMerge || info.marker == ActionMarker.Action && info.firstInGroup)
						pos.x += GetLeftSpacing(info);
				}

				// Get the view info for the action.
				ViewInfo vi = new ViewInfo(info, pos, GetState(info, prev, next), GetFriendness(info))
				{
					stackMerged = true
				};

				if (info.firstInGroup)
					currentStackHeadInfo = vi;
				else if (currentStackHeadInfo.HasValue)
					vi.stackHeadState = currentStackHeadInfo.Value.state;

				// Get the view for the action.
				ViewTurn vc = GetView(info, vi, info.marker == ActionMarker.TriggerDeco ? 1 : (int?)null);
				vc.SetTurn(info);
				vc.vInfo = vi;

				CurrentViews.Add(vc);
				CurrentViews.AddInformation(vc, vi);

				// TODO: Proper Sorting
				vc.SortingIndex = Mathf.Clamp(CurrentInfos.Count, 0, vc.Rect.parent.childCount - 1) - i /* + 1*/;

				// Right spacing
				ViewInfo spacingRef = vi;
				if (enableGroupMerge)
				{
					Assert.IsTrue(currentStackHeadInfo != null, nameof(currentStackHeadInfo) + " != null");
					spacingRef = currentStackHeadInfo.Value;
				}

				if (!enableGroupMerge || next?.firstInGroup == true)
				{
					pos.x += vc.GetDesiredSize(spacingRef).x;
					pos.x += GetRightSpacing(spacingRef.info);
				}
			}

			return CurrentViews;
		}


		/// <summary>
		/// Syncs all the cards to the current action order in a single frame.
		/// </summary>
		[Button(ButtonSizes.Large)] public static void Sync()
		{
			Assert.IsTrue(_isInitialized);

			ViewOrder vieworder = UpdateState();

			foreach (ViewTurn view in vieworder)
			{
				ViewInfo vi = vieworder.GetInfo(view);
				view.Set(vi);
			}
		}

	#region Highlights

		/// <summary>
		/// Highlight or not all the cards in the current action order.
		/// </summary>
		public static void SetHighlight(bool b)
		{
			foreach (ViewTurn view in CurrentViews)
			{
				selection.Set(ref view.selection, b);
			}
		}

		/// <summary>
		/// Highlight or not certain cards in the current action order.
		/// </summary>
		public static void SetHighlight(Target target, bool b)
		{
			foreach (Fighter fter in target.fighters) SetHighlight(fter, b);
			foreach (Slot slot in target.slots)
			{
				if (slot.owner != null)
					SetHighlight(slot.owner, b);
			}
		}

		/// <summary>
		/// Highlight or not a single card in the current action order.
		/// </summary>
		public static void SetHighlight(Fighter fighter, bool b)
		{
			foreach (ViewTurn view in CurrentViews)
			{
				if (view.Info.acter == fighter)
					selection.Set(ref view.selection, b);
			}
		}

	#endregion

		private void Update()
		{
			foreach (ViewTurn view in CurrentViews)
			{
				selection.Update(ref view.selection, ref SelectStyle);
			}
		}

		/// <summary>
		/// Release a view, meant to be used by animation code.
		/// </summary>
		/// <param name="vc"></param>
		public static void ReleaseView([NotNull] ViewTurn vc)
		{
			vc.Set(ViewInfo.Inactive);

			_locviewmap.Remove(vc);
			_viewPool.Release(vc.poolee);
		}

		/// <summary>
		/// Release many views, meant to be used by animation code.
		/// </summary>
		/// <param name="to_release"></param>
		public static void ReleaseViews([NotNull] List<ViewTurn> to_release)
		{
			foreach (ViewTurn view in to_release)
			{
				ReleaseView(view);
			}
		}

		/// <summary>
		/// Get the left spacing for a action info.
		/// </summary>
		public static float GetLeftSpacing(TurnInfo info)
		{
			switch (info.marker)
			{
				case ActionMarker.Action:
					return (info.groupCount > 1 ? Live.StackedTurnSpacing : Live.TurnSpacing);
					;
				case ActionMarker.TriggerDeco:
					Signals signal = info.trigger.signal;
					return signal.IsEnd() ? Live.TurnSpacing / 2f : Live.TurnSpacing;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Get the right spacing for a action info.
		/// </summary>
		/// <param name="info"></param>
		public static float GetRightSpacing(TurnInfo info)
		{
			switch (info.marker)
			{
				case ActionMarker.Action:
					return (info.groupCount > 1 ? Live.StackedTurnSpacing : Live.TurnSpacing);
				case ActionMarker.TriggerDeco:
					Signals signal = info.trigger.signal;
					return signal.IsStart() ? Live.TurnSpacing / 2f : Live.TurnSpacing;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}


		/// <summary>
		/// Get a view for this info.
		/// </summary>
		/// <param name="turn"></param>
		/// <param name="vi"></param>
		/// <returns></returns>
		public static ViewTurn GetView(TurnInfo info, ViewInfo vi, int? maxdist = null)
		{
			if (_locviewmap.TryGetValue(info, out ViewTurn vc))
			{
				// if (!maxdist.HasValue || view.Info.listIndex - info.listIndex <= maxdist.Value)
				return vc;
			}

			// Get the prefab for this view
			// ----------------------------------------
			GameObject prefab;
			switch (vi.info.marker)
			{
				case ActionMarker.Action:
					Assert.IsNotNull(vi.info.action.acter, "state.info.action.@event != null");

					prefab = vi.info.action.acter.TurnPrefab;
					if (prefab == null)
						prefab = Live.TurnPrefab;

					break;

				case ActionMarker.TriggerDeco:
					prefab = Live.TriggerPrefab;
					break;

				case ActionMarker.Null:
				case ActionMarker.RoundHead:
				case ActionMarker.RoundBody:
				case ActionMarker.RoundTail:
				default:
					throw new ArgumentOutOfRangeException();
			}

			// Register the view
			// ----------------------------------------
			PooledView poolee = _viewPool.GetAndLock(prefab);
			vc        = poolee.vc;
			vc.poolee = poolee;

			AssignViewToInfo(info, vc);

			vc.Set(vi);

			return vc;
		}

		private static void AssignViewToInfo(TurnInfo info, [NotNull] ViewTurn vc)
		{
			_locviewmap.Set(info, vc);

			vc.SetTurn(info);
		}

		/// <summary>
		/// Resolve the state for this action using the basic logic to do.
		/// </summary>
		/// <param name="turnID"></param>
		/// <returns></returns>
		public static ViewStates GetState(TurnInfo info, TurnInfo? prev = null, TurnInfo? next = null)
		{
			switch (info.marker)
			{
				case ActionMarker.Action:
					if (info.listIndex == 0)
						return ViewStates.Major;

					if (GameOptions.current.combat_merge_groupturns)
					{
						if (info.firstInGroup)
							return info.roundIndex == 0
								? ViewStates.Major
								: ViewStates.Minor;
						else
						{
							return ViewStates.Stacked;
							//return ViewStates.Hidden;
						}
					}
					else
					{
						return info.firstInGroup
							? ViewStates.Major
							: ViewStates.Minor;
					}

				case ActionMarker.TriggerDeco:
					Signals signal = info.trigger.signal;

					if (signal.IsStart() && next != null) return GetState(next.Value);
					if (signal.IsEnd() && prev != null) return GetState(prev.Value);

					return ViewStates.Major;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Resolve the friendness for this action using the basic logic to do.
		/// </summary>
		/// <param name="turnID"></param>
		/// <returns></returns>
		public static ViewFriendness GetFriendness(TurnInfo info)
		{
			Team team;

			switch (info.marker)
			{
				case ActionMarker.Action:
					team = _battle.GetTeam(info.action.acter);
					break;

				case ActionMarker.TriggerDeco:
					team = info.trigger.GetEnv().skill?.user.team;
					break;

				case ActionMarker.Null:
				case ActionMarker.RoundHead:
				case ActionMarker.RoundBody:
				case ActionMarker.RoundTail:
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (team == null)
				return ViewFriendness.Neutral;

			return team.isPlayer
				? ViewFriendness.Ally
				: ViewFriendness.Enemy;
		}


		/// <summary>
		/// Execute and animate the query, assuming it was just executed.
		/// </summary>
		public static Tween Animate([CanBeNull] TurnSM op = null)
		{
			Sequence fullseq = DOTween.Sequence();

			if (!op.modified) // TODO this
				return fullseq;

			Sequence seq_intro  = DOTween.Sequence();
			Sequence seq_move   = DOTween.Sequence();
			Sequence seq_insert = DOTween.Sequence();
			Sequence seq_remove = DOTween.Sequence();
			Sequence seq_outro  = DOTween.Sequence();

			ViewOrder oldstate = CurrentViews;
			ViewOrder newstate = UpdateState();

			// Animate action removals.
			List<ViewTurn> removals   = oldstate.FindRemovedViews(newstate);
			List<ViewTurn> insertions = oldstate.FindAddedViews(newstate);


			// Animate action swaps.
			foreach (ViewTurn view in newstate)
			{
				ViewInfo  vi2 = newstate.GetInfo(view);
				ViewInfo? vi1 = oldstate.TryGetInfo(view);


				if (!vi1.HasValue) // New
					continue;

				bool moved = op?.selection.Contains(view.Action) == true;
				if (moved)
				{
					ViewStyle style = view.StateStyles.Get(vi2.state);

					// 1. Scale down
					seq_intro.Join(view.Scale(Vector2.one * Live.SwapScale, Live.SwapScaleIn));

					// 2. Move up a little.
					seq_intro.Join(view.Position(vi1.Value.position + Live.SwapHeight, Live.SwapHeightIn));

					// 3. Move to the new position.
					seq_move.Join(view.Position(vi2.position + Live.SwapHeight, Live.SwapMoveEase));

					//view.position.To(vi2.position + Live.SwapHeight, new CurveTo { curve = Live.SwapMoveHeightCurve });

					seq_outro.Join(view.Position(vi2.position, Live.SwapHeightOut));
					seq_outro.Join(view.State(vi2.state, Live.StateEase));

					// 4. Move down a little.
					//seq_outro.Join(view.Position(vi2.position, Live.SwapHeightOut));
					//seq_outro.Join(view.State(vi2.state, Live.StateEase));

					// 5. Scale up.
					//seq_outro.Join(view.Scale(style.scale, Live.SwapScaleOut));
				}
				else
				{
					// Simply move to the new position.
					seq_outro.Join(view.Position(vi2.position, Live.SwapUnmoveEase));
					seq_outro.Join(view.State(vi2.state, Live.StateEase));
				}
			}

			// foreach (ViewTurn vt in removals)
			// {
			// 	ViewInfo vi = oldstate.GetInfo(vt);
			// 	vi.position += Vector2.down * 100;
			// 	vi.state    =  ViewStates.Hidden;
			//
			// 	seq1_intro.Join(vt.Scale(Vector2.one * 0.85f, Live.SwapScaleOut));
			// }


			seq_remove.Join(AnimateRemoves(oldstate, removals));

			// Animate action insertions.
			foreach (ViewTurn v in insertions)
			{
				ViewInfo vi1 = newstate.GetInfo(v);
				ViewInfo vi2 = newstate.GetInfo(v);

				if (op.selection.Contains(vi1.info.action))
					continue;

				vi1.position += Vector2.up * 100;
				vi1.state    =  ViewStates.Hidden;
				v.Set(vi1);

				// Move down to end.position
				seq_insert.Join(v.Position(vi2.position, Live.AddEase));
				seq_insert.Join(v.State(vi2.state, Live.StateEase));
			}

			fullseq.Append(seq_intro);
			if (seq_intro.Duration() > 0)
				fullseq.AppendInterval(Live.OpTiming1);

			fullseq.Append(seq_move);
			if (seq_move.Duration() > 0)
				fullseq.AppendInterval(Live.OpTiming2);

			fullseq.Append(seq_outro);
			if (seq_outro.Duration() > 0)
				fullseq.AppendInterval(Live.OpTiming3);

			fullseq.Append(seq_insert);
			if (seq_insert.Duration() > 0)
				fullseq.AppendInterval(Live.OpTiming4);

			fullseq.Append(seq_remove);

			return fullseq;
		}

		public static Sequence FlashTurn(ViewTurn vt)
		{
			Sequence seq = DOTween.Sequence();

			seq.Append(vt.Fill(vt.MainColor.Alpha(Live.FlashPower), Live.TurnFlashEaseIn));
			seq.Append(vt.Fill(Color.clear, Live.TurnFlashEaseOut));

			return seq;
		}

		[Button, CanBeNull]
		public static Sequence AnimateAdvance(ITurnActer goal)
		{
			ViewOrder old = CurrentViews;
			ViewOrder now = UpdateStateTo(goal);

			if (now == null)
				return null;

			return AnimateAdvance(old, now);
		}

		[CanBeNull]
		public static Sequence AnimateAdvance(Trigger trg)
		{
			ViewOrder old = CurrentViews;
			ViewOrder now = UpdateStateTo(trg);

			if (now == null)
				return null;

			// increment the trigger tracker
			if (!_triggerTracker.ContainsKey(trg))
				_triggerTracker.Add(trg, 0);

			_triggerTracker[trg]++;

			// Advance the action order with the new data
			return AnimateAdvance(old, now);
		}

		private static Sequence AnimateAdvance([NotNull] ViewOrder old, [NotNull] ViewOrder now)
		{
			Sequence sequence = DOTween.Sequence();

			GameSFX.PlayGlobal(Live.SFX_Advance);

			// Animate leaving turns. (i.e. the old active action)
			List<ViewTurn> removed = old.FindRemovedViews(now);
			List<ViewTurn> added   = old.FindAddedViews(now);

			AnimateRemoves(old, removed, true)
				.OnComplete(() => ReleaseViews(removed)) // Those views will be ready for recycling and be re-used for different turns!
				.JoinTo(sequence);

			// AnimateAdded(now, added)
			// 	.JoinTo(sequence);


			// Animate the turns which are stepping forward in the action order.
			foreach (ViewTurn view in now)
			{
				ViewInfo  vi2 = now.GetInfo(view);
				ViewInfo? vi1 = old.TryGetInfo(view);

				if (vi1.HasValue && vi2.info.firstInGroup && !vi1.Value.info.firstInGroup)
				{
					view.Position(vi2.position, Live.AdvanceMoveEase).JoinTo(sequence);
					view.State(vi2.state, Live.UnstackRotationease).JoinTo(sequence);
				}
				else
				{
					view.Position(vi2.position, Live.AdvanceMoveEase).JoinTo(sequence);
					view.State(vi2.state, Live.StateEase).JoinTo(sequence);
				}
			}

			sequence.AppendInterval(Live.AdvanceOutroDelay);
			if (now[0].Info.marker == ActionMarker.Action) // Flash the next acting action
				sequence.Append(FlashTurn(now[0]));

			return sequence;
		}

		public static Sequence AnimateIntro()
		{
			Sequence seq = DOTween.Sequence();

			//seq.AppendInterval(3);

			ViewOrder state = UpdateState();

			// TODO: slow
			RectTransform rt                = Live.GetComponent<RectTransform>();
			float         horizontal_offset = Mathf.Abs(1920 - rt.offsetMin.x);

			for (var i = 0; i < state.Count; i++)
			{
				ViewTurn view = state[i];
				ViewInfo info = state.GetInfo(view);

				if (info.position.x + rt.offsetMin.x > 1920)
					continue;

				float delay = info.info.roundIndex * 0.020f;

				Sequence seq2 = DOTween.Sequence();

				view.rotation = 0;
				view.SetPosition(info.position + new Vector2(horizontal_offset, 0));
				view.scale.Value = new Vector2(0.75f, 0.75f);

				view.Position(info.position, Live.IntroEase).JoinTo(seq2);
				view.State(info.state, Live.IntroStateEase).SetDelay(delay).AppendTo(seq2);

				seq2.SetDelay(delay);
				seq.Join(seq2);

				//view.scale.Value = new Vector2(0.1f, 1);
				//view.Scale(new Vector2(1, 1), Live.StateEase).SetDelay(delay).JoinTo(seq);

				//view.State(info.state, Live.StateEase).JoinTo(seq);
			}

			//_roundSpacing = 0;

			return seq;
		}

		/// <summary>
		/// Standard insertion anim for turns.
		/// </summary>
		/// <param name="now">The current ViewOrder we are animating.</param>
		/// <param name="added">The turns to animate.</param>
		/// <returns></returns>
		private static Tween AnimateAdded(ViewOrder now, [NotNull] List<ViewTurn> added)
		{
			Sequence seq = DOTween.Sequence();

			foreach (ViewTurn view in added)
			{
				ViewInfo start  = now.GetInfo(view);
				ViewInfo target = now.GetInfo(view);

				start.position += Vector2.up * view.GetDesiredSize(target);
				start.state    =  ViewStates.Hidden;
				view.Set(start);

				seq.Join(view.Position(target.position, Live.AddEase));
				seq.Join(view.Opacity(1, Live.AddEase));
				seq.Join(view.State(target.state, Live.StateEase));
			}

			return seq;
		}

		private static Sequence AnimateRemoves(ViewOrder old, [NotNull] List<ViewTurn> removed, bool advancing = false)
		{
			Sequence seq = DOTween.Sequence();

			for (var i = 0; i < removed.Count; i++)
			{
				ViewTurn vt = removed[i];
				ViewInfo vi = old.GetInfo(vt);

				if (advancing && vi.info.firstInGroup)
					vi.position += Vector2.left * vt.GetDesiredSize(vi);
				else
					vi.position += Vector2.down * vt.GetDesiredSize(vi);
				vi.state        = ViewStates.Hidden;
				vt.SortingIndex = -1;

				seq.Join(vt.Position(vi.position, Live.AdvanceMoveEase));
				seq.Join(vt.State(vi.state, Live.StateEase));
			}

			return seq;
		}
	}
}

// _battle.TriggerFired   +=  BattleOnTriggerFired;

// private static void BattleOnTriggerFired(Trigger trigger)
// {
// 	var v = _locviewmap.Forward.Keys;
// 	v.
// }