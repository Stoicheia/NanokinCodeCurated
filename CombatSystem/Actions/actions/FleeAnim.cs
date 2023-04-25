using Combat.Startup;
using Cysharp.Threading.Tasks;

namespace Combat.Toolkit
{
	public class FleeAnim : BattleAnim
	{
		public override async UniTask RunAnimated()
		{
			runner.io.outcome = BattleOutcome.Flee;
			await GameEffects.FadeOut(0.5f).WithCancellation(cts.Token);
			runner.Submit(CoreOpcode.Stop);
		}
	}
}