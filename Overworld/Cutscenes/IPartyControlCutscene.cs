using Anjin.Actors;

namespace Overworld.Cutscenes
{
	/// <summary>
	/// An in-game cutscene that wants to be able to take control of the party member actors in some way.
	/// </summary>
	public interface IPartyControlCutscene
	{
		ActorBrain GetPartyMemberBrain();
	}
}