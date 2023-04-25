
namespace Anjin.Scripting.Waitables
{
	public interface IActivatableWithTransitions {
		bool IsActive { get; }
		void Hide();
		void HideInstant();
	}
}