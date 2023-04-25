using System;
using System.Runtime.CompilerServices;
using Anjin.Scripting;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static void ForgetWithErrors(this UniTask task)
		{
			forgetWithErrors(task).Forget();

		}

		private static async UniTaskVoid forgetWithErrors(this UniTask task)
		{
			try
			{
				await task;
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}
		}

		public static void ForgetWithErrors<T>(this UniTask<T> task)
		{
			forgetWithErrors(task).Forget();
		}

		private static async UniTaskVoid forgetWithErrors<T>(this UniTask<T> task)
		{
			try
			{
				await task;
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}
		}
	}
}