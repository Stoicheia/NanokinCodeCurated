using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Anjin.Util
{
	[AddComponentMenu("Event/Standalone Input Module")]
	public class AnjinInputModule : PointerInputModule
	{
		private                  int        m_ConsecutiveMoveCount  = 0;
		[SerializeField] private string     m_HorizontalAxis        = "Horizontal";
		[SerializeField] private string     m_VerticalAxis          = "Vertical";
		[SerializeField] private string     m_SubmitButton          = "Submit";
		[SerializeField] private string     m_CancelButton          = "Cancel";
		[SerializeField] private float      m_InputActionsPerSecond = 10f;
		[SerializeField] private float      m_RepeatDelay           = 0.5f;
		private                  float      m_PrevActionTime;
		private                  Vector2    m_LastMoveVector;
		private                  Vector2    m_LastMousePosition;
		private                  Vector2    m_MousePosition;
		private                  GameObject m_CurrentFocusedGameObject;

		[SerializeField] [FormerlySerializedAs("m_AllowActivationOnMobileDevice")]
		private bool m_ForceModuleActive;

		public bool DontDeselectOnSelectingAir = false;


		protected AnjinInputModule() { }

		[Obsolete("Mode is no longer needed on input module as it handles both mouse and keyboard simultaneously.", false)]
		public InputMode inputMode
		{
			get { return InputMode.Mouse; }
		}

		/// <summary>
		///   <para>Is this module allowed to be activated if you are on mobile.</para>
		/// </summary>
		[Obsolete(
			"allowActivationOnMobileDevice has been deprecated. Use forceModuleActive instead (UnityUpgradable) -> forceModuleActive")]
		public bool allowActivationOnMobileDevice
		{
			get { return m_ForceModuleActive; }
			set { m_ForceModuleActive = value; }
		}

		/// <summary>
		///   <para>Force this module to be active.</para>
		/// </summary>
		public bool forceModuleActive
		{
			get { return m_ForceModuleActive; }
			set { m_ForceModuleActive = value; }
		}

		/// <summary>
		///   <para>Number of keyboard / controller inputs allowed per second.</para>
		/// </summary>
		public float inputActionsPerSecond
		{
			get { return m_InputActionsPerSecond; }
			set { m_InputActionsPerSecond = value; }
		}

		/// <summary>
		///   <para>Delay in seconds before the input actions per second repeat rate takes effect.</para>
		/// </summary>
		public float repeatDelay
		{
			get { return m_RepeatDelay; }
			set { m_RepeatDelay = value; }
		}

		/// <summary>
		///   <para>Input manager name for the horizontal axis button.</para>
		/// </summary>
		public string horizontalAxis
		{
			get { return m_HorizontalAxis; }
			set { m_HorizontalAxis = value; }
		}

		/// <summary>
		///   <para>Input manager name for the vertical axis.</para>
		/// </summary>
		public string verticalAxis
		{
			get { return m_VerticalAxis; }
			set { m_VerticalAxis = value; }
		}

		/// <summary>
		///   <para>Maximum number of input events handled per second.</para>
		/// </summary>
		public string submitButton
		{
			get { return m_SubmitButton; }
			set { m_SubmitButton = value; }
		}

		/// <summary>
		///   <para>Input manager name for the 'cancel' button.</para>
		/// </summary>
		public string cancelButton
		{
			get { return m_CancelButton; }
			set { m_CancelButton = value; }
		}

		private bool ShouldIgnoreEventsOnNoFocus()
		{
			switch (SystemInfo.operatingSystemFamily)
			{
				case OperatingSystemFamily.MacOSX:
				case OperatingSystemFamily.Windows:
				case OperatingSystemFamily.Linux:
					#if UNITY_EDITOR
						return !EditorApplication.isRemoteConnected;
					#endif
				default:
					return false;
			}
		}

		/// <summary>
		///   <para>See BaseInputModule.</para>
		/// </summary>
		public override void UpdateModule()
		{
			if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
				return;
			m_LastMousePosition = m_MousePosition;
			m_MousePosition     = input.mousePosition;
		}

		/// <summary>
		///   <para>See BaseInputModule.</para>
		/// </summary>
		/// <returns>
		///   <para>Supported.</para>
		/// </returns>
		public override bool IsModuleSupported()
		{
			return m_ForceModuleActive || input.mousePresent || input.touchSupported;
		}

		/// <summary>
		///   <para>See BaseInputModule.</para>
		/// </summary>
		/// <returns>
		///   <para>Should activate.</para>
		/// </returns>
		public override bool ShouldActivateModule()
		{
			if (!base.ShouldActivateModule())
				return false;
			bool flag = m_ForceModuleActive                                                        |
			            input.GetButtonDown(m_SubmitButton)                                   |
			            input.GetButtonDown(m_CancelButton)                                   |
			            !Mathf.Approximately(input.GetAxisRaw(m_HorizontalAxis), 0.0f)        |
			            !Mathf.Approximately(input.GetAxisRaw(m_VerticalAxis),   0.0f)        |
			            (double) ( m_MousePosition - m_LastMousePosition ).sqrMagnitude > 0.0 |
			            input.GetMouseButtonDown(0);
			if (input.touchCount > 0)
				flag = true;
			return flag;
		}

		/// <summary>
		///   <para>See BaseInputModule.</para>
		/// </summary>
		public override void ActivateModule()
		{
			if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
				return;
			base.ActivateModule();
			m_MousePosition          = input.mousePosition;
			m_LastMousePosition      = input.mousePosition;
			GameObject selectedGameObject = eventSystem.currentSelectedGameObject;
			if ((UnityEngine.Object) selectedGameObject == (UnityEngine.Object) null)
				selectedGameObject = eventSystem.firstSelectedGameObject;
			eventSystem.SetSelectedGameObject(selectedGameObject, GetBaseEventData());
		}

		/// <summary>
		///   <para>See BaseInputModule.</para>
		/// </summary>
		public override void DeactivateModule()
		{
			base.DeactivateModule();
			ClearSelection();
		}

		/// <summary>
		///   <para>See BaseInputModule.</para>
		/// </summary>
		public override void Process()
		{
			if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
				return;
			bool selectedObject = SendUpdateEventToSelectedObject();
			if (eventSystem.sendNavigationEvents)
			{
				if (!selectedObject)
					selectedObject |= SendMoveEventToSelectedObject();
				if (!selectedObject)
					SendSubmitEventToSelectedObject();
			}

			if (ProcessTouchEvents() || !input.mousePresent)
				return;
			ProcessMouseEvent();
		}

		private bool ProcessTouchEvents()
		{
			for (int index = 0; index < input.touchCount; ++index)
			{
				Touch touch = input.GetTouch(index);
				if (touch.type != TouchType.Indirect)
				{
					bool             pressed;
					bool             released;
					PointerEventData pointerEventData = GetTouchPointerEventData(touch, out pressed, out released);
					ProcessTouchPress(pointerEventData, pressed, released);
					if (!released)
					{
						ProcessMove(pointerEventData);
						ProcessDrag(pointerEventData);
					}
					else
						RemovePointerData(pointerEventData);
				}
			}

			return input.touchCount > 0;
		}

		/// <summary>
		///   <para>This method is called by Unity whenever a touch event is processed. Override this method with a custom implementation to process touch events yourself.</para>
		/// </summary>
		/// <param name="pointerEvent">Event data relating to the touch event, such as position and ID to be passed to the touch event destination object.</param>
		/// <param name="pressed">This is true for the first frame of a touch event, and false thereafter. This can therefore be used to determine the instant a touch event occurred.</param>
		/// <param name="released">This is true only for the last frame of a touch event.</param>
		protected void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
		{
			GameObject gameObject1 = pointerEvent.pointerCurrentRaycast.gameObject;
			if (pressed)
			{
				pointerEvent.eligibleForClick    = true;
				pointerEvent.delta               = Vector2.zero;
				pointerEvent.dragging            = false;
				pointerEvent.useDragThreshold    = true;
				pointerEvent.pressPosition       = pointerEvent.position;
				pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
				DeselectIfSelectionChanged(gameObject1, (BaseEventData) pointerEvent);
				if ((UnityEngine.Object) pointerEvent.pointerEnter != (UnityEngine.Object) gameObject1)
				{
					HandlePointerExitAndEnter(pointerEvent, gameObject1);
					pointerEvent.pointerEnter = gameObject1;
				}

				GameObject gameObject2 = ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(gameObject1,
					(BaseEventData) pointerEvent, ExecuteEvents.pointerDownHandler);
				if ((UnityEngine.Object) gameObject2 == (UnityEngine.Object) null)
					gameObject2       = ExecuteEvents.GetEventHandler<IPointerClickHandler>(gameObject1);
				float unscaledTime = Time.unscaledTime;
				if ((UnityEngine.Object) gameObject2 == (UnityEngine.Object) pointerEvent.lastPress)
				{
					if ((double) ( unscaledTime - pointerEvent.clickTime ) < 0.300000011920929)
						++pointerEvent.clickCount;
					else
						pointerEvent.clickCount = 1;
					pointerEvent.clickTime   = unscaledTime;
				}
				else
					pointerEvent.clickCount = 1;

				pointerEvent.pointerPress    = gameObject2;
				pointerEvent.rawPointerPress = gameObject1;
				pointerEvent.clickTime       = unscaledTime;
				pointerEvent.pointerDrag     = ExecuteEvents.GetEventHandler<IDragHandler>(gameObject1);
				if ((UnityEngine.Object) pointerEvent.pointerDrag != (UnityEngine.Object) null)
					ExecuteEvents.Execute<IInitializePotentialDragHandler>(pointerEvent.pointerDrag, (BaseEventData) pointerEvent,
						ExecuteEvents.initializePotentialDrag);
			}

			if (!released)
				return;
			ExecuteEvents.Execute<IPointerUpHandler>(pointerEvent.pointerPress, (BaseEventData) pointerEvent,
				ExecuteEvents.pointerUpHandler);
			GameObject eventHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(gameObject1);
			if ((UnityEngine.Object) pointerEvent.pointerPress == (UnityEngine.Object) eventHandler &&
			    pointerEvent.eligibleForClick)
				ExecuteEvents.Execute<IPointerClickHandler>(pointerEvent.pointerPress, (BaseEventData) pointerEvent,
					ExecuteEvents.pointerClickHandler);
			else if ((UnityEngine.Object) pointerEvent.pointerDrag != (UnityEngine.Object) null && pointerEvent.dragging)
				ExecuteEvents.ExecuteHierarchy<IDropHandler>(gameObject1, (BaseEventData) pointerEvent, ExecuteEvents.dropHandler);
			pointerEvent.eligibleForClick = false;
			pointerEvent.pointerPress     = (GameObject) null;
			pointerEvent.rawPointerPress  = (GameObject) null;
			if ((UnityEngine.Object) pointerEvent.pointerDrag != (UnityEngine.Object) null && pointerEvent.dragging)
				ExecuteEvents.Execute<IEndDragHandler>(pointerEvent.pointerDrag, (BaseEventData) pointerEvent,
					ExecuteEvents.endDragHandler);
			pointerEvent.dragging    = false;
			pointerEvent.pointerDrag = (GameObject) null;
			ExecuteEvents.ExecuteHierarchy<IPointerExitHandler>(pointerEvent.pointerEnter, (BaseEventData) pointerEvent,
				ExecuteEvents.pointerExitHandler);
			pointerEvent.pointerEnter = (GameObject) null;
		}

		/// <summary>
		///   <para>Calculate and send a submit event to the current selected object.</para>
		/// </summary>
		/// <returns>
		///   <para>If the submit event was used by the selected object.</para>
		/// </returns>
		protected bool SendSubmitEventToSelectedObject()
		{
			if ((UnityEngine.Object) eventSystem.currentSelectedGameObject == (UnityEngine.Object) null)
				return false;
			BaseEventData baseEventData = GetBaseEventData();
			if (input.GetButtonDown(m_SubmitButton))
				ExecuteEvents.Execute<ISubmitHandler>(eventSystem.currentSelectedGameObject, baseEventData,
					ExecuteEvents.submitHandler);
			if (input.GetButtonDown(m_CancelButton))
				ExecuteEvents.Execute<ICancelHandler>(eventSystem.currentSelectedGameObject, baseEventData,
					ExecuteEvents.cancelHandler);
			return baseEventData.used;
		}

		private Vector2 GetRawMoveVector()
		{
			Vector2 zero = Vector2.zero;
			zero.x       = input.GetAxisRaw(m_HorizontalAxis);
			zero.y       = input.GetAxisRaw(m_VerticalAxis);
			if (input.GetButtonDown(m_HorizontalAxis))
			{
				if ((double) zero.x < 0.0)
					zero.x = -1f;
				if ((double) zero.x > 0.0)
					zero.x = 1f;
			}

			if (input.GetButtonDown(m_VerticalAxis))
			{
				if ((double) zero.y < 0.0)
					zero.y = -1f;
				if ((double) zero.y > 0.0)
					zero.y = 1f;
			}

			return zero;
		}

		/// <summary>
		///   <para>Calculate and send a move event to the current selected object.</para>
		/// </summary>
		/// <returns>
		///   <para>If the move event was used by the selected object.</para>
		/// </returns>
		protected bool SendMoveEventToSelectedObject()
		{
			float   unscaledTime  = Time.unscaledTime;
			Vector2 rawMoveVector = GetRawMoveVector();
			if (Mathf.Approximately(rawMoveVector.x, 0.0f) && Mathf.Approximately(rawMoveVector.y, 0.0f))
			{
				m_ConsecutiveMoveCount = 0;
				return false;
			}

			bool flag1 = input.GetButtonDown(m_HorizontalAxis) || input.GetButtonDown(m_VerticalAxis);
			bool flag2 = (double) Vector2.Dot(rawMoveVector, m_LastMoveVector) > 0.0;
			if (!flag1)
				flag1 = !flag2 || m_ConsecutiveMoveCount != 1
					? (double) unscaledTime                      >
					  (double) m_PrevActionTime                         + 1.0 / (double) m_InputActionsPerSecond
					: (double) unscaledTime > (double) m_PrevActionTime + (double) m_RepeatDelay;
			if (!flag1)
				return false;
			AxisEventData axisEventData = GetAxisEventData(rawMoveVector.x, rawMoveVector.y, 0.6f);
			if (axisEventData.moveDir != MoveDirection.None)
			{
				ExecuteEvents.Execute<IMoveHandler>(eventSystem.currentSelectedGameObject, (BaseEventData) axisEventData,
					ExecuteEvents.moveHandler);
				if (!flag2)
					m_ConsecutiveMoveCount = 0;
				++m_ConsecutiveMoveCount;
				m_PrevActionTime = unscaledTime;
				m_LastMoveVector = rawMoveVector;
			}
			else
				m_ConsecutiveMoveCount = 0;

			return axisEventData.used;
		}

		/// <summary>
		///   <para>Iterate through all the different mouse events.</para>
		/// </summary>
		/// <param name="id">The mouse pointer Event data id to get.</param>
		protected void ProcessMouseEvent()
		{
			ProcessMouseEvent(0);
		}

		[Obsolete("This method is no longer checked, overriding it with return true does nothing!")]
		protected virtual bool ForceAutoSelect()
		{
			return false;
		}

		/// <summary>
		///   <para>Iterate through all the different mouse events.</para>
		/// </summary>
		/// <param name="id">The mouse pointer Event data id to get.</param>
		protected void ProcessMouseEvent(int id)
		{
			MouseState           pointerEventData = GetMousePointerEventData(id);
			MouseButtonEventData eventData        =
				pointerEventData.GetButtonState(PointerEventData.InputButton.Left).eventData;
			m_CurrentFocusedGameObject = eventData.buttonData.pointerCurrentRaycast.gameObject;
			ProcessMousePress(eventData);
			ProcessMove(eventData.buttonData);
			ProcessDrag(eventData.buttonData);
			ProcessMousePress(pointerEventData.GetButtonState(PointerEventData.InputButton.Right).eventData);
			ProcessDrag(pointerEventData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
			ProcessMousePress(pointerEventData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
			ProcessDrag(pointerEventData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);
			if (Mathf.Approximately(eventData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
				return;
			ExecuteEvents.ExecuteHierarchy<IScrollHandler>(
				ExecuteEvents.GetEventHandler<IScrollHandler>(eventData.buttonData.pointerCurrentRaycast.gameObject),
				(BaseEventData) eventData.buttonData, ExecuteEvents.scrollHandler);
		}

		/// <summary>
		///   <para>Send a update event to the currently selected object.</para>
		/// </summary>
		/// <returns>
		///   <para>If the update event was used by the selected object.</para>
		/// </returns>
		protected bool SendUpdateEventToSelectedObject()
		{
			if ((UnityEngine.Object) eventSystem.currentSelectedGameObject == (UnityEngine.Object) null)
				return false;
			BaseEventData baseEventData = GetBaseEventData();
			ExecuteEvents.Execute<IUpdateSelectedHandler>(eventSystem.currentSelectedGameObject, baseEventData,
				ExecuteEvents.updateSelectedHandler);
			return baseEventData.used;
		}

		protected void ProcessMousePress(MouseButtonEventData data)
		{
			PointerEventData buttonData  = data.buttonData;
			GameObject       gameObject1 = buttonData.pointerCurrentRaycast.gameObject;
			if (data.PressedThisFrame())
			{
				buttonData.eligibleForClick    = true;
				buttonData.delta               = Vector2.zero;
				buttonData.dragging            = false;
				buttonData.useDragThreshold    = true;
				buttonData.pressPosition       = buttonData.position;
				buttonData.pointerPressRaycast = buttonData.pointerCurrentRaycast;

				bool DontDeselect = false;

				//Get Anjin UI Options
				if (gameObject1 != null)
				{
					var c = gameObject1.GetComponent<AnjinUIInputSettings>();
					if (c != null)
					{
						DontDeselect = c.DontDeselectPrevious;
					}
				}
				else
				{
					DontDeselect = DontDeselectOnSelectingAir;
				}

				if (!DontDeselect)
				{
					DeselectIfSelectionChanged(gameObject1, (BaseEventData) buttonData);
				}

				GameObject gameObject2 = ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(gameObject1,
					(BaseEventData) buttonData, ExecuteEvents.pointerDownHandler);
				if ((UnityEngine.Object) gameObject2 == (UnityEngine.Object) null)
					gameObject2       = ExecuteEvents.GetEventHandler<IPointerClickHandler>(gameObject1);
				float unscaledTime = Time.unscaledTime;
				if ((UnityEngine.Object) gameObject2 == (UnityEngine.Object) buttonData.lastPress)
				{
					if ((double) ( unscaledTime - buttonData.clickTime ) < 0.300000011920929)
						++buttonData.clickCount;
					else
						buttonData.clickCount = 1;
					buttonData.clickTime   = unscaledTime;
				}
				else
					buttonData.clickCount = 1;

				buttonData.pointerPress    = gameObject2;
				buttonData.rawPointerPress = gameObject1;
				buttonData.clickTime       = unscaledTime;
				buttonData.pointerDrag     = ExecuteEvents.GetEventHandler<IDragHandler>(gameObject1);
				if ((UnityEngine.Object) buttonData.pointerDrag != (UnityEngine.Object) null)
					ExecuteEvents.Execute<IInitializePotentialDragHandler>(buttonData.pointerDrag, (BaseEventData) buttonData,
						ExecuteEvents.initializePotentialDrag);
			}

			if (!data.ReleasedThisFrame())
				return;
			ExecuteEvents.Execute<IPointerUpHandler>(buttonData.pointerPress, (BaseEventData) buttonData,
				ExecuteEvents.pointerUpHandler);
			GameObject eventHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(gameObject1);
			if ((UnityEngine.Object) buttonData.pointerPress == (UnityEngine.Object) eventHandler && buttonData.eligibleForClick)
				ExecuteEvents.Execute<IPointerClickHandler>(buttonData.pointerPress, (BaseEventData) buttonData,
					ExecuteEvents.pointerClickHandler);
			else if ((UnityEngine.Object) buttonData.pointerDrag != (UnityEngine.Object) null && buttonData.dragging)
				ExecuteEvents.ExecuteHierarchy<IDropHandler>(gameObject1, (BaseEventData) buttonData, ExecuteEvents.dropHandler);
			buttonData.eligibleForClick = false;
			buttonData.pointerPress     = (GameObject) null;
			buttonData.rawPointerPress  = (GameObject) null;
			if ((UnityEngine.Object) buttonData.pointerDrag != (UnityEngine.Object) null && buttonData.dragging)
				ExecuteEvents.Execute<IEndDragHandler>(buttonData.pointerDrag, (BaseEventData) buttonData,
					ExecuteEvents.endDragHandler);
			buttonData.dragging    = false;
			buttonData.pointerDrag = (GameObject) null;
			if ((UnityEngine.Object) gameObject1 != (UnityEngine.Object) buttonData.pointerEnter)
			{
				HandlePointerExitAndEnter(buttonData, (GameObject) null);
				HandlePointerExitAndEnter(buttonData, gameObject1);
			}
		}

		protected GameObject GetCurrentFocusedGameObject()
		{
			return m_CurrentFocusedGameObject;
		}

		[Obsolete("Mode is no longer needed on input module as it handles both mouse and keyboard simultaneously.", false)]
		public enum InputMode
		{
			Mouse,
			Buttons,
		}
	}
}