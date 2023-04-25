using System;
using Anjin.Scripting;
using Combat.Toolkit;
using Data.Combat;
using JetBrains.Annotations;

namespace Combat.Data
{
	// TODO remove EvaluateEffects
	[LuaUserdata(Descendants = true)]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public abstract class ProcEffect : BattleResource
	{
		public ProcContext ctx;
		public Proc        proc;
		public Fighter     dealer;
		public Fighter     fighter;
		public Slot        slot;

		// Intrinsic Properties
		public float chance = 1;

		// Final computed values
		public virtual Elements Element  => Elements.none;
		public virtual Natures  Nature   => Element.GetNature();
		public virtual float    HPChange => 0;
		public virtual float    SPChange => 0;
		public virtual float    OPChange => 0;

		public virtual void BeforeApply() { }

		public ProcEffectFlags TryApplyFighter() => fighter != null ? ApplyFighter() : ProcEffectFlags.NoEffect;
		public ProcEffectFlags TryApplySlot()    => slot != null ? ApplySlot() : ProcEffectFlags.NoEffect;

		protected virtual ProcEffectFlags ApplyFighter() => ProcEffectFlags.MetaEffect;
		protected virtual ProcEffectFlags ApplySlot()    => ProcEffectFlags.MetaEffect;

		public virtual bool IsHurting => fighter != null && HPChange < 0;
		public virtual bool IsHealing => fighter != null && HPChange > 0;
		public virtual bool IsBuffing => false;

		public bool      IsPhysical => Nature == Natures.Physical;
		public bool      IsMagical  => Nature == Natures.Magical;
		public ProcStatus Status      => ctx.status;

		public override string ToString() => GetType().Name;

		[NotNull]
		public override string ID
		{
			get => $"{GetEnv().id}/{GetType().Name}";
			set { }
		}

	#region Battle Resource

		public override void AddTarget(Slot slot)
		{
			throw new NotImplementedException();
		}

		public override void AddTarget(Fighter fighter)
		{
			throw new NotImplementedException();
		}

	#endregion

		public void Chance(float chance)
		{
			this.chance = chance;
		}
	}
}

// public virtual Elements       ElementHint    { get; }
// public virtual ElementNatures ElementNatures { get; }
// public State    state;
// public Closure func;
// public Slot slot;
// public float      chance;
// public float      value;
// public ProcEffect potential;
// public bool critical;
// public string id;
// public Table filter;
// public TurnOperator.Runner operation;