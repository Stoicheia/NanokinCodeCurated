using Combat.Data;
using Util.Components;

namespace Util.Math.Splines
{
	public abstract class PlotShape : AnjinBehaviour
	{
		public abstract Plot Get(int index);
		public abstract Plot Get(int index, int max);
	}
}