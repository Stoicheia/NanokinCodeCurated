using System.Collections.Generic;
using System.Linq;
using Anjin.Cameras;
using Anjin.Util;
using Combat;
using UnityEngine;

namespace Util
{
	public static class MathUtil
	{
		public const float PIXEL_TO_WORLD = 1 / 32f;
		public const float WORLD_TO_PIXEL = 32;

		public static readonly Vector2 YDOWN_TO_UP = new Vector2(1, -1);
		public static readonly Vector2 HALF_PIXEL  = 0.5f * PIXEL_TO_WORLD * Vector2.one;
		public static readonly Vector2 DOWN_RIGHT  = Vector2.down + Vector2.right;


		/// <summary>
		/// Normalized tunable sigmoid function
		/// https://dhemery.github.io/DHE-Modules/technical/sigmoid/#sigmoid
		/// </summary>
		public static float tsigmoid(float x, float k = 0.3f) => (x - k * x) / (k - 2 * k * Mathf.Abs(x) + 1);

		public static float scurve(float x, float k = 0.3f) => (1 + tsigmoid(2 * x - 1, -k)) / 2f;

		public static float jcurve(float x, float k = 0.3f) => tsigmoid(x, k);

		public static float rcurve(float x, float k = 0.3f) => tsigmoid(x, -k);

		public static void Wrap(ref int value, int min, int max)
		{
			value = Wrap(value, min, max);
		}

		public static void Wrap(ref int value, int max)
		{
			value = Wrap(value, max);
		}

		public static int Wrap(int value, int min, int max)
		{
			if (value < min)
			{
				return max;
			}
			else if (value > max)
			{
				return min;
			}

			return value;
		}

		public static int Wrap(int value, int max)
		{
			return Wrap(value, 0, max);
		}

		public static float Angle(Vector2 vec)
		{
			if (vec.x < 0)
			{
				return 360 - Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg * -1;
			}
			else
			{
				return Mathf.Atan2(vec.x, vec.y) * Mathf.Rad2Deg;
			}
		}

		public static Vector2 RadianToVector2(float radian)
		{
			return new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
		}

		public static Vector2 DegreeToVector2(float degree)
		{
			return RadianToVector2(degree * Mathf.Deg2Rad);
		}

		public static Vector3 DegreeToVector3XZ(float degree)
		{
			Vector2 vec = RadianToVector2(degree * Mathf.Deg2Rad);
			return new Vector3(vec.x, 0, vec.y);
		}

		public static Vector2 AnglePosition(float angle, float distance)
		{
			float rad = angle * Mathf.Deg2Rad;

			return new Vector2(distance * Mathf.Sin(rad), distance * Mathf.Cos(rad));
		}

		public static Vector2 AnglePosition(Vector2 start, float angle, float distance)
		{
			return start + AnglePosition(angle, distance);
		}

		/// <summary>
		/// Clamp a reference value between a min and max
		/// </summary>
		/// <param name="value"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static void Clamp(ref float value, float min, float max)
		{
			if (value < min) value      = min;
			else if (value > max) value = max;
		}

		/// <summary>
		/// Clamp a reference value between a min and max
		/// </summary>
		/// <param name="value"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public static void Clamp(ref int value, int min, int max)
		{
			if (value < min) value      = min;
			else if (value > max) value = max;
		}

		/// <summary>
		/// Snaps a vector (x, z) to n axis
		/// </summary>
		/// <param name="v">The Vector</param>
		/// <param name="n">The number of axis, default is 8</param>
		/// <returns>The snapped vector</returns>
		public static Vector3 RoundVector(Vector3 v, int n = 8)
		{
			float cuts  = 360f / n;
			float theta = Mathf.Atan2(v.z, v.x) * Mathf.Rad2Deg;
			theta =  Mathf.Round(theta / cuts) * cuts;
			theta *= Mathf.Deg2Rad;
			return new Vector3(Mathf.Cos(theta), v.y, Mathf.Sin(theta));
		}

