using UnityEngine;

namespace Combat.Data
{
	public interface ITargetable
	{
		/// <summary>
		/// Real position of the target.
		/// </summary>
		Vector3 GetTargetPosition();

		/// <summary>
		/// Center of the target, visually.
		/// </summary>
		Vector3 GetTargetCenter();

		/// <summary>
		/// Objects of the target.
		/// </summary>
		GameObject GetTargetObject();
	}
}