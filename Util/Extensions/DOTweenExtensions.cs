using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static TTween JoinTo<TTween>(this TTween tween, Sequence sequence)
			where TTween : Tween
		{
			if (tween == null) return null;

			sequence.Join(tween);
			return tween;
		}

		public static TTween AppendTo<TTween>(this TTween tween, Sequence sequence)
			where TTween : Tween
		{
			if (tween == null) return null;

			sequence.Append(tween);
			return tween;
		}

		public static UniTask Token(this Tween tween, CancellationToken cancellationToken, TweenCancelBehaviour cancel = TweenCancelBehaviour.Complete)
			=> tween.ToUniTask(cancel, cancellationToken: cancellationToken);
	}
}