		public static float LerpDamp(float value, float target, float damping, float timescale = 1)
		{
			float d = damping / Mathf.Clamp(timescale, 0.00001f, 100);
			if (d <= 0.001f) return target;
			return (value * (d - 1) + target) / d;
		}

		public static Vector3 Centroid(params Vector3[] v)
		{
			return Centroid(v.AsEnumerable());
		}

		public static Vector3 Centroid(IEnumerable<Vector3> v)
		{
			Centroid calc = new Centroid();

			foreach (Vector3 vector3 in v)
			{
				calc.add(vector3);
			}

			return calc.get();
		}

		/// <summary>
		/// Allows combining a list of rectangles into one. (gets a rectangle that perfectly encompasses every rectangle)
		/// </summary>
		public static Rect Union(IList<Rect> rects)
		{
			if (rects.Count == 0) return default;
			if (rects.Count == 1) return rects[0];

			Rect ret = rects[0];
			for (int i = 1; i < rects.Count; i++)
			{
				ret = ret.Union(rects[i]);
			}

			return ret;
		}

		public static float DistanceToSphere(Vector3 pos, Vector3 spherePos, float sphereRadius)
		{
			return Mathf.Max(0, Vector3.Distance(pos, spherePos) - sphereRadius);
		}

		/// <summary>
		/// Get the distance from a point to a line defined by (lineStart, lineEnd).
		/// in this case, we can thank google for saving us from unemployment.
		/// </summary>
		/// <param name="pos">The point to compare to the line</param>
		/// <param name="lineStart">Start of the line</param>
		/// <param name="lineEnd">end of the line</param>
		/// <returns>The distance</returns>
		public static float DistanceToLine(Vector3 pos, Vector3 lineStart, Vector3 lineEnd)
		{
			Vector3 ba = pos - lineStart;
			Vector3 ca = lineEnd - lineStart;

			float   len      = ca.magnitude;
			Vector3 caScaled = ca;
			if (len > 0.000001f)
				caScaled /= len;

			float   dot  = Mathf.Clamp(Vector3.Dot(caScaled, ba), 0, len);
			Vector3 proj = lineStart + caScaled * dot;

			return (proj - pos).magnitude;
		}

		/// <summary>
		/// Get the distance from a point to a ray.
		/// </summary>
		public static float DistanceToRay(Vector3 pos, Ray ray)
		{
			return Vector3.Cross(ray.direction, pos - ray.origin).magnitude;
		}

	#region Lerp with Sharpness

		private static float GetLerpSharpnessOverTime(float sharpness, float deltaTime)
		{
			return 1f - Mathf.Exp(-Mathf.Max(sharpness, 0.01f) * deltaTime);
		}

		public static Vector3 LerpWithSharpness(Vector3 current, Vector3 direction, float speed, float sharpness, float deltaTime)
		{
			return Vector3.Lerp(current, direction * speed, 1f - Mathf.Exp(-sharpness * deltaTime));
		}

		public static void LerpWithSharpness(ref Vector3 current, Vector3 next, float sharpness, float deltaTime)
		{
			current = Vector3.Lerp(current, next, GetLerpSharpnessOverTime(sharpness, deltaTime));
		}

		public static void SlerpWithSharpness(ref Vector3 current, Vector3 direction, float speed, float sharpness, float deltaTime)
		{
			current = Vector3.Slerp(current, direction * speed, 1f - Mathf.Exp(-sharpness * deltaTime));
		}

		public static void SlerpWithSharpness(ref Vector3 current, Vector3 next, float sharpness, float deltaTime)
		{
			current = Vector3.Slerp(current, next, GetLerpSharpnessOverTime(sharpness, deltaTime));
		}

		public static Vector3 LerpWithSharpness(Vector3 current, Vector3 next, float sharpness, float deltaTime)
		{
			return Vector3.Lerp(current, next, GetLerpSharpnessOverTime(sharpness, deltaTime));
		}

		public static Vector3 SlerpWithSharpness(Vector3 current, Vector3 direction, float speed, float sharpness, float deltaTime)
		{
			return Vector3.Slerp(current, direction * speed, 1f - Mathf.Exp(-sharpness * deltaTime));
		}

