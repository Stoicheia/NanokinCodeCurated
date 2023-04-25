using System;
using Anjin.Actors;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Skills.Generic
{
	public enum HitEffectOrigin
	{
		Feet,
		Center,
		// TODO we could add 'Anchor' which uses puppet nodes
	}

	public static class HitEffectOriginExtensions
	{
		public static Vector3 GetOriginPosition([NotNull] this GameObject go, [NotNull] GameObject goCconstraint)
		{
			bool hasActor      = go.TryGetComponent(out ActorBase actor);
			bool hasConstraint = goCconstraint.TryGetComponent(out FxInfo constraint);

			return GetOriginPositionFast(go, actor, constraint, hasActor, hasConstraint);
		}

		public static Vector3 GetOriginPositionFast(
			this GameObject go,
			ActorBase       actor,
			FxInfo          origin,
			bool            hasActor,
			bool            hasOrigin)
		{
			if (go == null) return Vector3.zero;
			if (!hasActor) return go.transform.position;

			HitEffectOrigin orig = hasOrigin
				? origin.Origin
				: HitEffectOrigin.Center;

			return GetOriginPosition(actor, orig);
		}

		private static Vector3 GetOriginPosition([NotNull] this ActorBase actor, HitEffectOrigin orig)
		{
			switch (orig)
			{
				case HitEffectOrigin.Feet:   return actor.transform.position;
				case HitEffectOrigin.Center: return actor.center;

				default: throw new ArgumentOutOfRangeException();
			}
		}
	}
}