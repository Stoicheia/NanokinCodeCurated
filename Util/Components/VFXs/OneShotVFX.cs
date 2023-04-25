using Combat.Data.VFXs;

namespace Combat.Toolkit
{
	public abstract class OneShotVFX : VFX
	{
		private bool _hasEnded;

		public override bool IsActive => !_hasEnded;

		internal sealed override void Leave() { }

		protected void OnEnded()
		{
			_hasEnded = true;
		}
	}
}