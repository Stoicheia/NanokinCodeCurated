using System.Collections.Generic;
using System.IO;
using System.Text;
using Combat.Components;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;
using Newtonsoft.Json;
using Util.Network;

namespace Combat.Toolkit
{
	public class InteropChip : Chip
	{
		private readonly List<CoreInstruction> _allInstructions    = new List<CoreInstruction>();
		private readonly List<CoreInstruction> _instructionsToSend = new List<CoreInstruction>();

		private Connector _connector;
		private Session   _session;
		private bool      _isClearOnNext;

		public InteropChip()
		{
			CreateServer();
		}

		public override UniTask ExecuteAsync(CoreInstruction ins)
		{
			_instructionsToSend.Add(ins);
			_allInstructions.Add(ins);

			TryPush();

			return UniTask.CompletedTask;
		}

		private void CreateServer()
		{
			_connector = new Connector("127.0.0.1", 30700);
			_connector.Connect();
			_connector.OnConnected += sesh =>
			{
				_session = sesh;
				_session.Diconnected += () =>
				{
					_session = null;
				};
				SendAll(_session);
				TryPush();
			};
		}

		private void SendAll(Session sesh)
		{
			_isClearOnNext = true;
			foreach (CoreInstruction msg in _allInstructions)
			{
				_instructionsToSend.Add(msg);
			}
		}

		/// <summary>
		/// Tries to push messages to the debugger.
		/// If there is no session established, attempt to connect. (if the connector is ready)
		/// </summary>
		private void TryPush()
		{
			if (_session != null)
			{
				if (_isClearOnNext)
				{
					_isClearOnNext = false;
					_session.Send(new PacketWriter(0));
				}

				foreach (CoreInstruction message in _instructionsToSend)
				{
					SendMessage(message);
				}

				_instructionsToSend.Clear();
			}
			else
			{
				if (_connector.isReady)
					_connector.Connect();
			}
		}

		private void SendMessage(CoreInstruction msg)
		{
			if (_session == null)
				return;

			var sb = new StringBuilder();
			var sw = new StringWriter(sb);

			using (JsonWriter jsonWriter = new JsonTextWriter(sw))
			{
				jsonWriter.Formatting = Formatting.None;

				jsonWriter.WriteStartArray();
				jsonWriter.WriteStartObject();
				Write(msg.op, msg, jsonWriter);
				jsonWriter.WriteEndObject();

				jsonWriter.WriteEndArray();
			}

			_session.Send(new PacketWriter(1).String(sb.ToString()).GetBytes());
		}

		public virtual void Write(CoreOpcode ins, CoreInstruction data, JsonWriter writer)
		{
			WriteProperties(ins, data, writer);
		}

		protected void Write(CoreOpcode coreOpcode, CoreInstruction data, [NotNull] JsonWriter writer, string msg)
		{
			WriteProperties(coreOpcode, data, writer);

			writer.WritePropertyName("Message");
			writer.WriteValue(msg);
		}

		private void WriteProperties(CoreOpcode coreOpcode, CoreInstruction data, JsonWriter writer)
		{
			// const string PATH_START = "Combat.Messages.Msg.";
			string name = GetType().Name + (ParametersString != null
				? $" ({ParametersString})"
				: "");

			// int id    = core.processor.GetID(instruction);
			// int depth = core.processor.GetDepth(instruction);

			// var type = "unk";
			// switch (this)
			// {
			// 	case Trace.Splitter _:
			// 		type = "splitter";
			// 		break;
			//
			// 	case CoreInstruction _:
			// 		type = "command";
			// 		break;
			//
			// 	case AddonInstruction _:
			// 		type = "signal";
			// 		break;
			// }

			// writer.WritePropertyName("Id");
			// writer.WriteValue(id);
			//
			// writer.WritePropertyName("Msg");
			// writer.WriteValue(name);
			//
			// writer.WritePropertyName("Type");
			// writer.WriteValue(type);
			//
			// writer.WritePropertyName("Depth");
			// writer.WriteValue(depth);
		}

		[CanBeNull]
		public virtual string ParametersString => null;

		[NotNull]
		public Chip Handler => this;
	}
}