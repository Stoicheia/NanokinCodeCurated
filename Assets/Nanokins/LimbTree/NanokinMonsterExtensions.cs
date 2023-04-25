using Data.Nanokin;
using Util.Addressable;

namespace Assets.Nanokins
{
	public static class NanokinMonsterExtensions
	{
		public static NanokinLimbTree ToPuppetTree(this NanokinInstance nanokinInstance, AsyncHandles handles)
		{
			return NanokinLimbTree.WithAddressable(nanokinInstance[LimbType.Body]?.Asset,
				nanokinInstance[LimbType.Head]?.Asset,
				nanokinInstance[LimbType.Arm1]?.Asset,
				nanokinInstance[LimbType.Arm2]?.Asset,
				handles);
		}
	}
}