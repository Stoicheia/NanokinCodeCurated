using System;
using DG.Tweening;
using UnityEngine;
using Util.UniTween.Value;

namespace Anjin.Actors.States
{
	/// <summary>
	/// A state which uses DOTween to move the actor to a goal supplied externally by the
	/// user of this state.
	/// </summary>
	[Serializable]
	public class TweenState : StateKCC
	{
		[SerializeField] public EaserTo tweener;
		[SerializeField] public bool    disableCollisions = true;

		private TweenableVector3 _currentPosition;
		private Tween            _currentTween;

		public Vector3 Goal { get; set; }

		public bool HasEnded => _currentTween == null || !_currentTween.active;

		public override void OnActivate()
		{
			base.OnActivate();

			_currentPosition       = _currentPosition ?? new TweenableVector3();
			_currentPosition.value = actor.Position;

			_currentTween = _currentPosition.To(Goal, tweener);

			if (disableCollisions) actor.Motor.CollidableLayers = new LayerMask();
		}

		public override void OnDeactivate()
		{
			base.OnDeactivate();

			if (disableCollisions) actor.Motor.CollidableLayers = actor.CollisionMask;
		}

		public override void AfterCharacterUpdate(float dt)
		{
			base.AfterCharacterUpdate(dt);

			if (active)
			{
				actor.Motor.SetPosition(_currentPosition);
			}
		}
	}
}