using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.UnityTimeline;

namespace Combat.Toolkit.Timeline
{
	public class LuaPosClip : PlayableAsset, ITimelineClipAsset, ILuaClip
	{
		[SerializeField] public string Lua;

		public Script Script { get; set; }

		public ClipCaps clipCaps => ClipCaps.Blending;

		public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
		{
			ScriptPlayable<Data> data = ScriptPlayable<Data>.Create(graph);

			if (Application.isPlaying)
			{
				data.GetBehaviour().lua = Lua;
			}

			return data;
		}

		public class Data : PositionData
		{
			public string lua;
			public Script script;

			public override void PrepareFrame(Playable playable, FrameData info)
			{
				if (script == null) return;

				DynValue ret = script.DoString(lua);
				if (ret.UserData.Object is Vector3)
				{
					position = (Vector3) ret.UserData.Object;
				}
			}
		}
	}
}