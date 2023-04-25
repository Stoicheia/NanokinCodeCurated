using UnityEngine;

namespace Util.Extensions
{
	public static class ParticleSystemExtensions
	{
		/// <summary>
		/// Sets a particle system's play state.
		/// </summary>
		/// <param name="ps">The particle system. Handles null gracefully by doing nothing.</param>
		/// <param name="state">Play state to set on the particle system.</param>
		public static void SetPlaying(this ParticleSystem ps, bool state, bool safe = true)
		{
			if (safe && !ps) // TODO remove implicit check
				return;

			if (state && !ps.isPlaying) ps.Play();
			else if (!state && ps.isPlaying) ps.Stop();
		}
	}
}