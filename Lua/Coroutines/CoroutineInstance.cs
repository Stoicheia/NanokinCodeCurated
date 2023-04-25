using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Anjin.Scripting.Waitables;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Assertions;
using Coroutine = MoonSharp.Interpreter.Coroutine;

namespace Anjin.Scripting
{
	[SuppressMessage("ReSharper", "Unity.RedundantHideInInspectorAttribute")]
	public class CoroutineInstance
	{
		/// <summary>
		/// Table we are executing inside of which should be holding the closure.
		/// </summary>
		[HideInInspector] public readonly Table envtable;

		/// <summary>
		/// Closure which we are executing as a coroutine.
		/// </summary>
		[HideInInspector] public readonly Closure closure;

		/// <summary>
		/// Input args to the coroutine.
		/// </summary>
		public readonly object[] args;

		/// <summary>
		/// Coroutine which we are executing.
		/// </summary>
		[HideInInspector] public readonly Coroutine coroutine;

		/// <summary>
		/// Return value of the coroutine.
		/// TODO UNTESTED!
		/// </summary>
		[HideInInspector] public DynValue returnValue = DynValue.Nil;

		[Space]
		public States state = States.Ready;
		private bool _justYielded;

		[ShowInInspector] private float _timer;

		[NotNull]
		[ShowInInspector] private List<ICoroutineWaitable> _waitables;

		[CanBeNull]
		[ShowInInspector] private List<ICoroutineWaitable> _bgwaitables;

		[HideInInspector] public Action onBeforeResume;
		[HideInInspector] public Action onAfterResume;
		[HideInInspector] public Action onEnding;

		public bool skipEverything;

#if UNITY_EDITOR
		/// <summary>
		/// Debugging utility.
		/// </summary>
		[UsedImplicitly]
		private string _creationTrace;
#endif

		public CoroutineInstance([CanBeNull] Table envtable, Closure closure, Coroutine coroutine, object[] args = null)
		{
			this.envtable  = envtable;
			this.closure   = closure;
			this.coroutine = coroutine;
			this.args      = args;
			_timer         = 0;
			_waitables     = new List<ICoroutineWaitable>();

#if UNITY_EDITOR
			_creationTrace = Environment.StackTrace;
#endif
		}

		/// <summary>
		/// Debugging utility
		/// </summary>
		[UsedImplicitly]
		public string FunctionName { get; set; }

		/// <summary>
		/// Debugging utility
		/// </summary>
		[UsedImplicitly]
		public string TableName => envtable?.GetEnvName() ?? "(none)";

		public CoroutineState State => coroutine.State;


		public enum States
		{
			Ready,
			Running,
			Canceled,
			Ended,
		}

		public bool Running => state == States.Running;

		public bool Ended => state == States.Canceled || state == States.Ended;
		// public bool NotStarted => !canceled && coroutine.State == CoroutineState.NotStarted;

		public void Cancel()
		{
			state = States.Canceled;
		}

		/// <summary>
		/// Continue the coroutine until it's waiting on something external.
		/// </summary>
		public void TryContinue(float dt, bool with_skipping = false)
		{
			if (state == States.Canceled)
			{
				this.LogError("Cannot continue CoroutineInstance since it's been canceled.");
				return;
			}

			bool skipping = skipEverything || with_skipping;

			_justYielded = false;

			while (ContinueAwait(dt, skipping))
			{
				if (state == States.Ready)
					state = States.Running;

				_justYielded = false;
				_timer       = 0;
				_waitables.Clear();

				// Continue the function until we hit a coroutine.yield call
				try
				{
					onBeforeResume?.Invoke();

					if (envtable != null)
					{
						// This is EXTREMELY important to run several coroutines in the same script table
						envtable["_coroutine"] = this;
						envtable["force_next"] = false;
						envtable["skip_next"]  = false;
						envtable["skipping"]   = skipping;

						// handled by wait.lua, clears the state (push/pop operation)
						Lua.Invoke(envtable, "__before_resume", null, true);
					}

					DynValue yieldval = args == null
						// We have to do this to avoid co-variant array conversion and use the correct overload instead.
						// Co-variant array conversion may asplode the computer, or something..
						? coroutine.Resume(LuaUtil.NO_ARGS)
						: coroutine.Resume(args);

					if (envtable != null)
					{
						// here comes the pop
						Lua.Invoke(envtable, "__after_resume", null, true);
					}

					if (coroutine.State == CoroutineState.Dead)
					{
						state       = States.Ended;
						returnValue = yieldval; // Set the final return value of coroutine. (UNTESTED!!!)
						onEnding?.Invoke();
						break;
					}

					Assert.IsNotNull(yieldval);
					Assert.IsFalse(yieldval.IsNil());


					// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
					switch (yieldval.Type)
					{
						case DataType.String:
							if (yieldval.String == "end_skip")
							{
								//onAfterResume?.Invoke();
								goto end_loop;
							}

							break;

						case DataType.Number:
							if (!skipping)
							{
								_timer       = (float)yieldval.Number;
								_justYielded = true;
							}

							break;

						case DataType.Table:
						{
							_waitables.Clear();
							foreach (DynValue value in yieldval.Table.Values)
							{
								if (value.Type != DataType.UserData) continue;
								if (value.UserData != null && value.UserData.Object is ICoroutineWaitable waitable && !(HandleSkippables(waitable, skipping, true)))
									_waitables.Add(waitable);
							}

							_justYielded = true;
							break;
						}

						case DataType.UserData:
						{
							var waitable = yieldval.UserData.Object as ICoroutineWaitable;

							if (!HandleSkippables(waitable, skipping, true))
							{
								_waitables.Clear();
								if (waitable != null)
									_waitables.Add(waitable);

								_justYielded = true;
							}

							break;
						}
					}
				}
				catch (InterpreterException e)
				{
					state = States.Canceled;
					Debug.LogError(e.GetPrettyString());
					break;
				}
				catch (Exception e)
				{
					state = States.Canceled;
					Debug.LogException(e);
					break;
				}
				finally
				{
					onAfterResume?.Invoke();
				}

				// Handle special coroutine states
				// ----------------------------------------
				if (coroutine.State == CoroutineState.NotStarted)
				{
					// weird... shouldn't happen
					break;
				}
			}

			end_loop: ;
		}

