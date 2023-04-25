using Combat;
using Combat.Data;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ProcEvent : TriggerEvent
{
	public ProcContext context;
	public bool        missed = false;

	public ProcEvent(Fighter dealer, ProcContext appl)
	{
		me         = dealer;
		protector  = null;
		cancelable = true;
		context    = appl;
	}

	public void redirect(Fighter redirect)
	{
		protector = redirect;
	}

	public override void Reset()
	{
		base.Reset();
		context   = null;
		protector = null;
		missed    = false;
	}

	/// <summary>
	/// Modify the proc to be a miss. (displays MISS! instead of elemental damage)
	/// </summary>
	public bool miss()
	{
		return (missed == true);
	}

	/// <summary>
	/// Modify the proc to be a miss if b is true.
	/// </summary>
	public bool miss(bool b)
	{
		return missed == (missed || b);
	}

	public Fighter protector { get; private set; }

	/// <summary>
	/// Proc itself.
	/// </summary>
	public Proc proc => context.proc;

	/// <summary>
	/// ID of the proc.
	/// </summary>
	public string id => context.proc.ID;

	/// <summary>
	/// Dealer of the proc.
	/// </summary>
	[CanBeNull]
	public Fighter dealer => context.proc.dealer;

	[CanBeNull]
	public bool dealt => context.proc.dealer != null;

	/// <summary>
	/// Victim of the proc.
	/// </summary>
	public object victim => context.victim;

	/// <summary>
	/// Whether or not the proc is physical.
	/// </summary>
	public virtual bool physical => context.physical;

	/// <summary>
	/// Whether or not the proc is magical.
	/// </summary>
	public virtual bool magical => context.magical;

	/// <summary>
	/// Whether or not the proc hurts/damages.
	/// </summary>
	public virtual bool hurts => context.hurts;

	/// <summary>
	/// Whether or not the proc is healing.
	/// </summary>
	public virtual bool heals => context.heals;

	/// <summary>
	/// Whether or not the proc is an attack. (hurts victim and has a dealer)
	/// </summary>
	public virtual bool attacking => context.attacking;

	public virtual bool states
	{
		get { throw new System.NotImplementedException(); }
	}

	/// <summary>
	/// Check if the proc matches the element.
	/// </summary>
	public virtual bool element(Elements elem) => context.element(elem);

	/// <summary>
	/// Check if the proc has the specified effect type.
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public virtual bool has(string name) => context.has(name);

	/// <summary>
	/// Get an effect by name.
	/// </summary>
	[CanBeNull] public virtual ProcEffect get(string name) => context.get(name);

	/// <summary>
	/// Check if the proc has all the tags.
	/// </summary>
	public virtual bool has_tag([NotNull] Table tags) => context.has_tag(tags);

	/// <summary>
	/// Check if the proc has all the tags.
	/// </summary>
	public virtual bool has_tags([NotNull] Table tags) => context.has_tags(tags);

	/// <summary>
	/// Check if the proc has a tag.
	/// </summary>
	public bool has_tag(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null)
		=> context.has_tag(t1, t2, t3, t4, t5, t6);

	/// <summary>
	/// Check if the proc has all the tags.
	/// </summary>
	public bool has_tags(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null)
		=> context.has_tags(t1, t2, t3, t4, t5, t6);


	[NotNull] public ProcEvent effects(string pattern) => this;

	public virtual void extend([NotNull] Table tbl)           { context.extend(tbl); }
	public virtual void set(ProcStat           stat, float v) { context.set(stat, v); }
	public virtual void up(ProcStat            stat, float v) { context.up(stat, v); }
	public virtual void down(ProcStat          stat, float v) { context.down(stat, v); }
	public virtual void scale(ProcStat         stat, float v) { context.scale(stat, v); }
}