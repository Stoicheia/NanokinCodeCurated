using System;
using Anjin.Util;
using Combat;
using Combat.Data;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ProcEffectEvent : ProcEvent
{
	public ProcEffect effect => (ProcEffect)noun;

	public ProcEffectEvent(Fighter dealer, ProcContext appl) : base(dealer, appl) { }

	public override bool physical  => effect.IsPhysical;
	public override bool magical   => effect.IsMagical;
	public override bool hurts     => effect.IsHurting;
	public override bool heals     => effect.IsHealing;
	public override bool states    => effect.IsBuffing;
	public override bool attacking => hurts && proc.dealer != null;

	public float    damage => hp;
	public float    value  => hp.Maximum(0);
	public float    hp     => effect.HPChange;
	public float    sp     => effect.SPChange;
	public float    op     => effect.OPChange;
	public Elements elem   => effect.Element;
	public float    chance => effect.chance;

	public override bool element(Elements elem) => effect.Element == elem;

	public override ProcEffect get(string name) => throw new InvalidOperationException();

	// public override bool tagged(string  tag)  => throw new InvalidOperationException();
	// public override bool tagged(Table   tags) => throw new InvalidOperationException();
	// public override bool has_tag(string tag)  => throw new InvalidOperationException();
	// public override bool has_tag(Table  tags) => throw new InvalidOperationException();
	// public override bool has_tags(Table tags) => throw new InvalidOperationException();

	public override void Reset()
	{
		base.Reset();
		noun = null;
	}

	/// <summary>
	/// This should not be called by the time proc effects are dispatching
	/// </summary>
	public new virtual void extend([NotNull] Table tbl) => throw new NotImplementedException();
}