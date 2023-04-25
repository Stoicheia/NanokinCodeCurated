using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Overworld.Cutscenes.Timeline
{
	public class LuaCallMarker : Marker, INotification
	{
		public PropertyName id { get; }

		public string FunctionName;
		public bool   PauseTillDone;
	}
}