using Anjin.Actors;
using Anjin.Util;
using Combat.Data;
using SaveFiles;
using UnityEngine;

namespace Combat
{
	public class Coach
	{
		/// <summary>
		/// The percent of HP under which the goof victory will play.
		/// </summary>
		protected const float GOOF_THRESHOLD = 0.1f;

		/// <summary>
		/// Percent under which the weak idle anim will play.
		/// </summary>
		protected const float WEAK_PERCENT = 0.4f;

		/// <summary>
		/// The coach's actor in the world.
		/// </summary>
		public CoachActor actor;

		/// <summary>
		/// The fighter that is being coached.
		/// </summary>
		public Fighter fighter;

		/// <summary>
		/// Home positioning for the coach.
		/// </summary>
		public Plot home;

		/// <summary>
		/// Character that this coach represents.
		/// </summary>
		public CharacterEntry character;

		public AnimID state => actor.state;

		public Coach(Fighter fighter)
		{
			this.fighter = fighter;
		}

		/// <summary>
		/// Teleport the coach to its home.
		/// </summary>
		public void Teleport(Vector3 pos)
		{
			if (actor != null)
			{
				actor.transform.position = pos;
			}
		}

		/// <summary>
		/// Teleport the coach to its home.
		/// </summary>
		public void Teleport(Plot plot)
		{
			if (actor != null)
			{
				actor.transform.position = (plot.position + Vector3.up * 0.1f).DropToGround();
				actor.facing             = plot.facing;

				if (actor.ScaleViaHeading)
				{
					Vector3 localScale = actor.transform.localScale;
					localScale.x = (actor.facing.z > 0 ? 1 : -1);

					actor.transform.localScale = localScale;
				}
			}
		}

		/// <summary>
		/// Teleport the coach to its home.
		/// </summary>
		public void TeleportHome()
		{
			if (actor != null)
			{
				actor.transform.position = (home.position + Vector3.up * 0.1f).DropToGround();
				actor.facing             = home.facing;
			}
		}

		public virtual void OnPointsChanged()
		{
			switch (state)
			{
				case AnimID.CombatIdle:
				case AnimID.CombatIdle2:
				case AnimID.CombatIdle3:
				case AnimID.CombatHurt:
				case AnimID.CombatHurt2:
				case AnimID.CombatHurt3:
					SetIdle();
					break;
			}
		}

		public virtual void OnDeath()
		{
			SetIdle();
		}

		public virtual void OnRevive()
		{
			SetIdle();
		}

		/// <summary>
		/// The the animation state for this coach.
		/// </summary>
		/// <param name="id"></param>
		public virtual void SetAnim(AnimID id)
		{
			if (actor != null)
			{
				actor.SetAnim(id);
			}
		}

		public virtual void SetIdle()
		{
			if (!fighter.deathMarked)
				SetAnim(AnimID.CombatIdle);
			else
				SetAnim(AnimID.CombatHurt);
		}

		/// <summary>
		/// Set the win state for this coach, using the basic logic for goof.
		/// </summary>
		public virtual void SetWin()
		{
			if (fighter.hp_percent < GOOF_THRESHOLD)
				SetAnim(AnimID.CombatWinGoof);
			else
				SetAnim(AnimID.CombatWin);
		}

		public virtual void SetTurn()
		{
			SetAnim(AnimID.CombatTurn);
		}

		public virtual void SetAction()
		{
			SetAnim(AnimID.CombatAction);
		}
	}
}