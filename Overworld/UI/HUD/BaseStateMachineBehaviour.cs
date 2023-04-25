using Anjin.Util;
using UnityEngine;
using UnityEngine.Animations;
using Vexe.Runtime.Extensions;

namespace Anjin.UI {
	public abstract class BaseStateMachineBehaviour : StateMachineBehaviour {

		private StateMachineLateUpdateDefferer _defferer;
		private Animator                       _animator;

		public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
		{
			_animator = animator;
		}

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			if (_defferer == null)
				_defferer = animator.GetOrAddComponent<StateMachineLateUpdateDefferer>();

			_defferer.Behaviours.AddIfNotExists(this);
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
		{
			if (!_defferer) return;
			_defferer.Behaviours.Remove(this);
		}

		public virtual void OnLateUpdate() {}

	}
}