using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace Overworld.UI {
	public class DummySplashScreen : SerializedMonoBehaviour, ISplashScreen {

		public UniTask OnShow() => UniTask.CompletedTask;
		public UniTask OnHide() => UniTask.CompletedTask;
	}
}