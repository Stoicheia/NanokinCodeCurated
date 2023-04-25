using System;
using System.ComponentModel;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Overworld.UI
{
	[DisplayName("Held Repeat")]
	[UsedImplicitly]
	public class RepeatInteraction : IInputInteraction
	{
		public float delay = 0.405f;
		public float speed = 0.100f;

		private double _timePressed;
		private int    _presses;

		// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		// private static void Init()
		// {
		// }

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		private static void InitEditor()
		{
			InputSystem.RegisterInteraction(typeof(RepeatInteraction));
		}
#endif


		public void Process(ref InputInteractionContext context)
		{
			switch (context.phase)
			{
				case InputActionPhase.Disabled:
					break;

				case InputActionPhase.Waiting:
					if (context.ControlIsActuated())
					{
						_timePressed = context.time;
						_presses     = 0;

						context.Started();
						context.SetTimeout(delay);
					}

					break;

				case InputActionPhase.Started:
					if (!context.ControlIsActuated())
					{
						context.Canceled();
						break;
					}

					context.PerformedAndStayStarted();
					context.SetTimeout(speed);

					break;

				case InputActionPhase.Performed:
					break;

				case InputActionPhase.Canceled:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void Reset()
		{
			_timePressed = 0;
			_presses     = 0;
		}
	}


// #if UNITY_EDITOR
// 	/// <summary>
// 	/// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
// 	/// </summary>
// 	internal class HoldInteractionEditor : InputParameterEditor<HoldInteraction>
// 	{
// 		protected override void OnEnable()
// 		{
// 			m_PressPointSetting.Initialize("Press Point",
// 				"Float value that an axis control has to cross for it to be considered pressed.",
// 				"Default Button Press Point",
// 				() => target.pressPoint, v => target.pressPoint = v, () => ButtonControl.s_GlobalDefaultButtonPressPoint);
// 			m_DurationSetting.Initialize("Hold Time",
// 				"Time (in seconds) that a control has to be held in order for it to register as a hold.",
// 				"Default Hold Time",
// 				() => target.duration, x => target.duration = x, () => InputSystem.settings.defaultHoldTime);
// 		}
//
// 		public override void OnGUI()
// 		{
// 			m_PressPointSetting.OnGUI();
// 			m_DurationSetting.OnGUI();
// 		}
//
// 		private CustomOrDefaultSetting m_PressPointSetting;
// 		private CustomOrDefaultSetting m_DurationSetting;
// 	}
// #endif
}