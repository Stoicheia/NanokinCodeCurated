using System;
using Anjin.Scripting;
using Anjin.Util;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Data.Combat
{
	[LuaUserdata]
	[Serializable, JsonObject(MemberSerialization.Fields)]
	public struct Pointf
	{
		[SerializeField, LabelText("HP")]
		public float hp;

		[SerializeField, LabelText("SP")]
		public float sp;

		[SerializeField, LabelText("OP")]
		public float op;

		public Pointf(
			float hp = 0,
			float sp = 0,
			float op = 0
		)
		{
			this.hp = hp;
			this.sp = sp;
			this.op = op;
		}

		public static Pointf Zero => new Pointf();
		public static Pointf One  => new Pointf(1, 1, 1);

		public static Pointf operator +(Pointf p1, float p2)
		{
			return new Pointf
			{
				hp = p1.hp + p2,
				sp = p1.sp + p2,
				op = p1.op + p2
			};
		}

		public static Pointf operator -(Pointf p1, float p2)
		{
			return new Pointf
			{
				hp = p1.hp - p2,
				sp = p1.sp - p2,
				op = p1.op - p2
			};
		}

		// public static implicit operator Pointf(float v)
		// {
		// 	return new Pointf
		// 	{
		// 		hp = v,
		// 		sp = v,
		// 		op = v,
		// 	};
		// }

		public static Pointf operator +(Pointf p1, Pointf p2)
		{
			return new Pointf
			{
				hp = p1.hp + p2.hp,
				sp = p1.sp + p2.sp,
				op = p1.op + p2.op
			};
		}

		public static Pointf operator -(Pointf pts)
		{
			return new Pointf
			{
				hp = -pts.hp,
				sp = -pts.sp,
				op = -pts.op
			};
		}

		public static Pointf operator *(Pointf p1, float mul)
		{
			return new Pointf
			{
				hp = p1.hp * mul,
				sp = p1.sp * mul,
				op = p1.op * mul
			};
		}

		public static Pointf operator *(Pointf p1, Pointf p2)
		{
			return new Pointf
			{
				hp = p1.hp * p2.hp,
				sp = p1.sp * p2.sp,
				op = p1.op * p2.op
			};
		}

		public static Pointf operator /(Pointf p1, Pointf p2)
		{
			return new Pointf
			{
				hp = p1.hp / p2.hp,
				sp = p1.sp / p2.sp,
				op = p1.op / p2.op
			};
		}

		public Pointf Floored()
		{
			hp = Mathf.FloorToInt(hp);
			sp = Mathf.FloorToInt(sp);
			op = Mathf.FloorToInt(op);
			return this;
		}

		public Pointf Max(Pointf max)
		{
			this.hp = Mathf.Clamp(this.hp, 0, max.hp);
			this.sp = Mathf.Clamp(this.sp, 0, max.sp);
			this.op = Mathf.Clamp(this.op, 0, max.op);

			return this;
		}

		public override string ToString()
		{
			return $"({hp} hp, {sp} sp)";
		}

		public string ToString(Pointf max)
		{
			return $"({hp}/{max.hp} hp — {sp}/{max.sp} sp)";
		}

		public Pointf Min(int min)
		{
			return new Pointf
			{
				hp = hp.Minimum(min),
				sp = sp.Minimum(min),
				op = op.Minimum(min),
			};
		}
	}
}