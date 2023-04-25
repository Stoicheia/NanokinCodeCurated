using System;
using Anjin.Scripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data.Combat
{
	[LuaUserdata]
	[Serializable]
	public struct Statf
	{
		public float power;
		[FormerlySerializedAs("willpower")]
		public float will;
		public float speed;
		public float ap;

		public Statf(float power = 0,
			float          will  = 0,
			float          speed = 0,
			float          ap    = 0
		)
		{
			this.power = power;
			this.will  = will;
			this.speed = speed;
			this.ap    = ap;
		}

		public static Statf Zero => new Statf();
		public static Statf One  => Zero.Set(1);

		public static Statf operator -(Statf v1, float v2) =>
			new Statf
			{
				power = v1.power - v2,
				speed = v1.speed - v2,
				will  = v1.will - v2,
				ap    = v1.ap - v2
			};

		public static Statf operator +(Statf v1, float v2) =>
			new Statf
			{
				power = v1.power + v2,
				speed = v1.speed + v2,
				will  = v1.will + v2,
				ap    = v1.ap + v2
			};

		public static implicit operator Statf(float v2) =>
			new Statf
			{
				power = v2,
				speed = v2,
				will  = v2,
				ap    = v2
			};


		public static Statf operator +(Statf v1, Statf v2) =>
			new Statf
			{
				power = v1.power + v2.power,
				speed = v1.speed + v2.speed,
				will  = v1.will + v2.will,
				ap    = v1.ap + v2.ap
			};

		public static Statf operator *(Statf v1, Statf v2) =>
			new Statf
			{
				power = v1.power * v2.power,
				speed = v1.speed * v2.speed,
				will  = v1.will * v2.will,
				ap    = v1.ap * v2.ap,
			};

		public static Statf operator *(Statf v1, float mul) =>
			new Statf
			{
				power = v1.power * mul,
				speed = v1.speed * mul,
				will  = v1.will * mul,
				ap    = v1.ap * mul,
			};

		public static Statf operator -(Statf v1) =>
			new Statf
			{
				power = -v1.power,
				speed = -v1.speed,
				will  = -v1.will,
				ap    = -v1.ap,
			};


		private Statf Set(int v)
		{
			power = v;
			speed = v;
			will  = v;
			ap    = v;

			return this;
		}

		/// <summary>
		/// Prints the stats in a legible format: Statf(# pow, # spd, # wil, # ap)
		/// E.g.: Statf(30 pow, 11 spd, 24 wil, 2 ap)
		/// </summary>
		public override string ToString() => $"({power} pow, {speed} spe, {will} wil, {ap} ap)";

		public static Statf Scale(float power = 1, float willpower = 1, float speed = 1, float ap = 0) =>
			new Statf
			{
				power = power,
				will  = willpower,
				speed = speed,
				ap    = ap,
			};

		public Statf Min(float min)
		{
			return new Statf
			{
				power = Mathf.Max(min, power),
				speed = Mathf.Max(min, speed),
				will  = Mathf.Max(min, will),
				ap    = Mathf.Max(min, ap),
			};
		}
	}
}