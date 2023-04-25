using System;
using System.Collections.Generic;
using Combat.Components.WinLoseHandling;
using Combat.Startup;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Components
{
	public delegate void InstructionHandler(ref CoreInstruction data);

	public delegate UniTask InstructionHandlerAsync(CoreInstruction data);

	public abstract class Chip : IComparable<Chip>
	{
		public Battle battle;
		public BattleRunner  runner;
		public BattleIO    io => runner.io;

		private readonly List<HandlerEntry>      _handlers      = new List<HandlerEntry>();
		private readonly List<HandlerEntryAsync> _handlersAsync = new List<HandlerEntryAsync>();

		protected virtual int Priority => 0;

		public readonly struct HandlerEntry
		{
			public readonly CoreOpcode         opcode;
			public readonly InstructionHandler handler;

			public HandlerEntry(CoreOpcode opcode, InstructionHandler handler)
			{
				this.opcode  = opcode;
				this.handler = handler;
			}
		}

		public readonly struct HandlerEntryAsync
		{
			public readonly CoreOpcode              coreOpcode;
			public readonly InstructionHandlerAsync handler;

			public HandlerEntryAsync(CoreOpcode coreOpcode, InstructionHandlerAsync handler)
			{
				this.coreOpcode = coreOpcode;
				this.handler    = handler;
			}
		}

		protected Chip()
		{
			// ReSharper disable once VirtualMemberCallInConstructor
			RegisterHandlers();
		}

		protected virtual void RegisterHandlers() { }

		public virtual void Install() { }

		public virtual UniTask InstallAsync()
		{
			return UniTask.CompletedTask;
		}

		public virtual void Uninstall() { }

		protected void Handle(CoreOpcode coreOpcode, InstructionHandler handler)
		{
			_handlers.Add(new HandlerEntry(coreOpcode, handler));
		}

		protected void Handle(CoreOpcode coreOpcode, InstructionHandlerAsync handler)
		{
			_handlersAsync.Add(new HandlerEntryAsync(coreOpcode, handler));
		}

		public virtual void Update() { }

		public void Execute(ref CoreInstruction struc)
		{
			for (var i = 0; i < _handlers.Count; i++)
			{
				HandlerEntry entry = _handlers[i];
				if (entry.opcode == struc.op)
				{
					entry.handler(ref struc);
				}
			}
		}

		public virtual async UniTask ExecuteAsync(CoreInstruction struc)
		{
			UniTaskBatch batch = UniTask2.Batch();

			for (var i = 0; i < _handlersAsync.Count; i++)
			{
				HandlerEntryAsync entry = _handlersAsync[i];
				if (entry.coreOpcode == struc.op)
				{
					entry.handler(struc).Batch(batch);
				}
			}

			try
			{
				await batch;
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogException(e);
			}
		}

		public int CompareTo([NotNull] Chip other) => Priority.CompareTo(other.Priority);

		public override string ToString() => GetType().Name;

		public virtual bool CanHandle(CoreInstruction ins)
		{
			return true;
		}
	}
}