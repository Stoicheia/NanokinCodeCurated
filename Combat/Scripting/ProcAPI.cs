using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Combat;
using Combat.Data;
using Combat.Features.TurnOrder.Sampling.Operations;
using Combat.StandardResources;
using Combat.Toolkit;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using Vexe.Runtime.Extensions;

/// <summary>
/// Methods to construct several battle types, intended
/// for use with Moonsharp.
/// </summary>
[LuaUserdata(StaticName = "procapi")]
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class ProcAPI
{
	[NotNull] public static AutoAnimProc     autoanim(Closure                 logic)               => new AutoAnimProc(logic);
	[NotNull] public static AnimProc         pushanim(string                  name, Closure logic) => new AnimProc(name, logic);
	[NotNull] public static AnimProc         pushanim(Closure                 logic)    => new AnimProc(logic);
	[NotNull] public static List<ProcEffect> pushanim([NotNull] List<Closure> closures) => closures.Select(c => new AnimProc(c)).ToList<ProcEffect>();

	// Formulated values
	// ----------------------------------------
	[NotNull] public static StandardHurt hurt(int raw_hp, Elements elem = Elements.none) => new StandardHurt(elem, raw_hp);

	[CanBeNull] public static StandardDamage damage(DynValue dv, int power, float variance = -1f)
	{
		if (dv.AsEnum(out Elements elem))
			return new StandardDamage(elem, power, variance);
		else if (dv.AsTable(out Table conf))
			return new StandardDamage(conf);

		Debug.LogError("Bad arg #1 to damage(...), elem or table expected.");
		return null;
	}

	// Absolute values
	// ----------------------------------------
	[NotNull] public static HurtHP  hurt_hp(int  v, Elements elem = Elements.none) => new HurtHP(v, elem);
	[NotNull] public static HealHP  heal_hp(int  v) => new HealHP(v);
	[NotNull] public static DrainHP drain_hp(int v) => new DrainHP(v);

	[NotNull] public static HurtSP  hurt_sp(int  v) => new HurtSP(v);
	[NotNull] public static HealSP  heal_sp(int  v) => new HealSP(v);
	[NotNull] public static DrainSP drain_sp(int v) => new DrainSP(v);

	[NotNull] public static HurtOP  hurt_op(float  v) => new HurtOP(v);
	[NotNull] public static HealOP  heal_op(float  v) => new HealOP(v);
	[NotNull] public static DrainOP drain_op(float v) => new DrainOP(v);

	// Percent values
	// ----------------------------------------
	[NotNull] public static HurtHPByPercent  hurtp_hp(float  v, Elements elem = Elements.none) => new HurtHPByPercent(v, elem);
	[NotNull] public static HealHPByPercent  healp_hp(float  v) => new HealHPByPercent(v);
	[NotNull] public static DrainHPByPercent drainp_hp(float v) => new DrainHPByPercent(v);

	[NotNull] public static HurtSPByPercent  hurtp_sp(float  v) => new HurtSPByPercent(v);
	[NotNull] public static HealSPByPercent  healp_sp(float  v) => new HealSPByPercent(v);
	[NotNull] public static DrainSPByPercent drainp_sp(float v) => new DrainSPByPercent(v);

	[NotNull] public static HurtOPByPercent  hurtp_op(float  v) => new HurtOPByPercent(v);
	[NotNull] public static HealOPByPercent  healp_op(float  v) => new HealOPByPercent(v);
	[NotNull] public static DrainOPByPercent drainp_op(float v) => new DrainOPByPercent(v);

	// States
	// ----------------------------------------
	[NotNull] public static AddState add_state(DynValue dv)
	{
		if (dv.AsObject(out State state))
		{
			return new AddState(state);
		}
		else if (dv.AsFunction(out Closure func))
		{
			return new AddState(func);
		}

		throw new ArgumentException($"Invalid argument to {nameof(add_state)}");
	}

	[NotNull] public static RemoveState rem_state(DynValue dv, int count = -1)
	{
		if (dv.IsObjectNull() || dv.IsNil())
		{
			return new RemoveState("", 1);
		}

		if (dv.AsTable(out Table tbl))
		{
			return new RemoveState(tbl, count);
		}
		else if (dv.AsString(out string id))
		{
			return new RemoveState(id, count);
		}

		if (dv.AsObject(out State state))
		{
			return new RemoveState(state);
		}

		throw new ArgumentException($"Invalid argument to {nameof(rem_state)}");
	}

	[NotNull] public static RefreshStates refresh_state(string id) => new RefreshStates(id);
	[NotNull] public static ConsumeMarks  consume_marks(string id) => new ConsumeMarks(id);

	// Other
	// ----------------------------------------
	[CanBeNull] public static SwapHome swap(DynValue dv, MoveSemantic semantic = MoveSemantic.Auto)
	{
		if (dv.AsUserdata(out Slot slot))
			return new SwapHome(slot, semantic);
		else if (dv.AsFunction(out Closure closure))
			return new SwapHome(closure, semantic);

		throw new ArgumentException();
	}

	[CanBeNull]
	public static ChanceEffect chance(DynValue dchance, DynValue deffect)
	{
		while (deffect.Type == DataType.Function)
		{
			deffect = Lua.Invoke(deffect.Function);
		}

		if (dchance.AsFloat(out float chance))
		{
			if (deffect.AsUserdata(out ProcEffect peff)) return new ChanceEffect(chance, peff);
			if (deffect.AsUserdata(out State state)) return new ChanceEffect(chance, state);
			if (deffect.AsUserdata(out TurnFunc qturn)) return new ChanceEffect(chance, qturn);
			if (deffect.IsNil()) return null;
		}

		throw new ArgumentException("Bad argument to ProcAPI.chance");
	}

	public static TurnFuncEffect qturn_eff(TurnFunc func)
	{
		return new TurnFuncEffect(func);
	}

	[CanBeNull]
	public static KillEffect kill(DynValue tag)
	{
		if (tag.AsString(out string str))
			return new KillEffect(str);
		else if (tag.AsFunction(out Closure closure))
			return new KillEffect(closure);
		else if (tag.AsTable(out Table tags))
			return new KillEffect(tags);
		else if (tag.IsNil())
			return new KillEffect();

		throw new ArgumentException("Bad argument to ProcAPI.kill");
	}

	// [NotNull]
	// public static ManualDeathTrigger trigger_deaths()
	// {
	// 	return new ManualDeathTrigger();
	// 	//throw new ArgumentException("Bad argument to ProcAPI.chance");
	// }

	// [NotNull]
	// public static ManualReviveTrigger trigger_revives()
	// {
	// 	return new ManualReviveTrigger();
	// 	//throw new ArgumentException("Bad argument to ProcAPI.chance");
	// }
}