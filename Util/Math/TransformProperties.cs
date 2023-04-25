using UnityEngine;

namespace Util
{
	public struct TransformProperties
	{
		public readonly Vector3 position;
		public readonly Vector3 eulerAngles;
		public readonly Vector3 scale;

		public TransformProperties(Vector3 position, Vector3 eulerAngles, Vector3 scale)
		{
			this.position    = position;
			this.eulerAngles = eulerAngles;
			this.scale       = scale;
		}

		public TransformProperties(Transform transform)
		{
			position    = transform.localPosition;
			eulerAngles = transform.localEulerAngles;
			scale       = transform.localScale;
		}

		public void ApplyLocal(Transform tfmCenterSprite)
		{
			tfmCenterSprite.localPosition    = position;
			tfmCenterSprite.localScale       = scale;
			tfmCenterSprite.localEulerAngles = eulerAngles;
		}
	}
}