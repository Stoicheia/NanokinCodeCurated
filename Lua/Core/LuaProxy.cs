// ReSharper disable InconsistentNaming

using Combat.Proxies;
using JetBrains.Annotations;

namespace Anjin.Scripting
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public abstract class LuaProxy<T>
	{
		protected T proxy;

		public LuaProxy<T> Set(T t)
		{
			proxy = t;
			return this;
		}
	}
}