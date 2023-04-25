using Anjin.Actors;
using Anjin.UI;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;

namespace Core.Manageds
{
	public class ManagedSpeechBubble : CoroutineManaged
	{
		private readonly DirectedActor _actor;
		private readonly DynValue      _text;
		private readonly float?        _seconds;
		private readonly Table         _bubble_settings;

		private SpeechBubble _bubble;

		public override bool Active => _bubble.state > HUDBubble.State.Off;

		public ManagedSpeechBubble(DirectedActor actor, DynValue text, float? seconds, Table bubble_settings)
		{
			_actor           = actor;
			_text            = text;
			_seconds         = seconds;
			_bubble_settings = bubble_settings;
		}

		public override void OnStart()
		{
			Table settings = _bubble_settings ?? _actor.bubbleSettings;

			(SpeechBubble bubble, bool ok) = _seconds.HasValue
				? ActorsLua.Say(_actor.actor, _text, _seconds.Value, settings)
				: ActorsLua.Say(_actor.actor, _text, settings);

			if (!ok)
			{
				Stop();
				return;
			}

			_bubble = bubble;
		}

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			_bubble.StartDeactivation(!skipped);
		}

	}
}