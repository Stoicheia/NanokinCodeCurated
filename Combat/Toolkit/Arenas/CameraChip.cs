using Anjin.Nanokin;
using Anjin.Scripting;
using Combat.Components;
using Cysharp.Threading.Tasks;

namespace Combat
{
	public class CameraChip : Chip
	{
		protected override void RegisterHandlers()
		{
			base.RegisterHandlers();

			Handle(CoreOpcode.StartTurn, HandleStartTurn);
		}

		public override async UniTask InstallAsync()
		{
			await base.InstallAsync();
			await GameController.TillIntialized();
			//await Lua.initTask;

			runner.camera.SetBattle(runner);
		}

		public override void Uninstall()
		{
			base.Uninstall();

			runner.camera.SetBattle(null);
		}

		public void HandleStartTurn(ref CoreInstruction arg)
		{
			runner.camera.PlayState(ArenaCamera.States.idle);
		}
	}
}