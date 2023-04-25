using Anjin.Nanokin;
using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	public class ManagedMenu<TMenu> : CoroutineManaged
		where TMenu : StaticMenu<TMenu>
	{
		private bool _hasOpened = false;

		public override bool Active => _hasOpened && !StaticMenu<TMenu>.Exists;

		public override void OnStart()
		{
			base.OnStart();
			StaticMenu<TMenu>.EnableMenu();
		}

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			base.OnEnd(forceStopped, skipped);
			StaticMenu<TMenu>.DisableMenu();
		}

		public bool CanContinue(bool justYielded)
		{
			if (StaticMenu<TMenu>.Exists)
			{
				_hasOpened = true;
			}

			return base.CanContinue(justYielded);
		}
	}
}