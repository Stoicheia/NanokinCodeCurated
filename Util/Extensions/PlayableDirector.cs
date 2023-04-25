using System;
using System.Collections.Generic;
using Anjin.Cameras;
using Cinemachine;
using Combat;
using Combat.Toolkit.Timeline;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.Components.Cinemachine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
#endif

namespace Anjin.Util
{
	public static partial class Extensions
	{
		private static List<PlayableAsset> _scratchPlayableAssets = new List<PlayableAsset>();

		public static void Set(this PlayableDirector director, PlayableAsset asset)
		{
			director.Play(asset);
			director.time = director.duration;
		}

		public static void AjPlay(this PlayableDirector director, TimelineAsset timeline)
		{
			director.InjectCinemachineBrain();
			director.Play(timeline);
		}

		public static void InjectCinemachineBrain(this PlayableDirector director)
		{
			TimelineAsset trackAsset = director.playableAsset as TimelineAsset;

			if (trackAsset == null)
				return;

			foreach (TrackAsset track in trackAsset.GetOutputTracks())
			{
				if (track is CinemachineTrack cinemachineTrack)
				{
					director.SetGenericBinding(cinemachineTrack, Application.isPlaying
						? GameCams.Live.Brain
						: Object.FindObjectOfType<CinemachineBrain>());
				}
			}
		}

		public static void InjectOrbitExtension(this PlayableDirector director, CinemachineOrbit orbit)
		{
			TimelineAsset trackAsset = director.playableAsset as TimelineAsset;

			if (trackAsset == null)
				return;

			foreach (TrackAsset track in trackAsset.GetOutputTracks())
			{
				if (track is OrbitTrack orbitExtensionTrack)
				{
					director.SetGenericBinding(orbitExtensionTrack, orbit);
				}
			}
		}

		public static void InjectVCam(this PlayableDirector director, CinemachineVirtualCamera vcam)
		{
			TimelineAsset trackAsset = director.playableAsset as TimelineAsset;

			if (trackAsset == null)
				return;

			List<PlayableAsset> clips = director.GetClips<CinemachineShot>();

			foreach (PlayableAsset clipAsset in clips)
			{
				CinemachineShot shot = (CinemachineShot) clipAsset;

				Object referenceValue = director.GetReferenceValue(shot.VirtualCamera.exposedName, out bool idValid);
				if (referenceValue == null)
				{
					shot.VirtualCamera.exposedName = Guid.NewGuid().ToString();
					director.SetReferenceValue(shot.VirtualCamera.exposedName, vcam);
				}
			}
		}

		public static void InjectLookPoint(this PlayableDirector director, Transform focusPoint)
		{
			TimelineAsset trackAsset = director.playableAsset as TimelineAsset;

			if (trackAsset == null)
				return;

			foreach (TrackAsset track in trackAsset.GetOutputTracks())
			{
				if (track is LookPosTrack cameraFocusTrack)
				{
					director.SetGenericBinding(cameraFocusTrack, focusPoint);
				}
			}
		}

		public static void InjectArena(this PlayableDirector director, Arena arena)
		{
			TimelineAsset trackAsset = director.playableAsset as TimelineAsset;

			if (trackAsset == null)
				return;

			List<PlayableAsset> clips = director.GetClips<IArenaHolder>();

			foreach (PlayableAsset clipAsset in clips)
			{
				IArenaHolder arenaHolder = (IArenaHolder) clipAsset;
				arenaHolder.Arena = arena;
			}
		}

		public static void InjectBattle(this PlayableDirector director, BattleRunner runner)
		{
			TimelineAsset trackAsset = director.playableAsset as TimelineAsset;

			if (trackAsset == null)
				return;

			List<PlayableAsset> clips = director.GetClips<IBattleClip>();

			foreach (PlayableAsset clipAsset in clips)
			{
				IBattleClip battleClip = (IBattleClip) clipAsset;
				battleClip.Runner = runner;
			}
		}

		public static List<PlayableAsset> GetClips<TClip>(this PlayableDirector director)
		{
			_scratchPlayableAssets.Clear();

			TimelineAsset trackAsset = director.playableAsset as TimelineAsset;

			if (trackAsset == null)
				return _scratchPlayableAssets;

			for (int i = 0; i < trackAsset.outputTrackCount; i++)
			{
				TrackAsset outputTrack = trackAsset.GetOutputTrack(i);

				TimelineClip[] clips = (TimelineClip[]) outputTrack.GetClips();
				foreach (TimelineClip clip in clips)
				{
					if (clip.asset is TClip)
					{
						_scratchPlayableAssets.Add((PlayableAsset) clip.asset);
					}
				}
			}

			return _scratchPlayableAssets;
		}

		/// <summary>
		/// Resume and await until the director's state is not playing
		/// </summary>
		/// <param name="director"></param>
		public static async UniTask ResumeAsync(this PlayableDirector director)
		{
			director.Resume();
			while (director.state == PlayState.Playing) // TODO BUG BIG BAD BUG we cannot rely on director.state, it will change if you pause the game
				await UniTask.NextFrame();
		}
	}
}