		/// <summary>
		/// Await the waitables for completion.
		/// </summary>
		/// <returns>True if we are done waiting.</returns>
		public bool ContinueAwait(float dt, bool skipping)
		{
			// note: the execution order is important, waitables need to receive justYielded.
			// In theory, both timer and waitables could be used at the same time

			bool delay_frame = false;

			// Bg waitables
			if (_bgwaitables != null)
			{
				for (var i = 0; i < _bgwaitables.Count; i++)
				{
					ICoroutineWaitable waitable = _bgwaitables[i];

					bool skipped = HandleSkippables(waitable, skipping);
					if (skipped || waitable.CanContinue(_justYielded))
					{
						_bgwaitables.RemoveAt(i--);
					}
				}
			}

			// Waitables
			for (var i = 0; i < _waitables.Count; i++)
			{
				ICoroutineWaitable waitable = _waitables[i];

				bool skipped = HandleSkippables(waitable, skipping);
				/*if (skipped && waitable is ManagedPlayableDirector) {
					delay_frame = true;
				}*/

				if (skipped || waitable.CanContinue(_justYielded))
				{
					_waitables.RemoveAt(i--);
				}
			}

			// Timer
			if (_timer > 0 && !skipping)
			{
				_timer -= dt;
				if (_timer > 0)
					return false;
			}
			else
			{
				_timer = 0;
			}

			return !delay_frame && _waitables.Count == 0;
		}

		bool HandleSkippables(ICoroutineWaitable waitable, bool skipping, bool logging = false)
		{
			if (!skipping)
				return false;

			if (!(waitable is CoroutineManaged managed))
			{
				if (logging)
					this.LogTrace("--", $"Cannot skip waitable of type {waitable.GetType()}.");
				return false;
			}

			if (!managed.Skippable)
			{
				if (logging)
					this.LogTrace("--", $"Cannot skip managed of type {managed.GetType()} because it's Skippable flag is false.");
				return false;
			}

			this.LogTrace("--", $"Skip {managed.GetType()}.");
			if (managed.state == CoroutineManaged.State.Running)
				managed.Stop(true);
			else if (managed.state == CoroutineManaged.State.Skipping)
				return false;

			return true;
		}

		public async UniTask Play()
		{
			throw new NotImplementedException();
		}

		public void OutsideWait(ICoroutineWaitable waitable)
		{
			_waitables.Add(waitable);
		}

		public void OnBeginSkip()
		{
			if (_bgwaitables != null)
			{
				for (var i = 0; i < _bgwaitables.Count; i++)
				{
					ICoroutineWaitable waitable = _bgwaitables[i];

					bool skipped = HandleSkippables(waitable, true);
					if (skipped || waitable.CanContinue(_justYielded))
						_bgwaitables.RemoveAt(i--);
				}
			}

			// Waitables
			for (var i = 0; i < _waitables.Count; i++)
			{
				ICoroutineWaitable waitable = _waitables[i];

				bool skipped = HandleSkippables(waitable, true);
				if (skipped || waitable.CanContinue(_justYielded))
					_waitables.RemoveAt(i--);
			}

			// Timer
			_timer = 0;
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class CoroutineInstanceProxy : LuaProxy<CoroutineInstance>
		{
			[CanBeNull]
			public ManagedWaitableGroup catchup_bg()
			{
				if (proxy._bgwaitables == null) return null;
				var ret = new ManagedWaitableGroup(proxy._bgwaitables);
				ret.catchup = true;
				proxy._bgwaitables.Clear();
				return ret;
			}

			public void add_bg_waitable([CanBeNull] ICoroutineWaitable waitable)
			{
				if (waitable == null) return;

				proxy._bgwaitables = proxy._bgwaitables ?? new List<ICoroutineWaitable>();
				proxy._bgwaitables.Add(waitable);
			}

			public void skip()
			{
				proxy.skipEverything = true;
			}

			public void cancel() => proxy.Cancel();
		}
	}
}