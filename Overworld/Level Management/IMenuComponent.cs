using Cysharp.Threading.Tasks;

namespace Anjin.Nanokin
{
	public interface IMenuComponent
	{
		/// <summary>
		/// Enter all UI elements. (animated)
		/// </summary>
		UniTask EnableMenu();

		/// <summary>
		/// Exit all UI elements. (animated)
		/// </summary>
		/// <returns></returns>
		UniTask DisableMenu();

		UniTask SetState(bool state);
	}
}