		public static Vector3 SlerpWithSharpness(Vector3 current, Vector3 next, float sharpness, float deltaTime)
		{
			return Vector3.Slerp(current, next, GetLerpSharpnessOverTime(sharpness, deltaTime));
		}

		public static void LerpWithSharpness(ref float a, float b, float sharpness, float deltaTime)
		{
			a = Mathf.Lerp(a, b, GetLerpSharpnessOverTime(sharpness, deltaTime));
		}

	#endregion

		public static float CalculateJumpForce(float jumpHeight, float gravity)
		{
			// Note: This formula was often used to call this function: (GravityForce * GravityDirection.normalized).magnitude
			//       This is unnecessary when the direction is separate from the force.
			//       Things will be a little more intuitive if we assume that GravityDirection is a clean normalized vector
			//       and only encodes a direction, as its name implies.

			return Mathf.Sqrt(2 * jumpHeight * gravity / Time.fixedDeltaTime);
		}

		public static float CalculateForwardJump(float height, float gravityForce)
		{
			return Mathf.Sqrt(-Mathf.Abs(gravityForce) * height / -0.5f) / 2;
		}

		public static Vector3 GetPlanarCameraDirection(Quaternion cameraQuaternion, Vector3 up)
		{
			// Transform the direction to be planar with the camera direction
			Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(cameraQuaternion * Vector3.forward, up);

			// If our direction's magnitude is somehow zero, make sure to account for that.
			if (Mathf.Abs(cameraPlanarDirection.sqrMagnitude) < Mathf.Epsilon)
				cameraPlanarDirection = Vector3.ProjectOnPlane(cameraQuaternion * Vector3.up, up);

			return cameraPlanarDirection.normalized;
		}

	#region Sprite Direction Snapping

		public static float ToWorldAzimuthBlendable(Vector3 facing, int nDirections = 8)
		{
			return ToWorldAzimuthBlendable(facing, GameCams.currentEuler.y, nDirections);
		}

		public static float ToWorldAzimuthBlendable(Vector3 facing, float cameraRot, int nDirections = 8)
		{
			// TODO this needs to be optimized, it's executed many many times each frame
			float py = (Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg).WrapAngle();
			float cy = (cameraRot + 180).WrapAngle();

			float angle = py - cy;

			// Balance the azimuth on both sides
			angle += 360f / nDirections / 2f;

			// Wrapping to keep in range [0, 360[
			angle = angle.WrapAngle();

			// Snapping to 45 degree increments
			angle = angle.FloorSnap(360f / nDirections);

			return angle / 360f;
		}

		public static Direction8 ToWorldAzimuthOrdinal(Vector3 facing, Camera camera = null)
		{
			float blending = ToWorldAzimuthBlendable(facing, camera.transform.eulerAngles.y);

			int index = (blending * 8).Floor();
			return 1 + (Direction8)index;
		}

		public static Direction8 ToWorldAzimuthOrdinal(float blending)
		{
			int index = (blending * 8).Floor();
			return 1 + (Direction8)index;
		}

		public static Direction8 ToWorldAzimuthCardinal(float blending)
		{
			int index = (blending * 4).Floor();
			return 1 + (Direction8)(index * 2);
		}

	#endregion

	#region Mapping

		/// <summary>
		/// Maps a value from [sourceFrom..sourceTo] to [targetFrom..targetTo] with clamping.
		///
		/// This is basically Mathf.Lerp(targetFrom, targetTo, Mathf.InverseLerp(sourceFrom, sourceTo, sourceValue)).
		/// </summary>
		/// <param name="sourceValue">The value in the range of [sourceFrom..sourceTo]. Will be clamped if not in that range.</param>
		/// <param name="sourceFrom">The lower end of the source range.</param>
		/// <param name="sourceTo">The higher end of the source range.</param>
		/// <param name="targetFrom">The lower end of the target range.</param>
		/// <param name="targetTo">The higher end of the target range.</param>
		/// <returns>The mapped value.</returns>
		public static float Remap(float sourceValue, float sourceFrom, float sourceTo, float targetFrom, float targetTo)
		{
			float sourceRange = sourceTo - sourceFrom;
			float targetRange = targetTo - targetFrom;
			float percent     = Mathf.Clamp01((sourceValue - sourceFrom) / sourceRange);
			return targetFrom + targetRange * percent;
		}

