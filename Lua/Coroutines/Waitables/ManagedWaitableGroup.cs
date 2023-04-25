using System.Collections.Generic;
using System.Linq;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class ManagedWaitableGroup : CoroutineManaged
	{
		[ShowInInspector]
		private readonly List<ICoroutineWaitable> _waitables;

		public bool catchup;


		public ManagedWaitableGroup(List<ICoroutineWaitable> waitables)
		{
			_waitables = waitables?.ToList(); // we have to make a copy for the logic in CanContinue
		}

		public override bool Active => _waitables.Count != 0;

		public override bool Skippable
		{
			get
			{
				bool skippable = true;

				for (int i = 0; i < _waitables.Count; i++)
				{
					if (_waitables[i] is CoroutineManaged managed && managed.Skippable) continue;

					skippable = false;
					break;
				}

				return skippable;
			}
		}

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			for (int i = 0; i < _waitables.Count; i++)
			{
				if (_waitables[i] is CoroutineManaged managed && managed.Skippable)
				{
					managed.OnEnd(forceStopped, true);
				}
			}
		}

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			if (_waitables == null) return true;

			// We could do this differently, but there may be waitables currently that can flip/flip between continuable/not continuable
			// if they base their continuation off of other data in the game. Those waitables are designed with immediate discard in mind,
			// so we will try to recopy this here.
			for (var i = 0; i < _waitables.Count; i++)
			{
				ICoroutineWaitable w = _waitables[i];
				if (w == null || w.CanContinue(justYielded, catchup))
				{
					_waitables.RemoveAt(i--);
				}
			}

			return !Active;
		}
	}
}