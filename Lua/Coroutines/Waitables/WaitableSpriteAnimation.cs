using API.Spritesheet.Indexing.Runtime;
using Util.Odin.Attributes;

namespace Anjin.Scripting.Waitables
{
	/// <summary>
	/// A coroutine waitable to wait for a SpriteAnimator to finish
	/// playing a specific animation.
	/// </summary>
	[LuaUserdata]
	public class WaitableSpriteAnimation : ICoroutineWaitable
	{
		[ShowInPlay]
		private readonly SpriteAnim _animator;

		[ShowInPlay]
		private readonly string     _animation;

		[ShowInPlay]
		private readonly int?       _bindingID;

		[ShowInPlay]
		private bool _hasCompleted;

		[ShowInPlay]
		private bool _hasStarted;

		public WaitableSpriteAnimation(SpriteAnim animator, string animation)
		{
			_animator  = animator;
			_animation = animation;
			_bindingID = null;

			animator.onCompleted += OnAnimatorOnCompleted;
		}

		public WaitableSpriteAnimation(SpriteAnim animator, int bindingID)
		{
			_animator  = animator;
			_animation = null;
			_bindingID = bindingID;

			animator.onCompleted += OnAnimatorOnCompleted;
		}

		private void OnAnimatorOnCompleted()
		{
			_hasCompleted         =  true;
			_animator.onCompleted -= OnAnimatorOnCompleted;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			if (_animator == null || _animator.player == null) return true;

			if (!_hasStarted)
			{
				if ((_animation != null && _animator.player.Animation == _animation) || (_bindingID != null && _animator.player.AnimationID == _bindingID))
				{
					_hasStarted = true;
				}

				return false;
			}

			return _hasCompleted;
		}
	}
}