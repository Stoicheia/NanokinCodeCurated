using System.Diagnostics.CodeAnalysis;

namespace Combat.Data
{
	public static class Tags
	{
		// Fighter tags
		// ----------------------------------------
		public const string FTER_OBJECT = "object";

		// State tags
		// ----------------------------------------
		public const string STATE_BAD    = "bad";
		public const string STATE_GOOD   = "status";
		public const string STATE_BUFF   = "bad";
		public const string STATE_STATUS = "status";
		public const string STATE_DOT    = "dot";

		// Slot tags
		// ----------------------------------------
		public const string SLOT_VANGUARD  = "front";
		public const string SLOT_REARGUARD = "back";
	}

	// Note:
	// at the end of each doc comment,
	// there is a line sort of like this:
	//
	// EVENTTYPE(metype
	//
	// This specifies the type of TriggerEvent
	// used with the signal, as well as the type
	// of the 'me' object. This line also specifies
	// whether or not the signal is cancelable.
	//
	// - In Lua, the TriggerEvent is the 'x' arg passed to trigger handler functions.
	// - In Lua, me is accessed through 'x.me' on the TriggerEvent.
	// - Canceling is done through x.cancel() or x.cancel(true) on the TriggerEvent
	//
	// ----------------------------------------
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum Signals
	{
		none,

	#region Fighter Lifecycle

		/// <summary>
		/// Internal fighter signal for when it's killed.
		///
		/// NONE(fighter)
		/// </summary>
		kill,

		/// <summary>
		/// When the fighter is removed.
		///
		/// NONE(fighter)
		/// </summary>
		remove,

		/// <summary>
		/// When a fighter is added mid-battle.
		///
		/// Note:
		/// Fighters can be abstract objects like an environmental
		/// volcano that is part of the arena and deals damage every round and
		/// has its own turns. They can also be things like a landmine placed
		/// on the field through a skill, allowing it to be targetable, have HP,
		/// etc.
		///
		/// NONE(fighter)
		/// </summary>
		add_fighter,

		/// <summary>
		/// When a fighter is removed mid-battle.
		///
		/// NONE(fighter)
		/// </summary>
		remove_fighter,

		/// <summary>
		/// When a fighter is marked for death.
		/// Deaths are all processed in-between turns both as a current limitation of the system
		/// and a stylistic choice. Could change in the future, but animating deaths mid-skill
		/// without glitching the skill's animation could be challenging.
		///
		/// NONE(fighter)
		/// </summary>
		mark_dead,

		/// <summary>
		/// Pre-acceptation of death before kill_fighter, sole purpose is for
		/// death cancellation.
		///
		/// NONE(fighter), CANCELABLE
		/// </summary>
		acccept_death,

		/// <summary>
		/// When the fighter is killed off.
		/// remove_fighter is emitted right after this.
		///
		/// NONE(fighter)
		/// </summary>
		kill_fighter,

		/// <summary>
		/// When the fighter is revived.
		/// revive_fighter is emitted right after this.
		///
		/// NONE(fighter)
		/// </summary>
		revive_fighter,

	#endregion

	#region Handler Order

		/// <summary>
		/// On the first action of the round, before that action is started.
		///
		/// ROUNDEVENT(null)
		/// </summary>
		start_round,

		/// <summary>
		/// On the last action of the round, after that action has been started and acted.
		///
		/// ROUNDEVENT(null)
		/// </summary>
		end_round,

		/// <summary>
		/// When the action starts, before acting.
		///
		/// TURNEVENT(fighter), CANCELABLE
		/// </summary>
		start_turn,

		/// <summary>
		/// Same as start_turn, but only for the first action of that fighter in the round.
		/// It doesn't matter where in the action order it is, as long as the fighter hasn't
		/// had any action since the last round.
		///
		/// TURNEVENT(fighter), CANCELABLE
		/// </summary>
		start_turn_first,

		/// <summary>
		/// Same as start_turn, but only for the first action of that fighter in a
		/// consecutive group of turns of the same fighter.
		///
		/// Note 1: a single lonely action is still considered a group, and will emit start_turns.
		/// Note 2: Through Hold or other action order manipulations, the group can be broken up to have
		/// several start_turns event per round. This is a nice property that skillful players can take
		/// advantage of.
		///
		/// TURNEVENT(fighter)
		/// </summary>
		start_turns,

		/// <summary>
		/// The fighter is granted an action. (fires before the action is started)
		/// </summary>
		start_act,

		/// <summary>
		/// The fighter is granted an action, but only for the first time in the round.
		/// </summary>
		start_act_first,

		/// <summary>
		/// The fighter is granted an action, but only for the first time for a set of consecutive actions.
		/// </summary>
		start_acts,

		/// <summary>
		/// After the action has been acted, but before advancing to the next action.
		/// This will fire regardless of the fighter's ability to act or not.
		///
		/// TURNEVENT(fighter), CANCELABLE
		/// </summary>
		end_turn,

		/// <summary>
		/// Same as end_turn, but only for the last action of that fighter in a
		/// consecutive group of turns of the same fighter.
		///
		/// Note 1: a single lonely action is still considered a group, and will emit end_turns.
		/// Note 2: Through Hold or other action order manipulations, the group can be broken up to have
		/// several end_turns event per round. This is a nice property that skillful players can take
		/// advantage of.
		///
		///
		/// TURNEVENT(fighter)
		/// </summary>
		end_turns,

		/// <summary>
		/// After the fighter has picked an action and executed it.
		/// </summary>
		end_act,

		/// <summary>
		/// After the fighter has picked an action and executed it, but only for the last action of that fighter in the round.
		/// </summary>
		end_acts,

	#endregion


	#region States

		enable,

		enable_state,

		accept_decay,

		/// <summary>
		/// Internal state signal for when it decays.
		/// </summary>
		decay,

		/// <summary>
		/// Internal state signal for when it expires.
		///
		/// BUFFEVENT(state), CANCELABLE
		/// </summary>
		expire,

		/// <summary>
		/// Internal state signal for when it's consumed.
		///
		/// Consuming states is a mechanic for marks specifically.
		/// They can have different logic or requirements to consume,
		/// and some skills can downright force a mark to be consumed
		/// instantly.
		///
		/// BUFFEVENT(state), CANCELABLE
		/// </summary>
		consume,


		/// <summary>
		/// When a state is refreshed to max health.
		/// This happens when adding a state to a fighter which already has a state with the same ID.
		///
		/// BUFFEVENT(state)
		/// </summary>
		refresh_buff,

		/// <summary>
		/// When a state is added.
		///
		/// BUFFEVENT(state)
		/// </summary>
		gain_buff,

		/// <summary>
		/// When a state starts the expiration process.
		///
		/// BUFFEVENT(state), CANCELABLE
		/// </summary>
		accept_expire,

		/// <summary>
		/// When a state starts the expiration process from being consumed.
		///
		/// BUFFEVENT(state), CANCELABLE
		/// </summary>
		accept_consume,

		/// <summary>
		/// When a state is actually expiring.
		///
		/// BUFFEVENT(state)
		/// </summary>
		expire_buff,

		/// <summary>
		/// When a state is actually being consumed.
		///
		/// BUFFEVENT(state)
		/// </summary>
		consume_buff,

		/// <summary>
		/// When a state is removed from a fighter.
		///
		/// BUFFEVENT(fighter)
		/// </summary>
		lose_buff,

		/// <summary>
		/// When a state reaches max stack.
		///
		/// BUFFEVENT(state)
		/// </summary>
		maxed_stack,

	#endregion

	#region Fighter actions

		/// <summary>
		/// When a fighter accepts an intentional formation change of their own choosing.
		///
		/// FORMATIONEVENT(fighter), CANCELABLE
		/// </summary>
		accept_formation,

		/// <summary>
		/// When a fighter uses hold.
		///
		/// EVENT(fighter)
		/// </summary>
		use_hold,

		/// <summary>
		/// When a skill is used.
		///
		/// SKILLEVENT(fighter), CANCELABLE
		/// </summary>
		use_skill,

		/// <summary>
		/// When a fighter starts a skill.
		///
		/// SKILLEVENT(fighter)
		/// </summary>
		start_skill,


		/// <summary>
		/// When a fighter ends a skill.
		///
		/// SKILLEVENT(fighter)
		/// </summary>
		end_skill,

	#region Procs

		/// <summary>
		/// When a proc is accepted by the fighter for application on them.
		///
		/// PROCEVENT(fighter), CANCELABLE
		/// </summary>
		accept_proc,

		/// <summary>
		/// After a proc is applied on the fighter.
		///
		/// PROCEVENT(fighter), CANCELABLE
		/// </summary>
		receive_proc,

		/// <summary>
		/// When a proc is dealt by the fighter.
		///
		/// PROCEVENT(fighter), CANCELABLE
		/// </summary>
		deal_proc,

		/// <summary>
		/// When a proc effect is accepted by a fighter for application on them.
		/// Indeed, the battle system is a funny world where you can just say
		/// "I refuse to accept this damage!" to get get away scathe free!
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		accept_effect,

		/// <summary>
		/// When a proc effect is dealt by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		deal_effect,

		/// <summary>
		/// After a proc effect is accepted by a fighter for application on them.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		accepted_effect,

		/// <summary>
		/// After a proc effect is dealt by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		dealt_effect,

		/// <summary>
		/// When a damaging proc effect is accepted by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		accept_damage,

		/// <summary>
		/// After a damaging proc effect is accepted by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		accepted_damage,

		/// <summary>
		/// When a damaging proc effect is dealt by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		deal_damage,

		/// <summary>
		/// After a damaging proc effect is dealt by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		dealt_damage,

		/// <summary>
		/// When a damaging proc effect is accepted by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		accept_state,

		/// <summary>
		/// After a damaging proc effect is accepted by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		accepted_state,

		/// <summary>
		/// When a damaging proc effect is dealt by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		deal_state,

		/// <summary>
		/// After a damaging proc effect is dealt by a fighter.
		///
		/// PROCEFFECTEVENT(fighter), CANCELABLE
		/// </summary>
		dealt_state,

	#endregion

	#endregion
	}

	// extension methods for signals
}