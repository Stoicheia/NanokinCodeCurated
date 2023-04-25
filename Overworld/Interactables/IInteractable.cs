using Anjin.Actors;

namespace Overworld.Interactables
{
	public interface IInteractable
	{
		void OnInteract(Actor actor);
		bool IsBlockingInteraction(Actor actor);
	}
}