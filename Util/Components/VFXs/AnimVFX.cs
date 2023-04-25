using API.PropertySheet;

namespace Combat.Data.VFXs
{
	public class AnimVFX : VFX
	{
		public string          name;
		public PuppetAnimation puppetAnim;
		public string          startMarker;
		public string          endMarker;

		public AnimVFX(string name)
		{
			this.name = name;
		}

		public AnimVFX(PuppetAnimation puppetanim, string startMarker=null, string endMarker=null)
		{
			puppetAnim       = puppetanim;
			this.startMarker = startMarker;
			this.endMarker   = endMarker;
		}


		public override string          AnimSet              => name;
		public override PuppetAnimation PuppetSet            => puppetAnim;
		public override string          PuppetSetMarkerStart => startMarker;
		public override string          PuppetSetMarkerEnd   => endMarker;
	}
}