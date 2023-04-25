using Anjin.Scripting;
using Combat.Features.TurnOrder.Sampling.Operations;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

[LuaUserdata(StaticName = "turnapi")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class TurnAPI
{
	public static TurnFunc new_op(Closure cl) => new TurnFunc(cl);
}