		/// <summary>
		/// Applies a deadzone [-deadzone..deadzone] in which the value will be set to 0.
		/// </summary>
		/// <param name="value">The joystick value.</param>
		/// <param name="deadzone">A value between for which all results [-deadzone..deadzone] will be set to 0.</param>
		/// <param name="fullRangeBetweenDeadzoneAndOne">If this is true, the values between [-1..-deadzone] and [deadzone..1] will be mapped to [-1..0] and [0..1] respectively.</param>
		/// <returns>The result value between [-1..1].</returns>
		public static float ApplyJoystickDeadzone(float value, float deadzone, bool fullRangeBetweenDeadzoneAndOne = false)
		{
			if (Mathf.Abs(value) <= deadzone)
				return 0;

			if (fullRangeBetweenDeadzoneAndOne && deadzone > 0f)
			{
				if (value < 0)
				{
					return Remap(value, -1f, -deadzone, -1f, 0f);
				}
				else
				{
					return Remap(value, deadzone, 1f, 0f, 1f);
				}
			}

			return value;
		}

		/// <summary>
		/// Maps a joystick input from [sourceFrom..sourceTo] to [-1..1] with clamping.
		/// Applies a deadzone [-deadzone..deadzone] in which the value will be set to 0.
		/// </summary>
		/// <param name="sourceValue">The value in the range of [sourceFrom..sourceTo]. Will be clamped if not in that range.</param>
		/// <param name="sourceFrom">The lower end of the source range.</param>
		/// <param name="sourceTo">The higher end of the source range.</param>
		/// <param name="deadzone">A value between 0 and 1 for which all results [-deadzone..deadzone] will be set to 0.</param>
		/// <param name="fullRangeBetweenDeadzoneAndOne">If this is true, the values between [-1..-deadzone] and [deadzone..1] will be mapped to [-1..0] and [0..1] respectively.</param>
		/// <returns>The result value between [-1..1].</returns>
		public static float RemapJoystick(float sourceValue, float sourceFrom, float sourceTo, float deadzone = 0f, bool fullRangeBetweenDeadzoneAndOne = false)
		{
			float percent = Remap(sourceValue, sourceFrom, sourceTo, -1, 1);

			if (deadzone > 0)
				percent = ApplyJoystickDeadzone(percent, deadzone, fullRangeBetweenDeadzoneAndOne);

			return percent;
		}

	#endregion

	#region Angles

		/// <summary>
		/// Returns the closer center between two angles.
		/// </summary>
		/// <param name="angle1">The first angle.</param>
		/// <param name="angle2">The second angle.</param>
		/// <returns>The closer center.</returns>
		public static float GetCenterAngleDeg(float angle1, float angle2)
		{
			return angle1 + Mathf.DeltaAngle(angle1, angle2) / 2f;
		}

		/// <summary>
		/// Normalizes an angle between 0 (inclusive) and 360 (exclusive).
		/// </summary>
		/// <param name="angle">The input angle.</param>
		/// <param name="range">The range to which the angle should be mapped.</param>
		/// <returns>The result angle.</returns>
		public static float NormalizeAngleDeg(float angle, float range)
		{
			while (angle < 0)
			{
				angle += range;
			}

			if (angle >= range)
				angle %= range;

			return angle;
		}

		/// <summary>
		/// Normalizes an angle between -180 (inclusive) and 180 (exclusive).
		/// </summary>
		/// <param name="angle">The input angle.</param>
		/// <returns>The result angle.</returns>
		public static float NormalizeAngleDeg180(float angle)
		{
			return NormalizeAngleDeg(angle, 160);
		}

