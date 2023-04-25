using Anjin.Util;
using Data.Combat;

namespace Combat.Data
{
	public struct ModStat<T>
		where T : struct
	{
		/// <summary>
		/// Default to zero.
		/// </summary>
		public T flat;

		/// <summary>
		/// Default to one.
		/// </summary>
		public T scale;

		/// <summary>
		/// Default to one.
		/// </summary>
		public T? set;

		public ModStat(T flat, T scale)
		{
			this.flat  = flat;
			this.scale = scale;
			this.set   = default;
		}
	}

	public static class ModStatExt
	{
		public static ModStat<float>    identity      = new ModStat<float>(0, 1);
		public static ModStat<Elementf> identity_elem = new ModStat<Elementf>(Elementf.Zero, Elementf.One);
		public static ModStat<Pointf>   identity_pt   = new ModStat<Pointf>(Pointf.Zero, Pointf.One);
		public static ModStat<Statf>    identity_stat = new ModStat<Statf>(Statf.Zero, Statf.One);

		public static ModStat<float>    zero      = new ModStat<float>(0, 0);
		public static ModStat<Elementf> zero_elem = new ModStat<Elementf>(Elementf.Zero, Elementf.Zero);
		public static ModStat<Pointf>   zero_pt   = new ModStat<Pointf>(Pointf.Zero, Pointf.Zero);
		public static ModStat<Statf>    zero_stat = new ModStat<Statf>(Statf.Zero, Statf.Zero);

		public static ModStat<float> Add(ModStat<float> a, ModStat<float> b)
		{
			var ret = new ModStat<float>(a.flat + b.flat, a.scale + b.scale);

			if (a.set.HasValue && b.set.HasValue)
				ret.set = a.set.Value + b.set.Value;
			else if (a.set.HasValue)
				ret.set = a.set.Value;
			else if (b.set.HasValue)
				ret.set = b.set.Value;

			return ret;
		}

		public static ModStat<float> Add(ModStat<float> m1, ModStat<float> m2, ModStat<float> m3)                                                                             => Add(Add(m1, m2), m3);
		public static ModStat<float> Add(ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4)                                                          => Add(Add(m1, m2), Add(m3, m4));
		public static ModStat<float> Add(ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5)                                       => Add(Add(m1, m2), Add(m3, m4), m5);
		public static ModStat<float> Add(ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6)                    => Add(Add(m1, m2), Add(m3, m4), Add(m5, m6));
		public static ModStat<float> Add(ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6, ModStat<float> m7) => Add(Add(m1, m2), Add(m3, m4), Add(m5, m6), m7);

		public static ModStat<float> Add(ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6, ModStat<float> m7, ModStat<float> m8) =>
			Add(Add(m1, m2), Add(m3, m4), Add(m5, m6), Add(m7, m8));

		public static ModStat<float> Add(ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6, ModStat<float> m7, ModStat<float> m8, ModStat<float> m9) =>
			Add(Add(m1, m2), Add(m3, m4), Add(m5, m6), Add(m7, m8), m9);

		public static float Mod(this float a, ModStat<float> b)
		{
			float ret = (a + b.flat) * b.scale.Minimum(0);
			if (b.set.HasValue)
				ret = b.set.Value;
			return ret;
		}


		public static Pointf Mod(this Pointf a, ModStat<Pointf> b)
		{
			var ret = (a + b.flat) * b.scale.Min(0);
			if (b.set.HasValue)
				ret = b.set.Value;
			return ret;
		}

		public static Statf Mod(this Statf a, ModStat<Statf> b)
		{
			var ret = (a + b.flat) * b.scale.Min(0);
			if (b.set.HasValue)
				ret = b.set.Value;
			return ret;
		}

		public static Elementf Mod(this Elementf a, ModStat<Elementf> b)
		{
			var ret = (a + b.flat) * b.scale.Min(0);
			if (b.set.HasValue)
				ret = b.set.Value;
			return ret;
		}

		public static Elements Mod(this Elements a, Elements? b)
		{
			return b ?? a; // Replace
		}

		public static Natures Mod(this Natures a, Natures b)
		{
			return b; // Replace
		}

		/// <summary>
		/// Apply a ModStat with the assumption that scale is normalized to zero for x1
		///
		/// -1.0 = x0
		/// -0.5 = x0.5
		///  0.0 = x1
		///  0.5 = x1.5
		///  1.0 = x2
		///  1.5 = x2.5
		///  2.0 = x3
		///
		/// </summary>
		public static float Mod0(this float a, ModStat<float> b)
		{
			float ret = (a + b.flat) * (1 + b.scale);
			if (b.set.HasValue)
				ret = b.set.Value;
			return ret;
		}

		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2)                                                                                                                   => a.Mod0(Add(m1, m2));
		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2, ModStat<float> m3)                                                                                                => a.Mod0(Add(m1, m2, m3));
		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4)                                                                             => a.Mod0(Add(m1, m2, m3, m4));
		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5)                                                          => a.Mod0(Add(m1, m2, m3, m4, m5));
		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6)                                       => a.Mod0(Add(m1, m2, m3, m4, m5, m6));
		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6, ModStat<float> m7)                    => a.Mod0(Add(m1, m2, m3, m4, m5, m6, m7));
		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6, ModStat<float> m7, ModStat<float> m8) => a.Mod0(Add(m1, m2, m3, m4, m5, m6, m7, m8));

		public static float Mod0(this float a, ModStat<float> m1, ModStat<float> m2, ModStat<float> m3, ModStat<float> m4, ModStat<float> m5, ModStat<float> m6, ModStat<float> m7, ModStat<float> m8, ModStat<float> m9) =>
			a.Mod0(Add(m1, m2, m3, m4, m5, m6, m7, m8, m9));
	}
}