		/// <summary>
		/// Normalizes an angle between 0 (inclusive) and 360 (exclusive).
		/// </summary>
		/// <param name="angle">The input angle.</param>
		/// <returns>The result angle.</returns>
		public static float NormalizeAngleDeg360(float angle)
		{
			return NormalizeAngleDeg(angle, 360);
		}

	#endregion

	#region Framerate-Independent Lerping

		/// <summary>
		/// Provides a framerate-independent t for lerping towards a target.
		///
		/// Example:
		///
		///     currentValue = Mathf.Lerp(currentValue, 1f, MathHelper.EasedLerpFactor(0.75f);
		///
		/// will cover 75% of the remaining distance between currentValue and 1 each second.
		///
		/// There are essentially two ways of lerping a value over time: linear (constant speed) or
		/// eased (e.g. getting slower the closer you are to the target, see http://easings.net.)
		///
		/// For linear lerping (and most of the easing functions), you need to track the start and end
		/// positions and the time that elapsed.
		///
		/// Calling something like
		///
		///     currentValue = Mathf.Lerp(currentValue, 1f, 0.95f);
		///
		/// every frame provides an easy way of eased lerping without tracking elapsed time or the
		/// starting value, but since it's called every frame, the actual traversed distance per
		/// second changes the higher the framerate is.
		///
		/// This function replaces the lerp T to make it framerate-independent and easier to estimate.
		///
		/// For more info, see https://www.scirra.com/blog/ashley/17/using-lerp-with-delta-time.
		/// </summary>
		/// <param name="factor">How much % the lerp should cover per second.</param>
		/// <param name="deltaTime">How much time passed since the last call.</param>
		/// <returns>The framerate-independent lerp t.</returns>
		public static float EasedLerpFactor(float factor, float deltaTime = 0f)
		{
			if (deltaTime < Mathf.Epsilon)
				deltaTime = Time.deltaTime;

			return 1 - Mathf.Pow(1 - factor, deltaTime);
		}

		/// <summary>
		/// Framerate-independent eased lerping to a target value, slowing down the closer it is.
		///
		/// If you call
		///
		///     currentValue = MathHelper.EasedLerp(currentValue, 1f, 0.75f);
		///
		/// each frame (e.g. in Update()), starting with a currentValue of 0, then after 1 second
		/// it will be approximately 0.75 - which is 75% of the way between 0 and 1.
		///
		/// Adjusting the target or the percentPerSecond between calls is also possible.
		/// </summary>
		/// <param name="current">The current value.</param>
		/// <param name="target">The target value.</param>
		/// <param name="percentPerSecond">How much of the distance between current and target should be covered per second?</param>
		/// <param name="deltaTime">How much time passed since the last call.</param>
		/// <returns>The interpolated value from current to target.</returns>
		public static float EasedLerp(float current, float target, float percentPerSecond, float deltaTime = 0f)
		{
			float t = EasedLerpFactor(percentPerSecond, deltaTime);
			return Mathf.Lerp(current, target, t);
		}

	#endregion

	#region Parabolas

		public static Vector3 EvaluateParabola(Vector3 start, Vector3 end, float height, float t)
		{
			//float Func(float x) => -4 * height * x * x + 4 * height * x;

			var mid = Vector3.Lerp(start, end, t);

			var tt = -4 * height * t * t + 4 * height * t;

			return new Vector3(mid.x, tt + Mathf.Lerp(start.y, end.y, t), mid.z);
		}

		public static float ParabolaLength(float height, float distance)
		{
			float a = Mathf.Min(height, 0.0001f);
			float b = distance;

			return 0.5f * Mathf.Sqrt(b * b + 16f * a * a) +
				   (b * b) / (8f * a) *
				   Mathf.Log((4f * a + Mathf.Sqrt(b * b + 16f * a * a) ) / b);
		}

		// Note(C.L. 7-20-22): Probably not the most efficient way to do this.
		public static Vector3 ParabolaDirection(Vector3 start, Vector3 end, float height, float t)
		{
			return (EvaluateParabola(start, end, height, t + 0.01f) - EvaluateParabola(start, end, height, t)).normalized;
		}

	#endregion

	}
}