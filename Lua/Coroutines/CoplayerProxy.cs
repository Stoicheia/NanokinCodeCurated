using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Anjin.Actors;
using Anjin.Audio;
using Anjin.Cameras;
using Anjin.MP;
using Anjin.Nanokin;
using Anjin.Nanokin.Park;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.UI;
using Anjin.Util;
using Anjin.Utils;
using API.PropertySheet;
using API.Spritesheet.Indexing.Runtime;
using Cinemachine;
using Combat;
using Combat.Components;
using Combat.Data;
using Combat.Data.Decorative;
using Combat.Data.VFXs;
using Combat.Entities;
using Combat.Startup;
using Combat.Toolkit;
using Combat.Toolkit.Camera;
using Combat.UI.Notifications;
using Core.Manageds;
using Cysharp.Threading.Tasks;
using Data.Shops;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Tags;
using Overworld.UI;
using SaveFiles;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Playables;
using Util;
using Util.Animation;
using Util.Components.Cinemachine;
using Util.RenderingElements.Trails;
using Util.UniTween.Value;
using Vexe.Runtime.Extensions;
using ActorDirections = Overworld.Cutscenes.DirectedActor.Directions;
using Debug = UnityEngine.Debug;
using Trigger = Anjin.Nanokin.Map.Trigger;

namespace Overworld.Cutscenes
{
	public partial class Coplayer
	{
		// We keep CoroutinePlayerProxy inside of CoroutinePlayer so we can access private fields
		[SuppressMessage("ReSharper", "InconsistentNaming")]
		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class CoplayerProxy : MonoLuaProxy<Coplayer>
		{
			private static readonly string[] IDS_transition_in  = { "transition_in", "in" };
			private static readonly string[] IDS_transition_out = { "transition_out", "out" };

			public static readonly string[] IDS_bust_mode  = { "mode", "bust_mode" };
			public static readonly string[] IDS_bust_state = { "state", "bust_state" };

			public static readonly string[] IDS_bust_state_mouth = { "mouth", "bust_eyes" };
			public static readonly string[] IDS_bust_state_eyes  = { "eyes", "bust_eyes" };
			public static readonly string[] IDS_bust_state_brows = { "brows", "eyebrows", "bust_brows", "bust_eyebrows" };


			private static List<ICoroutineWaitable> _scratchWaitables = new List<ICoroutineWaitable>();
			private static List<FrameBinding>       _scratchFrames    = new List<FrameBinding>();

			[DebuggerHidden]
			public State costate => proxy.state;

			[DebuggerHidden]
			public int choice_result => proxy.state.choiceResult;

			[DebuggerHidden]
			public BattleOutcome battle_outcome => proxy.state.combatOutcome;

			public bool no_outgoing_blend { get => proxy.state.noOutgoingBlend; set => proxy.state.noOutgoingBlend = value; }

			public const int option_auto   = 0;
			public const int option_manual = 1;

			private List<string> choiceTexts   = new List<string>();
			private List<bool>   cancelChoices = new List<bool>();

			public void stop()
			{
				proxy.Stop();
			}

			public WaitableCoroutineInstance start_subroutine(Closure func) => proxy.NewSubroutine(func);

			/*if(args.Type == DataType.Table)
				else {

				}*/
			public ICoroutineWaitable wait_subroutines()
			{
				if (proxy.subroutines.Count <= 0) return null;

				foreach (CoroutineInstance sub in proxy.subroutines)
				{
					if (!sub.Ended)
						_scratchWaitables.Add(new WaitableCoroutineInstance(sub));
				}

				return FlushScratchWaitable();
			}


			public bool is_skipping() => proxy.Skipping;

			public void begin_skip()
			{
				proxy.StartSkipping();
			}

			public void end_skip()
			{
				proxy.StopSkipping();
			}

			public void end_with(Closure func)
			{
				proxy.StopWith(func);
			}

			public void control_game(bool state = true)
			{
				proxy.ControlGame(state);
			}

			public void control_cam(bool state = true)
			{
				proxy.ControlCamera(state);
			}

			public void set_graph(Table graph)
			{
				proxy.state.graph = graph;
			}

			public bool has_flag(string flag) => proxy.animflags.Contains(flag);

			public bool skips_flag(string flag) => proxy.skipflags.Contains(flag);


			public void set_script_tags(List<string> tags) => proxy.memberTags = tags;

			public void activate_script_tags()
			{
				for (var i = 0; i < proxy.memberTags.Count; i++)
					TagController.ActivateAll(proxy.memberTags[i]);
			}

			public void deactivate_script_tags()
			{
				for (var i = 0; i < proxy.memberTags.Count; i++)
					TagController.DeactivateAll(proxy.memberTags[i]);
			}

			// Could be moved elsewhere?
			public void disable_interact([NotNull] GameObject go)
			{
				if (go.TryGetComponent(out Interactable interactable))
				{
					interactable.locks++;
				}
			}

			// Could be moved elsewhere?
			public void enable_interact([NotNull] GameObject go)
			{
				if (go.TryGetComponent(out Interactable interactable))
				{
					interactable.locks--;
				}
			}


		#region Using Objects

			public object use_self()
			{
				if (proxy.script.TryGet("self_actor", out Actor actor))
					return proxy.UseMember(new DirectedActor(actor));
				else if (proxy.sourceObject && proxy.sourceObject.TryGetComponent(out Actor selfObjectActor))
					return proxy.UseMember(new DirectedActor(selfObjectActor));

				return proxy.script.Get("self_actor");
			}

			[NotNull]
			public DirectedActor use_player(Table options)
			{
				Actor npcActor = ActorController.playerActor;
				var   actor    = new DirectedActor(npcActor, options);
				proxy.UseMember(actor);
				return actor;
			}

			[NotNull]
			public object use_object(string addr, Table options)
			{
				var obj = new DirectedObject(addr, options);
				proxy.UseMember(obj);
				if (proxy.Running) return new WaitableMemberLoad(obj);
				return obj;
			}

			[NotNull]
			public object get_actor(string ref_path, Table options)
			{
				var actor = new DirectedActor(ref_path, options);
				proxy.UseMember(actor);
				if (proxy.Running) return new WaitableMemberLoad(actor);
				return actor;
			}

			[NotNull]
			public object get_actor([NotNull] DirectedActor actor, Table options)
			{
				actor.ReadOptions(options);
				proxy.UseMember(actor);
				if (proxy.Running) return new WaitableMemberLoad(actor);
				return actor;
			}

			[NotNull]
			public object use_actor(string ref_path, Table options)
			{
				var actor = new DirectedActor(ref_path, options);
				proxy.UseMember(actor);
				if (proxy.Running) return new WaitableMemberLoad(actor);
				return actor;
			}

			[NotNull]
			public object use_actor([NotNull] DirectedActor actor, Table options)
			{
				actor.ReadOptions(options);
				proxy.UseMember(actor);
				if (proxy.Running) return new WaitableMemberLoad(actor);
				return actor;
			}

			[NotNull]
			public DirectedActor use_existing_actor(Actor existing, Table options)
			{
				var actor = new DirectedActor(existing, options);
				proxy.UseMember(actor);
				return actor;
			}

			[NotNull]
			public object use_existing_actor([NotNull] List<Actor> existing, Table options)
			{
				var list = new List<DirectedActor>();
				foreach (Actor _actor in existing)
				{
					DirectedActor actor = new DirectedActor(_actor, options);
					proxy.UseMember(actor);
					list.Add(actor);
				}

				return list;
			}

			[NotNull]
			public object use_guest_npcs(int number, Table options)
			{
				var npcs = new List<DirectedActor>();
				for (int i = 0; i < number; i++)
				{
					NPCActor guest = ActorController.RentGuest();
					guest.designer.SetSpritesUsingTable(options);

					var actor = new DirectedActor(guest, options);
					actor.guest = true;
					proxy.UseMember(actor);
					actor.OnStart(proxy, false);

					npcs.Add(actor);
				}

				return npcs;
			}

			[NotNull]
			public DirectedActor use_guest_npc(Table options)
			{
				NPCActor guest = ActorController.RentGuest();
				guest.designer.SetSpritesUsingTable(options);

				// Note: this was not used anywhere in the game, and I couldn't figure out what this was for
				// if (options.TryGet("ignore_gametags", out bool ignore) && ignore)
				// {
				// 	Layer layer = guest.GetOrAddComponent<Layer>();
				// 	layer.Options = ActivatorOptions.OverrideGametags | ActivatorOptions.ForbidUpdateDuringCutscene;
				// }

				var actor = new DirectedActor(guest, options);
				actor.guest = true;
				proxy.UseMember(actor);
				actor.OnStart(proxy, false);
				return actor;
			}

			public void add_resource([NotNull] DynValue val)
			{
				if (val.Type != DataType.UserData) return;

				if (val.UserData.TryGet(out CinemachineVirtualCamera cam))
					proxy.AddResource(cam);
			}

			public void add_resource(string id, [NotNull] DynValue val)
			{
				if (val.Type != DataType.UserData) return;

				if (val.UserData.TryGet(out CinemachineVirtualCamera cam))
					proxy.AddResource(id, cam);
			}

		#endregion


		#region Gameplay

			public object wait_trigger(DynValue v1)
			{
				if (v1.AsUserdata(out Trigger trigger))
				{
					return new WaitableTrigger(trigger);
				}

				return null;
			}

		#endregion

		#region Bubbles

			public void set_actor_bubble([NotNull] List<DirectedActor> actors, Table settings)
			{
				foreach (DirectedActor actor in actors)
					actor.bubbleSettings = settings;
			}

			// BUBBLE
			// ------------------------------------------------------------


			[CanBeNull]
			public object say_hud(DynValue obj, DynValue text, Table settings = null)
			{
				if (proxy.Skipping) return null;

				Character character = Character.None;
				string    char_name = null;
				bool      is_name   = false;

				/*string    bust_state = null;
				string    bust_mode  = null;

				string    bust_eyes  = null;
				string    bust_mouth  = null;
				string    bust_brows  = null;*/

				BustAnimConfig anim = BustAnimConfig.Default;

				if (obj.AsUserdata(out DirectedActor actor) && actor.actor != null)
				{
					if (!actor.name.IsNullOrWhitespace())
						char_name = actor.name;
					if (!actor.actor.CharacterName.IsNullOrWhitespace())
						char_name = actor.actor.CharacterName;
					else
						character = actor.actor.Character;
				}
				else if (obj.AsUserdata(out Character c))
				{
					character = c;
				}
				else if (obj.AsString(out string name))
				{
					char_name = name;
				}
				else
				{
					char_name = DialogueTextbox.DEFAULT_NAME;
				}

				DialogueOptions? options = null;
				if (settings != null)
				{
					DialogueOptions opt = DialogueOptions.Default;
					opt.FillFromTable(settings);
					options = opt;
				}

				DialogueTextbox texbox = CutsceneHUD.Live.MainTextbox;
				texbox.SuppressTestBust = false;
				if (settings.TryGet("busts_on", out bool b))
				{
					texbox.SuppressTestBust = !b;
				}

				if (settings != null && settings.TryGet("reversed", out bool _))
					texbox = CutsceneHUD.Live.MainTextboxAlt;

				if (settings.TryGet(IDS_bust_mode, out string _mode)) anim.mode                = _mode;
				if (settings.TryGet(IDS_bust_state, out string _state)) anim.state             = _state;
				if (settings.TryGet(IDS_bust_state_eyes, out string _eyes)) anim.state_eyes    = _eyes;
				if (settings.TryGet(IDS_bust_state_mouth, out string _mouth)) anim.state_mouth = _mouth;
				if (settings.TryGet(IDS_bust_state_brows, out string _brows)) anim.state_brows = _brows;


				if (text.Type == DataType.String)
				{
					texbox.Show(character, text.String, options, anim, proxy, char_name);
				}
				else if (text.Type == DataType.Table)
				{
					texbox.Show(character, text.Table.Values.Select(x => new GameText(x.String)).ToList(), options, anim, proxy, char_name);
				}

				return new ManagedActivatableWithTransitions(texbox);
			}

			public object say_sub(DynValue obj, DynValue text, Table settings = null)
			{
				if (proxy.Skipping) return null;

				Character character = Character.None;
				string    char_name = null;
				bool      is_name   = false;


				if (obj.AsUserdata(out DirectedActor actor))
				{
					if (!actor.name.IsNullOrWhitespace())
						char_name = actor.name;
					if (!actor.actor.CharacterName.IsNullOrWhitespace())
						char_name = actor.actor.CharacterName;
					else
						character = actor.actor.Character;
				}
				else if (obj.AsUserdata(out Character c))
					character = c;
				else if (obj.AsString(out string name))
					char_name = name;
				else
					char_name = DialogueTextbox.DEFAULT_NAME;

				DialogueOptions? options = null;
				if (settings != null)
				{
					DialogueOptions opt = DialogueOptions.Default;
					opt.FillFromTable(settings);
					options = opt;
				}

				DialogueTextbox texbox = CutsceneHUD.Live.SubtitleBoxTextbox;
				if (settings != null && settings.TryGet("no_box", out bool _))
					texbox = CutsceneHUD.Live.SubtitleTextbox;

				if (text.Type == DataType.String)
					texbox.Show(character, text.String, options, null, proxy, char_name);
				else if (text.Type == DataType.Table)
					texbox.Show(character, text.Table.Values.Select(x => new GameText(x.String)).ToList(), options, null, proxy, char_name);

				return new ManagedActivatableWithTransitions(texbox);
			}

			public object say_bubble([NotNull] List<DirectedActor> actors, DynValue text, float seconds, Table bubble_settings)
			{
				foreach (DirectedActor actor in actors)
				{
					int id = (actor, "speech").GetHashCode();
					proxy.ReplaceManaged(id, new ManagedSpeechBubble(actor, text, seconds, bubble_settings));
				}

				return CollectNewManageds();
			}

			public object say_bubble([NotNull] List<DirectedActor> actors, DynValue text, Table bubble_settings)
			{
				foreach (DirectedActor actor in actors)
				{
					int id = (actor, "speech").GetHashCode();
					proxy.ReplaceManaged(id, new ManagedSpeechBubble(actor, text, null, bubble_settings));
				}

				return CollectNewManageds();
			}

			[NotNull]
			public object say_ambient(DynValue obj, [NotNull] DynValue text, Table settings)
			{
				Character character = Character.None;

				if (obj.AsUserdata(out DirectedActor actor))
				{
					character = actor.actor.Character;
				}
				else if (obj.AsUserdata(out Character c))
				{
					character = c;
				}

				DialogueOptions? options = null;
				if (settings != null && settings.Length > 0)
				{
					options = DialogueOptions.Default;
					options.Value.FillFromTable(settings);
				}

				if (text.Type == DataType.String)
					PartyChatHUD.Textbox.Show(character, text.String, options);
				else if (text.Type == DataType.Table)
					PartyChatHUD.Textbox.Show(character, text.Table.Values.Select(x => new GameText(x.String)).ToList(), options);

				//return proxy.RegisterManaged(new ManagedDialogueTextbox(PartyChatHUD.Textbox));
				return new ManagedActivatableWithTransitions(PartyChatHUD.Textbox);
			}

			[NotNull]
			// TODO: This API needs a refactor.
			public WaitableChoiceHUD say_choices(DirectedActor actor, [NotNull] DynValue dynText, Table settings)
			{
				// TODO:
				//var text = dynText.Table.Values.Select(x => new GameText(x.String)).ToList();
				//var text = dynText.Table.Values.Select(x => x.String).ToList();

				choiceTexts.Clear();
				cancelChoices.Clear();

				LinkedList<TablePair> mainPairs = dynText.Table.Pairs;
				List<TablePair>       innerPairs;

				foreach (TablePair mainPair in mainPairs)
				{
					if (mainPair.Key.Type != DataType.Number)
						continue;

					if (mainPair.Value.Type == DataType.String)
					{
						choiceTexts.Add(mainPair.Value.String);
					}
					else if (mainPair.Value.Type == DataType.Table)
					{
						innerPairs = mainPair.Value.Table.Pairs.ToList();

						choiceTexts.Add(innerPairs[0].Value.String);
						cancelChoices.Add(innerPairs[1].Value.Boolean);
					}
				}

				/*var text    = dynText.Table.Pairs.Select(x => x.Value.String).ToList();
				var cancels = dynText.Table.Pairs.Select(x => x.Value.Boolean).ToList();*/

				CutsceneHUD.Live.ChoiceTextbox.Show(choiceTexts, cancelChoices, i => proxy.state.choiceResult = i);

				/*GameHUD.Live.choiceBubble.Show(text, 0, i => proxy.state.choiceResult = i);
				GameHUD.Live.choiceBubble.hudElement.SetPositionModeWorldPoint(new WorldPoint(actor.actor.gameObject), Vector3.up * 1.5f);
				GameHUD.Live.choiceBubble.ApplySettings(settings);*/

				return new WaitableChoiceHUD(CutsceneHUD.Live.ChoiceTextbox);
			}

			public ICoroutineWaitable react([NotNull] List<DirectedActor> actors, string type, float duration)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					SpritePopup result = actor.actor.Reaction(type, duration);
					if (result)
					{
						_scratchWaitables.Add(new WaitableSpritePopup(result));
					}
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable show_bust(Character character, Table options = null)
			{
				CutsceneHUD.Live.StandaloneBustManager.AddBust(character, CharacterBustManager.BustOptions.FromTable(options));
				CutsceneHUD.Live.StandaloneBustManager.Show();
				return new ManagedActivatableWithTransitions(CutsceneHUD.Live.StandaloneBustManager);
			}

			public ICoroutineWaitable show_bust(Table table)
			{
				CharacterBustManager.Options options = CharacterBustManager.Options.Default;

				table.TryGet("duration", out options.duration, options.duration);
				table.TryGet(IDS_transition_in, out options.transition_in, options.transition_in);
				table.TryGet(IDS_transition_out, out options.transition_out, options.transition_out);

				foreach (TablePair pair in table.Pairs)
				{
					if (pair.IsIndex() && pair.Value.AsFloat(out float _duration))
					{
						options.duration = _duration;
					}

					if (pair.Value.AsTable(out var tbl))
					{
						Character character = Character.None;
						tbl.TryGet("character", out character, character);

						foreach (TablePair p in tbl.Pairs)
						{
							if (p.IsIndex() && p.Value.AsUserdata(out Character _c))
								character = _c;
						}

						CutsceneHUD.Live.StandaloneBustManager.AddBust(character, CharacterBustManager.BustOptions.FromTable(tbl));
					}
				}

				CutsceneHUD.Live.StandaloneBustManager.Show(options);

				return new ManagedActivatableWithTransitions(CutsceneHUD.Live.StandaloneBustManager);
			}

		#endregion

		#region Shop

			public object open_shop(Table tbl) => open_shop(null, Shop.new_shop(tbl));

			public object open_shop(Shop shop) => open_shop(null, shop);

			public object open_shop(Shop shop, Transform npc) => open_shop(npc, shop);

			public object open_shop(Transform npc, Shop shop)
			{
				if (npc == null && proxy.sourceObject != null)
				{
					npc = proxy.sourceObject.transform;
				}

				var managed = new ManagedShop(shop, npc);
				proxy.ReplaceManaged("shop", managed);
				return CollectNewManageds();
			}

		#endregion

		#region Splash Screens

			public ICoroutineWaitable splash_screen(string address)
			{
				if (SplashScreens.IsShowing) return null;
				SplashScreens.ShowPrefab(address);
				return SplashScreens.Live;
			}

			public void splash_screen_hide() => SplashScreens.Hide();

		#endregion

		#region Look

			public ICoroutineWaitable look_at(DynValue user, DynValue dynTarget)
			{
				ICoroutineWaitable doItem(DynValue item)
				{
					if (item.AsUserdata(out DirectedActor actor))
					{
						proxy.EnsureControl(actor);
						ref ActorDirections dirs = ref actor.directions;

						switch (dynTarget.Type)
						{
							case DataType.String:
								// If it's a string or table, assume actor reference
								if (proxy.state.graph.TryGet("_asset", out RegionGraph graph))
								{
									RegionObject graphObj = graph.FindByPath(dynTarget.String);
									switch (graphObj)
									{
										case RegionObjectSpatial spatial:
											dirs.LookPosition(spatial.Transform.Position);
											break;
									}
								}

								break;

							case DataType.UserData:
								switch (dynTarget.UserData.Object)
								{
									case Vector3 v:
										dirs.LookPosition(v);
										break;

									case WorldPoint wp:
										dirs.LookWorldPoint(wp);
										break;

									case GameObject go:
										dirs.LookWorldPoint(new WorldPoint(go));
										break;

									case Transform transform:
										dirs.LookWorldPoint(new WorldPoint(transform.gameObject));
										break;

									case DirectedActor actorTarget:
										// Prevent actors from looking at themselves (this wouldn't be useful and it's useful when working in scripts)
										if (actor != actorTarget)
											dirs.LookWorldPoint(new WorldPoint(actorTarget.gameObject));
										break;

									case Actor a:
										if (actor.actor != a)
											dirs.LookWorldPoint(new WorldPoint(a.gameObject));
										break;
								}

								break;
						}

						return new WaitableActorLook(actor);
					}
					else if (item.AsUserdata(out Fighter fighter))
					{
						if (dynTarget.AsVector3(out Vector3 pos))
							fighter.actor.facing = fighter.actor.position.Towards(pos);
					}

					return null;
				}


				if (dynTarget.Type == DataType.Table)
					dynTarget = dynTarget.Table.Get(1);

				if (user.AsTable(out Table tbl))
				{
					foreach (DynValue value in tbl.Values)
					{
						_scratchWaitables.AddIfNotNull(doItem(value));
					}
				}
				else
				{
					_scratchWaitables.AddIfNotNull(doItem(user));
				}

				return _scratchWaitables.Count > 0 ? FlushScratchWaitable() : null;
			}

			/*public void look_at(List<Fighter> fighters, DynValue dynTarget)
			{
				if (!dynTarget.AsVector3(out Vector3 pos))
					return;

				foreach (Fighter fighter in fighters)
				{
				}
			}

			public ICoroutineWaitable look_at(DirectedActor actor, DynValue pos) => look_at(new List<DirectedActor> { actor }, pos);*/

			/*[NotNull]
			public void look_at(Fighter fighter, Vector3 pos)
			{
				fighter.actor.facing = fighter.position.Towards(pos);
			}*/

			public ICoroutineWaitable look_centroid([NotNull] List<DirectedActor> actors)
			{
				var calc = new Centroid();
				foreach (DirectedActor actor in actors)
					calc.add(actor.gameObject.transform.position);

				Vector3 centroid = calc.get();

				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.directions.LookWorldPoint(new WorldPoint(centroid));

					_scratchWaitables.Add(new WaitableActorLook(actor));
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable look_forward([NotNull] List<DirectedActor> actors)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.directions.LookForward();

					_scratchWaitables.Add(new WaitableActorLook(actor));
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable look_backward([NotNull] List<DirectedActor> actors)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.directions.LookBackward();

					_scratchWaitables.Add(new WaitableActorLook(actor));
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable look_dir(DynValue val, Vector3 facing)
			{
				ICoroutineWaitable doItem(DynValue item)
				{
					if (item.AsUserdata(out Fighter fighter))
					{
						fighter.facing = facing;
					}
					else if (item.AsUserdata(out DirectedActor actor))
					{
						proxy.EnsureControl(actor);
						actor.directions.LookDirection(facing);

						return new WaitableActorLook(actor);
					}

					return null;
				}

				if (val.AsTable(out Table tbl))
				{
					foreach (DynValue item in tbl.Values)
					{
						_scratchWaitables.AddIfNotNull(doItem(item));
					}
				}
				else
				{
					_scratchWaitables.AddIfNotNull(doItem(val));
				}

				return _scratchWaitables.Count > 0 ? FlushScratchWaitable() : null;
			}

			/*private void look_dir([NotNull] List<Fighter> fighters, Vector3 facing)
			{
				foreach (Fighter fter in fighters)
				{
					fter.facing = facing;
				}
			}

			private ICoroutineWaitable look_dir([NotNull] List<DirectedActor> actors, Vector3 facing)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.directions.LookDirection(facing);

					_scratchWaitables.Add(new WaitableActorLook(actor));
				}

				return FlushScratchWaitable();
			}*/

			public ICoroutineWaitable look_cam_dir([NotNull] List<DirectedActor> actors, DynValue direction)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);

					if (direction.AsUserdata(out Direction8 dir8))
					{
						actor.directions.LookForward();
					}

					_scratchWaitables.Add(new WaitableActorLook(actor));
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable look_reset([NotNull] List<DirectedActor> actors)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.directions.LookReset();

					_scratchWaitables.Add(new WaitableActorLook(actor));
				}

				return FlushScratchWaitable();
			}

		#endregion

		#region Animation

			[CanBeNull]
			public ICoroutineWaitable override_actor_anim([NotNull] List<DirectedActor> actors, [NotNull] DynValue animation, [NotNull] Table options)
			{
				if (is_skipping()) return null;

				const string ID_INDEX_START = "index_start";
				const string ID_INDEX_END   = "index_end";

				foreach (DirectedActor actor in actors)
				{
					WaitableSpriteAnimation waitable = null;
					SpriteAnim              sheet    = actor.gameObject.GetComponentInChildren<SpriteAnim>();
					SpritePlayer            player   = sheet.player;

					options.TryGet("end_pause", out bool end_pause);
					options.TryGet("repeats", out int repeats, 1);

					bool? loops                                        = null;
					if (options.TryGet("loop", out bool _loops)) loops = _loops;

					options.TryGet("speed", out float speed, 1);
					options.TryGet("speed", out AnimationCurve speed_curve);

					if (options.TryGet("pose", out bool pose))
					{
						end_pause = true;
						repeats   = 1;
						loops     = false;
					}

					void OptionsToRenderState(ref RenderState rstate)
					{
						if (options.TryGet("x_flip", out bool xflip))
							rstate.xFlip = xflip;
					}

					bool TryGetAnimNameToBinding(string name, out AnimationBinding binding)
					{
						binding = AnimationBinding.Invalid;

						if (name.FirstOccurance(':', out int index))
						{
							string _anim = name.Substring(0, index);
							string _dir  = name.Substring(index + 1);

							AnimID ID  = AnimUtil.FromString(_anim.Replace(" ", "").Replace("_", "-").ToLower());
							var    dir = DirUtil.FromString(_dir);

							Debug.Log(_anim);
							Debug.Log(_dir);

							Debug.Log(ID);
							Debug.Log(dir);

							if (player.Indexing.GetAnimation(ID, dir, out AnimationBinding _binding))
							{
								binding = _binding;
								return true;
							}
						}

						return false;
					}

					Dictionary<int, FrameModifier> modifiers = null;
					foreach (TablePair pair in options.Pairs)
					{
						var key = pair.Key;
						if (key.Type == DataType.Number)
						{
							int frame    = (int)key.Number - 1;
							var duration = (int)options.Get(key).Number;

							modifiers = modifiers ?? new Dictionary<int, FrameModifier>();
							modifiers[frame] = new FrameModifier
							{
								duration = duration
							};
						}
					}

					proxy.EnsureControl(actor);
					ref ActorDirections dirs = ref actor.directions;
					dirs.overrideAnimEnabled = true;
					dirs.pauseAtEnd          = end_pause;

					// String: 	Just an animation name
					// Table:	An array of custom frames

					if (animation.Type == DataType.String)
					{
						var animName = animation.String;

						if (TryGetAnimNameToBinding(animName, out var binding))
						{
							dirs.overrideAnimState = new RenderState(binding);
							waitable               = new WaitableSpriteAnimation(sheet, binding.ID);
						}
						else
						{
							dirs.overrideAnimState = new RenderState(animName);
							waitable               = new WaitableSpriteAnimation(sheet, animName);
						}

						dirs.overrideAnimState.animRepeats    = repeats;
						dirs.overrideAnimState.loops          = loops;
						dirs.overrideAnimState.frameModifiers = modifiers;
						dirs.overrideAnimState.animSpeed      = speed;
						dirs.overrideAnimState.animSpeedCurve = speed_curve;

						OptionsToRenderState(ref dirs.overrideAnimState);
					}
					else if (animation.Type == DataType.Table)
					{
						Table tbl = animation.Table;

						AnimationSequence sequence = new AnimationSequence();

						{
							tbl.TryGet("speed", out speed, speed);

							if (tbl.TryGet("loop", out _loops))
								loops = _loops;

							if (tbl.TryGet("duration", out float duration, 1))
							{
								sequence.durationFrames = duration * 60;
							}
							else if (tbl.TryGet("duration_frames", out float duration_frames, 1))
							{
								sequence.durationFrames = duration_frames;
							}
						}

						void GetIndexRangeFromTable(ref AnimationBinding _anim_binding, ref AnimationSequence.Animation _anim, Table _index)
						{
							int start = 0;
							int end   = _anim_binding.Length - 1;

							if (_index.TryGet(ID_INDEX_START, out int _start)) start = _start;
							if (_index.TryGet(ID_INDEX_END, out int _end)) end       = _end;

							_anim.type     = AnimationSequence.AnimType.Range;
							_anim.frame    = start;
							_anim.endFrame = end;
						}

						void AddTableToSeq(Table _frame)
						{
							if (_frame == null) return;

							_frame.TryGet("anim", out string anim_name, "");
							_frame.TryGet("options", out DynValue frame_options, null);

							AnimationSequence.Animation anim = new AnimationSequence.Animation();
							anim.animation      = anim_name;
							anim.durationFrames = null;

							bool has_binding = player.Indexing.GetAnimation(anim_name, out AnimationBinding anim_binding);

							anim.type = AnimationSequence.AnimType.AllFrames;

							if (_frame.TryGet("index", out DynValue _index))
							{
								if (has_binding && _index.AsTable(out Table index_table))
								{
									GetIndexRangeFromTable(ref anim_binding, ref anim, index_table);
								}
								else if (_index.AsInt(out int index_int))
								{
									anim.type  = AnimationSequence.AnimType.SingleFrame;
									anim.frame = index_int;
								}
							}
							else if (has_binding && _frame.ContainsKey(ID_INDEX_START) || _frame.ContainsKey(ID_INDEX_END))
							{
								GetIndexRangeFromTable(ref anim_binding, ref anim, _frame);
							}

							/*else if ()
							{
								if ()
								{
									/*int start = 0;
									int end   = anim_binding.Length - 1;

									if (index_table.TryGet("index_start", out int _start)) start = _start;
									if (index_table.TryGet("index_end",   out int _end)) end     = _end;

									anim.type     = AnimationSequence.AnimType.Range;
									anim.frame    = start;
									anim.endFrame = end;#1#
								}
								else if ()
								{
								}
							}*/

							if (_frame.TryGet("duration", out float duration, 1))
							{
								anim.durationFrames = duration * 60;
							}
							else if (_frame.TryGet("duration_frames", out float duration_frames, 1))
							{
								anim.durationFrames = duration_frames;
							}

							sequence.Add(anim);
						}

						void AddStringToSeq(string str) => sequence.AddFullAnimation(str);


						bool is_nested = false;

						// If we find any indexed tables, we assume it's a list of tables.
						// Otherwise, it's just one table specifying a frame
						foreach (TablePair entry in tbl.Pairs)
						{
							if (entry.Key.Type == DataType.Number && (entry.Value.Type == DataType.Table || entry.Value.Type == DataType.String))
							{
								is_nested = true;
								continue;
							}
						}


						_scratchFrames.Clear();

						if (is_nested)
						{
							foreach (TablePair entry in tbl.Pairs)
							{
								if (entry.Key.Type == DataType.Number)
								{
									if (entry.Value.Type == DataType.Table)
										AddTableToSeq(entry.Value.Table);
									else if (entry.Value.Type == DataType.String)
										AddStringToSeq(entry.Value.String);
								}
							}
						}
						else
						{
							AddTableToSeq(tbl);
						}

						AnimationBinding binding = sequence.ToBinding(player.Indexing);

						dirs.overrideAnimState = new RenderState(binding)
						{
							animRepeats    = repeats,
							loops          = loops,
							frameModifiers = modifiers,
							animSpeed      = speed,
							animSpeedCurve = speed_curve,
							animPercent    = -1
						};

						OptionsToRenderState(ref dirs.overrideAnimState);

						waitable = new WaitableSpriteAnimation(sheet, binding.ID);
					}

					actor.directions = dirs;

					if (waitable != null)
						_scratchWaitables.Add(waitable);
				}

				return FlushScratchWaitable();
			}

			public void release_actor_anim([NotNull] List<DirectedActor> actors)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.directions.overrideAnimEnabled = false;
				}
			}

			public void idle_anim([NotNull] List<DirectedActor> actors, string name)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.idleAnimation = name;
				}
			}

			public void jump_anim([NotNull] List<DirectedActor> actors, string name)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.jumpAnimation = name;
				}
			}

			public void walk_anim([NotNull] List<DirectedActor> actors, string anim)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.walkAnimation = anim;
				}
			}

			public ICoroutineWaitable hop([NotNull] List<GameObject> objects, int count, float speed = 0.17f, float height = 0.5f, [CanBeNull] DynValue options = null)
			{
				AnimationCurve curve = null;

				AudioDef? jumpSFX = null;
				AudioDef? landSFX = null;

				if (options != null && options.Type == DataType.Table)
				{
					var opt = options.Table;
					opt.TryGet("curve", out curve);

					if (opt.ContainsKey("jump"))
					{
						DynValue dvJump             = opt.Get("jump");
						if (dvJump.IsNil()) jumpSFX = AudioDef.None;
					}

					if (opt.ContainsKey("land"))
					{
						DynValue dvLand             = opt.Get("land");
						if (dvLand.IsNil()) landSFX = AudioDef.None;
					}

					if (opt.TryGet("no_sfx", out DynValue _no))
					{
						jumpSFX = AudioDef.None;
						landSFX = AudioDef.None;
					}
				}


				foreach (GameObject obj in objects)
				{
					VFXManager vfxs = obj.GetComponentInChildren<VFXManager>();
					var        vfx  = new HopVFX(count, speed, height, curve, jumpSFX, landSFX);
					vfxs.Add(vfx);

					_scratchWaitables.Add(new Anjin.Scripting.Waitables.ManagedVFX(vfx));
				}

				return FlushScratchWaitable();
			}

			/// <summary>
			/// Play a shake effect
			/// </summary>
			public object shake([NotNull] List<GameObject> objects, float duration, float amplitude, float speed, float randomness)
			{
				foreach (GameObject obj in objects)
				{
					proxy.RegisterManaged(new ManagedVFX(obj, new ShakeVFX
					{
						duration   = duration,
						amplitude  = amplitude,
						speed      = speed,
						randomness = randomness
					}));
				}

				return CollectNewManageds();
			}

			public object shake_on([NotNull] List<GameObject> objects, float amplitude, float speed, float randomness)
			{
				foreach (GameObject obj in objects)
				{
					proxy.RegisterManaged(new ManagedVFX(obj, new ShakeVFX
					{
						duration   = float.MaxValue,
						amplitude  = amplitude,
						speed      = speed,
						randomness = randomness
					}));
				}

				return CollectNewManageds();
			}

			public void shake_off([NotNull] List<GameObject> objects)
			{
				foreach (GameObject obj in objects)
				{
					var manager = obj.GetComponentInChildren<VFXManager>();
					if (manager != null)
					{
						foreach (VFX vfx in manager.all)
						{
							if (vfx is ShakeVFX shake)
								shake.removalStaged = true;
						}
					}

					/*proxy.RegisterManaged(new ManagedVFX(obj, new ShakeVFX
					{
						duration   = float.MaxValue,
						amplitude  = amplitude,
						speed      = speed,
						randomness = randomness
					}));*/
				}
			}

			/// <summary>
			/// Play a puppet animation on the
			/// </summary>
			[NotNull] public CoroutinePuppetAnim play(GameObject go, PuppetAnimation animation, [CanBeNull] Table conf = null)
			{
				var ret = new CoroutinePuppetAnim(go, animation, conf);
				proxy.ReplaceManaged((go, "anim"), ret);
				return ret;
			}

			[NotNull] public CoroutinePuppetAnim play_through(GameObject go, PuppetAnimation animation, [CanBeNull] Table conf = null)
			{
				var ret = new CoroutinePuppetAnim(go, animation, conf);
				ret.NoStop = true;
				proxy.ReplaceManaged((go, "anim"), ret);
				return ret;
			}

			[NotNull] public CoroutinePuppetAnim play_with_markers(GameObject go, PuppetAnimation animation, [CanBeNull] Table conf = null)
			{
				var ret = new CoroutinePuppetAnim(go, animation, conf);
				proxy.ReplaceManaged((go, "anim"), ret);
				return ret;
			}

			/// <summary>
			/// Play a named animation on an animable object.
			/// </summary>
			public void play([NotNull] GameObject go, string anim, [CanBeNull] Table conf = null)
			{
				INameAnimable nameAnimable;

				if (!go.TryGetComponent(out nameAnimable))
				{
					if (go.TryGetComponent<GenericFighterActor>(out var generic))
						nameAnimable = generic.Animable;
				}

				if (nameAnimable is SpriteAnim sprite)
				{
					/*var binding = new RenderState(binding)
					{
						animRepeats    = repeats,
						loops          = loops,
						frameModifiers = modifiers,
						animSpeed      = speed,
						animSpeedCurve = speed_curve,
						animPercent    = -1,
						xFlip          = xflip,
					};*/

					sprite.Play(new AnimationBinding(anim, false, 0));
				}
				else if (nameAnimable != null)
				{
					proxy.StopAndRemoveManaged((go, "anim"));
					nameAnimable.Play(anim, PlayOptions.ForceReset);
				}
			}

			/// <summary>
			/// Play either an animation or a puppet anim
			/// </summary>
			/// <param name="conf"></param>
			/// <returns></returns>
			/// <exception cref="ArgumentException"></exception>
			[CanBeNull]
			public CoroutineManaged play(GameObject go, Table conf)
			{
				if (conf.TryGet("anim", out PuppetAnimation animp))
				{
					return play(go, animp, conf);
				}

				if (conf.TryGet("anim", out string animn))
				{
					play(go, animn, conf);
					return null;
				}

				throw new ArgumentException($"Unknown anim in Play conf table! anim={conf.Get("anim")}");
			}

			[CanBeNull]
			public CoroutineManaged play_through(GameObject go, Table conf)
			{
				if (conf.TryGet("anim", out PuppetAnimation animp))
				{
					return play_through(go, animp, conf);
				}

				throw new ArgumentException($"Unknown anim in Play conf table! anim={conf.Get("anim")}");
			}

			[CanBeNull]
			public CoroutineManaged play_with_markers(GameObject go, Table conf)
			{
				if (conf.TryGet("anim", out PuppetAnimation animp))
				{
					return play_with_markers(go, animp, conf);
				}

				throw new ArgumentException($"Unknown anim in Play conf table! anim={conf.Get("anim")}");
			}

			public void replace_spritesheet(GameObject go, string limbName, API.Spritesheet.Indexing.IndexedSpritesheetAsset asset)
			{
				if (go.TryGetComponent(out MultiSpritePuppet puppet))
				{
					puppet.ReplaceSpritesheet(limbName, asset);
				}
			}

			/// <summary>
			/// Do our best to visually offset a thing
			/// </summary>
			/// <param name="v1"></param>
			/// <param name="v2"></param>
			public void offset(DynValue v1, DynValue v2)
			{
				/*
				 * (DirectedActor, Vector3)
				 *
				 */

				if (v1.AsUserdata(out DirectedActor directed) && v2.AsUserdata(out Vector3 vec3))
				{
					proxy.EnsureControl(directed);
					directed.directions.overrideAnimState.offset = vec3;
				}
			}

			public void offset_reset(DynValue v1)
			{
				if (v1.AsUserdata(out DirectedActor directed))
				{
					proxy.EnsureControl(directed);
					directed.directions.overrideAnimState.offset = Vector3.zero;
				}
			}

		#endregion

		#region Visual Effects

			/// <summary>
			/// Play a vfx on a gameobject.
			/// </summary>
			public object vfx(GameObject onto, VFX vfx)
			{
				// Note:
				// We dont allow for list of gameobjects because using the same
				// vfx can be pretty broken.

				proxy.RegisterManaged(new ManagedVFX(onto, vfx));
				return CollectNewManageds();
			}

			/// <summary>
			/// Emit particles as a VFX on an object.
			/// </summary>
			[NotNull] public object vfx(GameObject onto, string particles) => vfx(onto, new FXVFX(particles));

			[NotNull]
			public WaitableParticleSystem particles([NotNull] ParticleSystem psystem)
			{
				psystem.Play();
				return new WaitableParticleSystem(psystem);
			}

			public void particles([NotNull] GameObject root)
			{
				ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>();
				foreach (ParticleSystem particle in particles)
				{
					particle.Play();
				}
			}

			public void particles([NotNull] List<GameObject> objects, string addr)
			{
				foreach (GameObject o in objects)
					vfx(o, new FXVFX(new FX
					{
						address = addr,
						onto    = o
					}));

				FlushScratchWaitable();
			}

			public object react_frames([NotNull] List<GameObject> objects, Table table)
			{
				foreach (GameObject go in objects)
				{
					proxy.RegisterManaged(new ManagedVFX(go, CombatAPI.ReadReactVFX(table)));
				}

				return CollectNewManageds();
			}

			public object blink_frames([NotNull] List<GameObject> objects, Table table)
			{
				foreach (GameObject go in objects)
				{
					proxy.RegisterManaged(new ManagedVFX(go, CombatAPI.ReadBlinkVFX(table)));
				}

				return CollectNewManageds();
			}

			public object flash_color_frames([NotNull] List<GameObject> objects, Table table)
			{
				foreach (GameObject go in objects)
				{
					proxy.RegisterManaged(new ManagedVFX(go, CombatAPI.ReadFlashColorVFX(table)));
				}

				return CollectNewManageds();
			}

			[NotNull]
			public ICoroutineWaitable freeze_frames(int frames)
			{
				GameObject go = new GameObject();

				bool is_overdrive = proxy.animflags.Contains(AnimFlags.Overdrive);

				FreezeFrameVolume freezeFrameVolume = go.AddComponent<FreezeFrameVolume>();
				freezeFrameVolume.DurationFrames = (int)(frames * (!is_overdrive ? 1f : 1.075f));
				freezeFrameVolume.SetGlobal();

				return new WaitableFreezeFrame(freezeFrameVolume);
			}

			public object trail(List<GameObject> objects, [NotNull] DynValue dv)
			{
				TrailSettings settings = ScriptableObject.CreateInstance<TrailSettings>();

				if (dv.AsTable(out Table tbl))
				{
					settings.ImageCount = tbl.TryGet("n", 0);
				}
				else if (dv.AsFloat(out float n))
				{
					settings.ImageCount = (int)n;
				}
				else if (dv.AsBool(out bool enable))
				{
					if (!enable)
					{
						foreach (GameObject o in objects)
						{
							// NOT SURE about this hashcode thing! It should work, but GameObject hashcode might hold some surprises...
							int id = (o, "trail").GetHashCode();
							proxy.StopAndRemoveManaged(id);
						}

						return null;
					}
				}
				else
				{
					Debug.LogError("Invalid argument type for trail. Must be a table or number.");
					return null;
				}

				foreach (GameObject o in objects)
				{
					// NOT SURE about this hashcode thing! It should work, but GameObject hashcode might hold some surprises...
					int id = (o, "trail").GetHashCode();

					if (!proxy.RestartManaged(id))
						proxy.RegisterManaged(id, new ManagedTrail(o, settings));
				}

				return CollectNewManageds();
			}

			public object fade(List<GameObject> objects, float duration, float targetOpacity) => fade(objects, new EaserTo(duration, Ease.Linear), targetOpacity);

			public object fade([NotNull] List<GameObject> objects, EaserTo easer, float targetOpacity)
			{
				foreach (GameObject o in objects)
				{
					int id = (o, "fade").GetHashCode();

					if (proxy.GetManaged(id, out ManagedFade fade))
					{
						fade.easer         = easer;
						fade.targetOpacity = targetOpacity;
						fade.Start();
					}
					else
					{
						proxy.RegisterManaged(id, fade = new ManagedFade(o));
					}

					_scratchManageds.Add(fade);
				}

				return CollectNewManageds();
			}

		#endregion

		#region Audio

			[CanBeNull]
			public object sfx([NotNull] DynValue dyndef)
			{
				if (dyndef.IsNil()) return null;

				var sfx = new AudioDef();

				if (dyndef.AsTable(out Table table))
				{
					if (table.TryGet("_asset", out AudioClip clip))
					{
						sfx = clip;
					}

					return proxy.RegisterManaged(new CoroutineSFX(sfx));
				}
				else
				{
					if (dyndef.AsExact(out sfx)) return proxy.RegisterManaged(new CoroutineSFX(sfx));
					if (dyndef.AsExact(out AudioClip clip)) return proxy.RegisterManaged(new CoroutineSFX(clip));
					if (dyndef.AsString(out string addr)) return proxy.RegisterManaged(new CoroutineSFX(addr));
				}


				return null;
			}

			[CanBeNull]
			public object sfx(DynValue dynpos, [NotNull] DynValue dyndef)
			{
				AudioDef sfx;

				// Get position
				if (!dynpos.AsObject(out WorldPoint wp))
					return null;

				// Get sfx
				if (dyndef.IsNil())
					return null;

				if (dyndef.AsString(out string addr))
					// Need to load the handle, ideally it should be loaded ahead of time
					return proxy.RegisterManaged(new CoroutineSFX(addr, wp));

				if (dyndef.AsObject(out sfx)) return proxy.RegisterManaged(new CoroutineSFX(sfx, wp));
				if (dyndef.AsObject(out AudioClip clip)) return proxy.RegisterManaged(new CoroutineSFX(clip, wp));


				return null;
			}

			[CanBeNull]
			public void sfx_through([NotNull] DynValue dyndef)
			{
				if (dyndef.IsNil()) return;

				var sfx = new AudioDef();

				if (dyndef.AsTable(out Table table))
				{
					if (table.TryGet("_asset", out AudioClip clip))
					{
						sfx = clip;
					}

					GameSFX.PlayGlobal(clip);
				}
				else
				{
					if (dyndef.AsExact(out AudioClip clip)) GameSFX.PlayGlobal(clip);
					else if (dyndef.AsExact(out AudioDef def)) GameSFX.PlayGlobal(def);
				}
			}

			[CanBeNull]
			public object sfx_through(DynValue dynpos, [NotNull] DynValue dyndef)
			{
				AudioDef sfx;

				// Get position
				if (!dynpos.AsObject(out WorldPoint wp))
					return null;

				// Get sfx
				if (dyndef.IsNil())
					return null;

				if (dyndef.AsString(out string addr))
					// Need to load the handle, ideally it should be loaded ahead of time
					return proxy.RegisterManaged(new CoroutineSFX(addr, wp).NoStop());

				if (dyndef.AsObject(out sfx)) return proxy.RegisterManaged(new CoroutineSFX(sfx, wp).NoStop());
				if (dyndef.AsObject(out AudioClip clip)) return proxy.RegisterManaged(new CoroutineSFX(clip, wp).NoStop());


				return null;
			}

			[CanBeNull]
			public object audio([NotNull] DynValue d1, DynValue d2, DynValue d3)
			{
				/*
				 * (sound)
				 * (options)
				 * (sound, options)
				 * (position, sound)
				 * (sound, position)
				 * (position, sound, options)
				 */

				bool dv_to_coroutineSFX(DynValue val, out CoroutineSFX sfx)
				{
					sfx = null;
					if (val.AsString(out string addr))
					{
						sfx = new CoroutineSFX(addr);
						return true;
					}

					if (val.AsObject(out AudioDef def))
					{
						sfx = new CoroutineSFX(def);
						return true;
					}

					if (val.AsObject(out AudioClip clip))
					{
						sfx = new CoroutineSFX(clip);
						return true;
					}

					return false;
				}

				bool dv_to_coroutineSFX_wp(DynValue val, WorldPoint wp, out CoroutineSFX sfx)
				{
					sfx = null;
					if (val.AsString(out string addr))
					{
						sfx = new CoroutineSFX(addr, wp);
						return true;
					}

					if (val.AsObject(out AudioDef def))
					{
						sfx = new CoroutineSFX(def, wp);
						return true;
					}

					if (val.AsObject(out AudioClip clip))
					{
						sfx = new CoroutineSFX(clip, wp);
						return true;
					}

					return false;
				}

				CoroutineSFX final(CoroutineSFX _sfx, DynValue _options = null)
				{
					if (_options != null && _options.AsTable(out Table tbl))
					{
						if (tbl.TryGet("loop", out bool _))
							_sfx.sfxState.looping = true;

						if (tbl.TryGet("no_stop", out bool _nostop))
							_sfx.NoStop();
					}

					CoroutineSFX p = proxy.RegisterManaged(_sfx);
					_scratchManageds.Clear();
					return p;
				}

				/*bool dv_to_worldpoint(DynValue val, out WorldPoint wp)
				{
					wp = default;

					if (WorldPoint.GetFromDynValue(val, out wp, WorldPoint.Default))
						return true;

					return false;
				}*/

				bool parse_table(Table table, out CoroutineSFX sfx)
				{
					sfx = null;
					// TODO
					return false;
				}

				//AudioDef sfx;

				if (d1.IsNil() && d2.IsNil() && d3.IsNil())
				{
					return null;
				}

				if (d2.IsNil() && d3.IsNil())
				{
					CoroutineSFX sfx;

					// Single argument
					if (dv_to_coroutineSFX(d1, out sfx)) return final(sfx);
					if (d1.AsTable(out Table tbl) && parse_table(tbl, out sfx)) return final(sfx);
				}
				else if (d3.IsNil())
				{
					CoroutineSFX sfx;
					WorldPoint   wp;

					if (WorldPoint.GetFromDynValue(d1, out wp, WorldPoint.Default) && dv_to_coroutineSFX_wp(d2, wp, out sfx)) return final(sfx);
					if (WorldPoint.GetFromDynValue(d2, out wp, WorldPoint.Default) && dv_to_coroutineSFX_wp(d1, wp, out sfx)) return final(sfx);

					if (dv_to_coroutineSFX(d1, out sfx)) return final(sfx, d2);
				}
				else
				{
					CoroutineSFX sfx;
					WorldPoint   wp;

					if (WorldPoint.GetFromDynValue(d1, out wp, WorldPoint.Default) && dv_to_coroutineSFX_wp(d2, wp, out sfx)) return final(sfx, d3);
					if (WorldPoint.GetFromDynValue(d2, out wp, WorldPoint.Default) && dv_to_coroutineSFX_wp(d1, wp, out sfx)) return final(sfx, d3);

					if (dv_to_coroutineSFX(d1, out sfx)) return final(sfx, d2);
				}

				return null;
			}

			public void stop_music()
			{
				proxy.ClearMusic();

				proxy.state.musicZone = new AudioZone
				{
					Layer    = AudioLayer.Music,
					Priority = 1800,
					MuteAll  = true
				};

				AudioManager.AddZone(proxy.state.musicZone);
			}

			public void stop_sfx([NotNull] AudioSource sound)
			{
				sound.Stop();
			}

			public void resume_music()
			{
				proxy.ClearMusic();
			}

			public void resume_sfx([NotNull] AudioSource sound)
			{
				sound.Play();
			}

			public void music(AudioClip clip, DynValue lerpSpeed)
			{
				proxy.ClearMusic();

				if (clip == null)
					return;

				proxy.state.musicZone          = AudioZone.CreateMusic(clip, lerpSpeed.AsFloat(0.25f));
				proxy.state.musicZone.Priority = 2001;

				AudioManager.AddZone(proxy.state.musicZone);
			}

		#endregion

		#region Move

			/// <summary>
			/// Move to a point with a generic tween-based animator.
			/// </summary>
			/// <returns></returns>
			[NotNull] public CoroutineMove move(GameObject self, DynValue goal, DynValue dvconf)
			{
				var move = new CoroutineMove(self);
				move.SetGoal(goal);
				move.overrideMove = dvconf;
				proxy.RegisterManaged(move);
				return move;
			}

			/// <summary>
			/// Move to a point with a generic tween-based animator.
			/// </summary>
			/// <returns></returns>
			[NotNull] public CoroutineMove movef(GameObject self, DynValue goal, DynValue dvconf)
			{
				var move = new CoroutineMove(self);
				move.options.frontGoal = true;
				move.SetGoal(goal);
				move.overrideMove = dvconf;

				proxy.RegisterManaged(move);
				return move;
			}


			public void walk_dir([NotNull] DirectedActor actor, Vector2 dir, float? speed = null)
			{
				proxy.EnsureControl(actor);
				ref ActorDirections dirs = ref actor.directions;

				dirs.moveMode      = DirectedActor.MoveMode.Direction;
				dirs.moveSpeed     = speed;
				dirs.moveDirection = new Vector3(dir.x, 0, dir.y);
			}

			public void teleport([NotNull] Actor actor, Vector3 pos)
			{
				actor.Teleport(pos);
			}

			public void teleport([NotNull] List<DirectedActor> actors, DynValue pos, Table options)
			{
				foreach (DirectedActor actor in actors)
				{
					switch (pos.Type)
					{
						case DataType.String:
							if (!proxy.state.graph.TryGet("_asset", out RegionGraph graph)) break;

							RegionObject graphObj = graph.FindByPath(pos.String);
							if (graphObj is RegionPath path)
							{
								int index = 0;
								if (options.TryGet("index", out DynValue _index))
								{
									if (_index.AsInt(out int int_index))
										index = int_index;
									else if (_index.AsString(out string str_index))
									{
										if (str_index == "last")
											index = path.Points.Count - 1;
									}
								}

								options.TryGet("follow_mode", out PathFollowMode mode, PathFollowMode.Raw);

								actor.TeleportToPath(path, index, mode);

								/*Vector3 pt = path.GetWorldPoint(index);
								actor.actor.Teleport(pt);*/
							}
							else if (graphObj is RegionObjectSpatial spatial)
							{
								actor.actor.Teleport(spatial.Transform.Position);
								if (options.TryGet("reorient", out bool _))
									actor.actor.Reorient(spatial.Transform.Forward);
							}

							break;
					}
				}
			}

			public void teleport_next([NotNull] List<DirectedActor> actors, int number = 1, Table options = null)
			{
				foreach (DirectedActor actor in actors)
				{
					if (!proxy.state.graph.TryGet("_asset", out RegionGraph graph)) break;

					options.TryGet("follow_mode", out PathFollowMode mode, PathFollowMode.Raw);
					actor.TeleportToNextPathPoint(number, mode);
				}
			}

			public void teleport_prev([NotNull] List<DirectedActor> actors, int number = 1, Table options = null)
			{
				foreach (DirectedActor actor in actors)
				{
					if (!proxy.state.graph.TryGet("_asset", out RegionGraph graph)) break;

					options.TryGet("follow_mode", out PathFollowMode mode, PathFollowMode.Raw);
					actor.TeleportToPrevPathPoint(number, mode);
				}
			}

			public void walk_speed([CanBeNull] List<DirectedActor> actors, float speed)
			{
				if (actors == null) return;

				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.directions.moveSpeed = speed;
				}
			}

			public void move_stop([NotNull] DirectedActor actor)
			{
				proxy.EnsureControl(actor);
				ref ActorDirections dirs = ref actor.directions;

				dirs.moveMode      = DirectedActor.MoveMode.None;
				dirs.moveSpeed     = 0;
				dirs.moveDirection = Vector2.zero;
				dirs.navigating    = false;
			}

			[NotNull]
			public WaitableMove move_next([NotNull] DirectedObject obj)
			{
				obj.splineIndex++;
				obj.directions.moving          = true;
				obj.directions.moveSplineIndex = obj.splineIndex;

				return new WaitableMove(obj.gameObject.transform, obj.spline.transform.TransformPoint(obj.spline.GetSample(obj.splineIndex).location));
			}

			public void walk_stop(List<DirectedActor> actors)
			{
				foreach (var actor in actors)
				{
					actor.EndPathing();
				}
			}

			public ICoroutineWaitable walk_to([NotNull] List<DirectedActor> actors, DynValue target, Table options = null)
			{
				_scratchWaitables.Clear();

				for (var i = 0; i < actors.Count; i++)
				{
					DirectedActor actor = actors[i];
					proxy.EnsureControl(actor);
					ref ActorDirections dirs = ref actor.directions;


					void DoPos(Vector3 v) => actor.WalkToPoint(v);


					if (target.AsUserdata(out Vector3 v3))
					{
						DoPos(v3);
					}
					else if (target.AsUserdata(out Transform trans))
					{
						DoPos(trans.position);
					}
					else if (target.AsUserdata(out GameObject go))
					{
						DoPos(go.transform.position);
					}
					else if (target.AsUserdata(out SpawnPoint spawn))
					{
						dirs.LookDirection(spawn.GetSpawnPointFacing(i));
						DoPos(spawn.GetSpawnPointPosition(i));
					}
					else if (target.AsUserdata(out DirectedActor targetActor))
					{
						float radius = 0.5f; // If we integrate ActorView with actors like Fighters in combat, we can

						options.TryGet("radius", out radius, radius);

						Vector3 pos = targetActor.actor.Position;
						pos -= actor.actor.facing * radius;

						DoPos(pos);
					}
					else if (target.AsUserdata(out RegionObjectSpatial regionObject))
					{
						DoPos(regionObject.Transform.Position);
					}
					else if (target.AsString(out string graphReference))
					{
						if (!proxy.state.graph.TryGet("_asset", out RegionGraph graph))
							return null;

						RegionObject graphObj = graph.FindByPath(graphReference);

						if (graphObj is RegionPath path)
						{
							options.TryGet("index", out int start_index);
							options.TryGet("target_index", out int target_index, start_index);
							options.TryGet("follow_mode", out PathFollowMode mode);

							actor.FollowPath(path, start_index, target_index, mode);

							if (options.TryGet("loop", out bool loop) && loop)
							{
								actor.directions.pathState.loop       = true;
								actor.directions.pathState.loop_count = 0;
							}
						}
						else if (graphObj is RegionObjectSpatial spatial)
						{
							DoPos(spatial.Transform.Position);
						}
						else
						{
							continue;
						}
					}
					else
					{
						continue;
					}

					if (options != null)
					{
						if (options.TryGet("speed", out float speed))
							dirs.moveSpeed = speed;
					}

					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			/*[NotNull]
			public ManagedActorMove walk_to([NotNull] DirectedActor actor, Vector3 pos, [CanBeNull] Table options)
			{
				proxy.EnsureControl(actor);
				ref ActorDirections dirs = ref actor.directions;

				actor.WalkToPoint(pos);

				if (options != null)
				{
					if (options.TryGet("speed", out float speed))
						dirs.moveSpeed = speed;
				}

				return new ManagedActorMove(actor);
			}*/

			/*public ICoroutineWaitable walk_to([NotNull] List<DirectedActor> actors, SpawnPoint spawn, [CanBeNull] Table options)
			{
				_scratchWaitables.Clear();
				for (var i = 0; i < actors.Count; i++)
				{
					DirectedActor actor = actors[i];
					proxy.EnsureControl(actor);
					ref ActorDirections dirs = ref actor.directions;

					dirs.LookDirection(spawn.GetSpawnPointFacing(i));
					actor.WalkToPoint(spawn.GetSpawnPointPosition(i));

					_scratchWaitables.Add(new ManagedActorMove(actor));
					if (options != null)
					{
						if (options.TryGet("speed", out float speed))
							dirs.moveSpeed = speed;
					}
				}

				return FlushScratchWaitable();
			}*/

			/*[NotNull]
			public ManagedActorMove walk_to([NotNull] DirectedActor actor, [NotNull] DirectedActor toActor, Table options)
			{
				const float radius = 0.5f; // If we integrate ActorView with actors like Fighters in combat, we can

				Vector3 pos = toActor.actor.Position;
				pos -= actor.actor.facing * radius;

				return walk_to(actor, pos, options);
			}*/

			//[NotNull] public ManagedActorMove walk_to([NotNull] DirectedActor actor, [NotNull] RegionObjectSpatial graphObj, Table options) => walk_to(actor, graphObj.Transform.Position, options);

			/*[CanBeNull]
			public ManagedActorMove walk_to(DirectedActor actor, string graphPath, Table options)
			{
				if (!proxy.state.graph.TryGet("_asset", out RegionGraph graph))
					return null;

				RegionObject graphObj = graph.FindByPath(graphPath);
				if (graphObj is RegionPath path)
				{
					//actor.regionPath = path;

					options.TryGet("index", out int start_index);
					options.TryGet("target_index", out int target_index, start_index);
					options.TryGet("follow_mode", out PathFollowMode mode);

					actor.FollowPath(path, start_index, target_index, mode);

					if (options.TryGet("loop", out bool loop) && loop)
					{
						actor.directions.pathState.loop       = true;
						actor.directions.pathState.loop_count = 0;
					}

					return new ManagedActorMove(actor);

					/*if (options.TryGet("index", out index))
						actor.regionPathIndex = index;#1#

					/*Vector3 pt = path.GetWorldPoint(start_index);
					return walk_to(actor, pt, null);#1#
				}

				if (graphObj is RegionObjectSpatial spatial)
					return walk_to(actor, spatial, options);

				return null;
			}*/

			/// <summary>
			/// Walk the next N segments of the actor's current path. (default is just 1)
			/// </summary>
			public ICoroutineWaitable walk_next([NotNull] List<DirectedActor> actors, int count = 1, Table options = null)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);

					PathFollowMode? mode                                                                 = null;
					if (options != null && options.TryGet("follow_mode", out PathFollowMode _mode)) mode = _mode;

					actor.WalkToNextPathPoint(count, mode);
					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			/// <summary>
			/// Walk backwards along the previous N segments of the actor's current path. (default is just 1)
			/// </summary>
			public ICoroutineWaitable walk_previous([NotNull] List<DirectedActor> actors, int count = 1)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.WalkToPreviousPathPoint(count);
					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			/// <summary>
			/// Walk the remaining nodes of the actor's current path.
			/// </summary>
			public ICoroutineWaitable walk_remaining([NotNull] List<DirectedActor> actors)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.WalkToPathEnd();
					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable walk_loop([NotNull] List<DirectedActor> actors, int count = -1)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.WalkPathLoop(count);
					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			public void walk_path(Actor actor, string regionPath)
			{
				throw new NotImplementedException();
			}

			public ManagedActorMove jump_to(DirectedActor actor, DynValue goal, float height = 1, float speed = 1, Table options = null)
			{
				ManagedActorMove do_pos(Vector3 pos)
				{
					proxy.EnsureControl(actor);
					float distance = Vector3.Distance(actor.gameObject.transform.position, pos);

					speed  *= 1.75f;
					height *= distance * 1 / 2f;

					AnimationCurve curve = AnimationCurve.Constant(0, 1, 1);

					if (options != null)
					{
						options.TryGet("curve", out curve);
					}

					actor.directions.moveMode  = DirectedActor.MoveMode.Tween;
					actor.directions.moveTween = new JumperTo(speed, height, curve) { speedBased = true };
					actor.directions.moveGoal  = pos;

					if (actor.jumpAnimation != null)
					{
						actor.directions.moveAnimation         = actor.jumpAnimation;
						actor.directions.moveAnimationRiseFall = true;
					}

					return new ManagedActorMove(actor);
				}

				if (goal.AsUserdata(out Transform t))
					return do_pos(t.position);

				if (goal.AsUserdata(out WorldPoint wp) && wp.TryGet(out Vector3 wp_pos))
					return do_pos(wp_pos);

				if (goal.AsUserdata(out DirectedActor targetActor))
				{
					float radius = 0.5f;

					options.TryGet("radius", out radius, radius);

					Vector3 pos = targetActor.actor.Position;
					pos -= actor.actor.facing * radius;

					return do_pos(pos);
				}

				return null;
			}

			/*[NotNull]
			public ManagedActorMove jump_to([NotNull] DirectedActor actor, Vector3 goal, float height = 1, float speed = 1, Table options = null)
			{
				proxy.EnsureControl(actor);
				float distance = Vector3.Distance(actor.gameObject.transform.position, goal);

				speed  *= 1.75f;
				height *= distance * 1 / 2f;

				AnimationCurve curve = AnimationCurve.Constant(0, 1, 1);

				if (options != null)
				{
					options.TryGet("curve", out curve);
				}

				actor.directions.moveMode  = DirectedActor.MoveMode.Tween;
				actor.directions.moveTween = new JumperTo(speed, height, curve) { speedBased = true };
				actor.directions.moveGoal  = goal;

				if (actor.jumpAnimation != null)
				{
					actor.directions.moveAnimation         = actor.jumpAnimation;
					actor.directions.moveAnimationRiseFall = true;
				}

				return new ManagedActorMove(actor);
			}*/

			public ManagedTween tween_to(GameObject obj, DynValue goal, float duration = 1)
			{
				Vector3 position;
				if (goal.AsUserdata(out Transform trans))
					position = trans.position;
				else
					return null;

				//Tween tween = obj.transform.DOMove(position, duration);

				//EaserTo easer = new EaserTo(duration, Ease.Linear);

				var   easer = new JumperTo(duration, 2);
				Tween tween = easer.ApplyTo(() => obj.transform.position, v3 => obj.transform.position = v3, position);

				return new ManagedTween(tween);
			}

			public ICoroutineWaitable jump_path(List<DirectedActor> actors, string graphPath, Table options = null)
			{
				if (!proxy.state.graph.TryGet("_asset", out RegionGraph graph))
					return null;

				RegionObject graphObj = graph.FindByPath(graphPath);
				if (graphObj is RegionPath path)
				{
					return jump_path(actors, path, options);
				}

				return null;
			}

			// int startIndex = 0, int targetIndex = 0, float height = 1, float duration = 1, int count = 1,
			public ICoroutineWaitable jump_path(List<DirectedActor> actors, RegionPath path, Table options = null)
			{
				foreach (DirectedActor actor in actors)
				{
					float height   = 1;
					float duration = 1;
					int   count    = 1;

					int? start_index  = null;
					int? target_index = null;

					if (options != null)
					{
						options.TryGet("height", out height, height);
						options.TryGet("duration", out duration, duration);
						options.TryGet("count", out count, count);
						options.TryGet("start_index", out start_index, start_index);
						options.TryGet("target_index", out target_index, target_index);
					}

					proxy.EnsureControl(actor);
					options.TryGet("follow_mode", out PathFollowMode mode);
					actor.TweenPath(path, new JumperTo(duration, height, count), start_index, target_index, mode);
					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable jump_next([NotNull] List<DirectedActor> actors, int number = 1, Table options = null)
			{
				//Debug.LogError("Jump_next needs to be reimplemented. Walking to next.");
				//return walk_next(actors, count);

				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);

					if (actor.directions.moveTween == null)
					{
						float height   = 1;
						float duration = 1;
						int   count    = 1;

						if (options != null)
						{
							options.TryGet("height", out height, height);
							options.TryGet("duration", out duration, duration);
							options.TryGet("count", out count, count);
						}

						actor.directions.moveTween = new JumperTo(duration, height, count)
						{
							OnJump = () =>
							{
								/*if(sfx_jump.IsValid)*/
								GameSFX.PlayGlobal(GameAssets.Live.SFX_Default_Jump_Sound);
							},
							OnLand = () =>
							{
								/*if(sfx_land.IsValid)*/
								GameSFX.PlayGlobal(GameAssets.Live.SFX_Default_Land_Sound);
							}
						};
					}

					PathFollowMode? mode = null;

					if (options != null)
					{
						if (options.TryGet("on_path", out bool on_path))
							actor.directions.pathState.on_path = on_path;

						if (options.TryGet("follow_mode", out PathFollowMode _mode))
							mode = _mode;
					}


					actor.TweenToNextPathPoint(number, mode);


					if (actor.jumpAnimation != null)
					{
						actor.directions.moveAnimation         = actor.jumpAnimation;
						actor.directions.moveAnimationRiseFall = true;
					}

					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			public ICoroutineWaitable jump_previous(List<DirectedActor> actors, int number = 1)
			{
				foreach (DirectedActor actor in actors)
				{
					proxy.EnsureControl(actor);
					actor.TweenToPreviousPathPoint(number);

					if (actor.jumpAnimation != null)
					{
						actor.directions.moveAnimation         = actor.jumpAnimation;
						actor.directions.moveAnimationRiseFall = true;
					}

					_scratchWaitables.Add(new ManagedActorMove(actor));
				}

				return FlushScratchWaitable();
			}

			public void ground_snap([NotNull] List<DirectedActor> actors, bool val = true)
			{
				foreach (DirectedActor actor in actors)
				{
					if (actor.actor is NPCActor npc)
						npc.GroundSnapping = val;
				}
			}

		#endregion


		#region Anchor

			public void anchor_to(DirectedActor actor, [NotNull] DynValue value)
			{
				if (value.IsNil()) return;

				proxy.EnsureControl(actor);

				actor.directions.moveMode = DirectedActor.MoveMode.Anchor;

				if (value.UserData.TryGet(out GameObject go))
					actor.directions.moveAnchor = new WorldPoint(go);
				else if (value.UserData.TryGet(out Transform transform))
					actor.directions.moveAnchor = new WorldPoint(transform.gameObject);
			}

		#endregion


		#region Combat

			// public CoroutineBattle start_battle(BattleRecipe recipe, string arena)
			// {
			// 	var managed = new CoroutineBattle
			// 	{
			// 		recipe       = recipe,
			// 		arenaAddress = arena
			// 	};
			// 	proxy.RegisterManaged(managed);
			// 	return managed;
			// }

			public object start_battle(Table config)
			{
				var managed = new CoroutineBattle();

				if (config.TryGet("arena", out DynValue arena))
				{
					if (arena.AsString(out string nameOrAddress))
					{
						if (Arena.FindByName(nameOrAddress, out Arena _arena))
						{
							managed.arena = _arena;
						}
						else
						{
							managed.arenaAddress = nameOrAddress;
						}
					}
					else if (arena.AsUserdata(out Arena component))
					{
						managed.arena = component;
					}
				}

				if (config.TryGet("heal", out DynValue b))
				{
					if (b.AsBool(out bool heal))
					{
						if (heal)
						{
							var save = SaveManager.current;
							save.HealParty();
						}
					}
				}


				if (config.TryGet("transition", out Table tbl))
				{
					CombatTransitionSettings transition = CombatTransitionSettings.Default;
					transition.SetFromTable(tbl);
					managed.transition = transition;
				}

				config.TryGet("arena", out managed.arenaAddress);

				if (!config.TryGet("recipe", out managed.recipeAddress)) managed.recipe = BattleRecipe.new_recipe(config);

				if (config.TryGet("shared_coach", out GameObject prefab))		managed.sharedCoachPrefab = prefab;

				if (config.TryGet("music", out string music)) managed.musicAddress                  = music;
				if (config.TryGet("flee_disabled", out bool flee_disabled)) managed.fleeDisabled    = flee_disabled;
				if (config.TryGet("retry_disabled", out bool retry_disabled)) managed.retryDisabled = retry_disabled;
				if (config.TryGet("immunity", out bool immunity)) managed.immunity                  = immunity;

				proxy.RegisterManaged(managed);

				return CollectNewManageds();
			}

			public void show_skill_name(string name)
			{
				const float duration = CombatNotifyUI.SKILL_USED_POPUP_DURATION;
				CombatNotifyUI.DoSkillUsedPopup(name, duration).Forget();
			}

		#endregion

		#region Timing

			// public static DecorationBlock UntilDistance(float       distance)                                => NestBlock(new UntilDistance(distance));
			// public static DecorationBlock UntilPercent(float        percent)                                 => NestBlock(new UntilPercent(percent));
			// public static DecorationBlock UntilElapsed(float        duration)                                => NestBlock(new UntilElapsed(duration));
			// public static DecorationBlock UntilProc(PuppetAnimation animation, string markerStart = "start") => NestBlock(new UntilProc(animation, markerStart));

		#endregion

		#region Arena Camera

			[NotNull] public void cmstate(string stateName)
			{
				BattleRunner battle = costate.battle;
				if (battle != null)
				{
					if (Enum.TryParse(stateName, true, out ArenaCamera.States state))
						battle.camera.PlayState(state);
				}
			}

			[NotNull] public CamAnimation cmfunc(Closure anim, int repeats)
			{
				var ret = new CamAnimation(anim, repeats);
				proxy.ReplaceManaged("cmfunc", ret);
				return ret;
			}

			public void cmstop()
			{
				costate.battle.camera.PlayState(ArenaCamera.States.idle);
			}

			public Vector3 get_cam()
			{
				System.Diagnostics.Debug.Assert(Camera.main != null, "Camera.main != null");
				return Camera.main.transform.position;
			}

			// [NotNull] public CamOrbitAnimation cmzoom(float zoom)
			// {
			// 	var ret = new CamOrbitAnimation(0, -zoom, -zoom);
			// 	proxy.RegisterManaged(ret);
			// 	return ret;
			// }
			//
			// [NotNull] public CamOrbitAnimation cmorbit(
			// 	float? azimuth   = null,
			// 	float? elevation = null,
			// 	float? distance  = null,
			// 	Ease   ease      = Ease.Linear,
			// 	float  duration  = 0.75f)
			// {
			// 	var ret = new CamOrbitAnimation(azimuth, elevation, distance, ease, duration);
			// 	proxy.RegisterManaged(ret);
			// 	return ret;
			// }
			//
			// [NotNull] public object cmorbit(
			// 	Table tbl
			// 	// Ease  ease     = Ease.Linear,
			// 	// float duration = 0.75f
			// )
			// {
			// 	Table tbRot,
			// 		tbLift,
			// 		tbDist,
			// 		tbZoom;
			// 	Ease ease     = Ease.Linear;
			// 	var  duration = 0.75f;
			//
			// 	if (tbl.TryGet("rot", out tbRot))
			// 	{
			// 		tbRot.Get(1).AsFloat(out float rot);
			// 		tbRot.Get(2).AsUserdata(out ease, ease);
			// 		tbRot.Get(3).AsFloat(out duration, duration);
			//
			// 		proxy.RegisterManaged(new CamOrbitAnimation(rot, null, null, ease, duration));
			// 	}
			//
			// 	if (tbl.TryGet("lift", out tbLift))
			// 	{
			// 		tbLift.Get(1).AsFloat(out float lift);
			// 		tbLift.Get(2).AsUserdata(out ease, ease);
			// 		tbLift.Get(3).AsFloat(out duration, duration);
			//
			// 		proxy.RegisterManaged(new CamOrbitAnimation(null, lift, null, ease, duration));
			// 	}
			//
			// 	if (tbl.TryGet("distance", out tbDist))
			// 	{
			// 		tbDist.Get(1).AsFloat(out float dist);
			// 		tbDist.Get(2).AsUserdata(out ease, ease);
			// 		tbDist.Get(3).AsFloat(out duration, duration);
			//
			// 		proxy.RegisterManaged(new CamOrbitAnimation(null, null, dist, ease, duration));
			// 	}
			//
			// 	if (tbl.TryGet("zoom", out tbZoom))
			// 	{
			// 		tbZoom.Get(1).AsFloat(out float zoom);
			// 		tbZoom.Get(2).AsUserdata(out ease, ease);
			// 		tbZoom.Get(3).AsFloat(out duration, duration);
			//
			// 		proxy.RegisterManaged(new CamOrbitAnimation(null, null, -zoom, ease, duration));
			// 	}
			//
			// 	return CollectNewManageds();
			// }

			[NotNull] public CoroutineCamAzimuth cmazimuth(DynValue dvvalue, DynValue dvduration, Ease easer) =>
				proxy.RegisterManaged(new CoroutineCamAzimuth
				{
					targetValue = dvvalue.AsFloat(),
					duration    = dvduration.AsFloat(),
					ease        = easer
				});

			[NotNull] public CoroutineCamElevation cmelevation(DynValue dvvalue, DynValue dvduration, Ease easer) =>
				proxy.RegisterManaged(new CoroutineCamElevation
				{
					targetValue = dvvalue.AsFloat(),
					duration    = dvduration.AsFloat(),
					ease        = easer
				});

			[NotNull] public CoroutineCamDistance cmdistance(DynValue dvvalue, DynValue dvduration, Ease easer) =>
				proxy.RegisterManaged(new CoroutineCamDistance
				{
					targetValue = dvvalue.AsFloat(),
					duration    = dvduration.AsFloat(),
					ease        = easer
				});

			[NotNull] public CoroutineCamAim cmorient(DynValue dvvalue, DynValue dvduration, Ease easer)
			{
				return proxy.RegisterManaged(new CoroutineCamAim
				{
					targetValue = dvvalue.AsVector3(),
					duration    = dvduration.AsFloat(),
					ease        = easer
				});
			}

			public CamLookAnimation cmlook(DynValue target, float duration, Ease ease) => proxy.ReplaceManaged("cmlook", new CamLookAnimation(target, duration, ease));

			//public CamAimAnimation cmaim(DynValue target, )

			public CoroutineCamFov cmfov(DynValue dvvalue, DynValue dvduration, Ease easer)
			{
				float target = dvvalue.AsFloat();

				// Normalized value
				if (target >= 0 && target <= 1.001f)
				{
					target = costate.battle.arena.Camera.InitialState.fov * Mathf.Clamp01(target);
					//Debug.Log($"Normalized Value Initial: {costate.battle.arena.Camera.InitialState.fov}, Target: {dvvalue.AsFloat()}, Output: {target}");
				}
				else
				{
					//Debug.Log($"Non-Normalized Value: Target {target}");
				}

				return proxy.ReplaceManaged("cmfov", new CoroutineCamFov
				{
					targetValue = target,
					duration    = dvduration.AsFloat(),
					ease        = easer
				});
			}

			[CanBeNull]
			public CamLookAnimation cmlook([NotNull] DynValue arg)
			{
				if (arg.Type == DataType.Table)
				{
					return proxy.ReplaceManaged("cmlook", new CamLookAnimation(arg));
				}
				else
				{
					proxy.StopAndRemoveManaged("cmlook");

					costate.battle.arena.Camera.SetMode(ArenaCamera.BodyLookModes.Synchronized, out MotionBehaviour motion, out MotionBehaviour _);
					motion.Main = new MotionDef
					{
						Type    = Motions.Damper,
						Damping = 0.15f
					};
					motion.Follow = MotionAPI.MTarget(arg);
					motion.Start();
				}

				return null;
			}

			public CamLookAnimation cmlook(Table     motion) => proxy.ReplaceManaged("cmlook", new CamLookAnimation(motion));
			public CamLookAnimation cmlook_ext(Table motion) => proxy.ReplaceManaged("cmlook", new CamLookAnimation(motion, true));

			public object cmshake(float duration, float amplitude)
			{
				if (costate.battle == null) return null;

				CinemachineNoiseController controller = costate.battle.camera.noise;

				controller.DoShake(duration, amplitude);
				proxy.ReplaceManaged("cmshake", new CinemachineNoiseController.Managed { controller = controller });

				return CollectNewManageds();
			}

			// [NotNull] public CamLookAnimation cmlook(WorldPoint wp, Ease ease, float duration)
			// {
			// 	var ret = new CamLookAnimation(wp, ease, duration);
			// 	proxy.ReplaceManaged("cmlook", ret);
			// 	return ret;
			// }
			//
			// [NotNull] public CamLookAnimation cmlook(WorldPoint wp1, WorldPoint wp2, float t, Ease ease, float duration)
			// {
			// 	var ret = new CamLookAnimation(wp1, wp2, t, ease, duration);
			// 	proxy.ReplaceManaged("cmlook", ret);
			// 	return ret;
			// }

		#endregion

		#region Generic Cameras

			public string active_cam
			{
				get => proxy.state.vcamName;
				set
				{
					proxy.state.vcamName = value;
					proxy.ControlCamera(true);
				}
			}

			public CinemachineVirtualCamera get_cam([NotNull] string id)
			{
				if (proxy._cameras.TryGetValue(id, out CinemachineVirtualCamera cam))
					return cam;

				return null;
			}

			public void cam_play(Closure loopingAnim)
			{
				throw new NotImplementedException();
			}

			public ManagedCinemachineBlend cam_state(string id, CinemachineBlendDefinition definition)
			{
				if (proxy.step != Steps.Skipping)
					proxy.vcamBlend = definition;
				else
					proxy.vcamBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);

				active_cam = id;

				if (definition.m_Style == CinemachineBlendDefinition.Style.Cut || definition.m_Time < Mathf.Epsilon)
					return null;

				proxy.RegisterManaged(ManagedCinemachineBlend.Instance);
				return ManagedCinemachineBlend.Instance;
			}

			public ManagedCinemachineBlend cam_state(string id, float duration) =>
				cam_state(id, CinemachineBlendDefinition.Style.EaseInOut, duration);

			public ManagedCinemachineBlend cam_state(string id, CinemachineBlendDefinition.Style style, float duration)
			{
				if (proxy.step != Steps.Skipping)
				{
					proxy.vcamBlend = new CinemachineBlendDefinition(style, duration);
				}
				else
				{
					proxy.vcamBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
				}

				active_cam = id;

				if (style == CinemachineBlendDefinition.Style.Cut || duration < Mathf.Epsilon)
					return null;

				proxy.RegisterManaged(ManagedCinemachineBlend.Instance);
				return ManagedCinemachineBlend.Instance;
			}

			public void cam_reset_blend()
			{
				proxy.vcamBlend = GameCams.Cut;
			}

			[NotNull]
			public ManagedTween cam_move([NotNull] string id, Vector3? start, Vector3? target, float time, [CanBeNull] Table options = null)
			{
				bool local_rot = false;
				if (options != null)
				{
					local_rot = options.TryGet("local_rotation", false);
				}

				Tween tween = null;
				if (proxy._cameras.TryGetValue(id, out CinemachineVirtualCamera vcam))
				{
					Transform trans = vcam.transform;

					Vector3 _rot(Vector3 vec)
					{
						return local_rot ? trans.rotation * vec : vec;
					}

					Vector3 initalPos = trans.position;
					if (target.HasValue && start.HasValue)
					{
						trans.position = initalPos + _rot(start.Value);
						tween          = trans.DOMove(initalPos + _rot(target.Value), time);
					}
					else if (!target.HasValue && start.HasValue)
					{
						trans.position = initalPos + _rot(start.Value);
						tween          = trans.DOMove(initalPos, time);
					}
					else if (target.HasValue && !start.HasValue)
					{
						tween = trans.DOMove(initalPos + _rot(target.Value), time);
					}
				}

				ModifyTweenUsingTable(tween, options);

				return new ManagedTween(tween);
			}

			[NotNull]
			public ManagedTween cam_rot([NotNull] string id, Vector3? start, Vector3? target, float time, [CanBeNull] Table options = null)
			{
				Tween tween = null;
				if (proxy._cameras.TryGetValue(id, out CinemachineVirtualCamera vcam))
				{
					Transform trans     = vcam.transform;
					Vector3   initalRot = trans.rotation.eulerAngles;

					if (target.HasValue && start.HasValue)
					{
						trans.rotation = Quaternion.Euler(initalRot + start.Value);
						tween          = trans.DORotate(initalRot + target.Value, time);
					}
					else if (!target.HasValue && start.HasValue)
					{
						trans.rotation = Quaternion.Euler(initalRot + start.Value);
						tween          = trans.DORotate(initalRot, time);
					}
					else if (target.HasValue && !start.HasValue)
					{
						tween = trans.DORotate(initalRot + target.Value, time);
					}
				}

				ModifyTweenUsingTable(tween, options);

				return new ManagedTween(tween);
			}

			[NotNull]
			public ManagedTween cam_fov([NotNull] string id, float? start, float? target, float time, [CanBeNull] Table options = null)
			{
				Tween tween = null;
				if (proxy._cameras.TryGetValue(id, out CinemachineVirtualCamera vcam))
				{
					float initialFOV = vcam.m_Lens.FieldOfView;

					if (target.HasValue && start.HasValue)
					{
						vcam.m_Lens.FieldOfView = start.Value;
						tween                   = DOTween.To(() => vcam.m_Lens.FieldOfView, fov => vcam.m_Lens.FieldOfView = fov, target.Value, time);
					}
					else if (!target.HasValue && start.HasValue)
					{
						vcam.m_Lens.FieldOfView = initialFOV;
						tween                   = DOTween.To(() => vcam.m_Lens.FieldOfView, fov => vcam.m_Lens.FieldOfView = fov, start.Value, time);
					}
					else if (target.HasValue && !start.HasValue)
					{
						tween = DOTween.To(() => vcam.m_Lens.FieldOfView, fov => vcam.m_Lens.FieldOfView = fov, initialFOV + target.Value, time);
					}
				}

				ModifyTweenUsingTable(tween, options);

				return new ManagedTween(tween);
			}

			/// <summary>
			/// Play a shake effect
			/// </summary>
			public object cam_shake(string name, float duration, float amplitude /*, float speed, float randomness*/)
			{
				if (proxy._cameras.TryGetValue(name, out CinemachineVirtualCamera vcam))
				{
					var controller = vcam.GetOrAddComponent<CinemachineNoiseController>();
					controller.DoShake(duration, amplitude);
					proxy.RegisterManaged(new CinemachineNoiseController.Managed { controller = controller });
					return CollectNewManageds();
				}

				return null;
			}

			public void cam_reset(string name)
			{
				if (proxy._cameras.TryGetValue(name, out CinemachineVirtualCamera vcam) && proxy._restoreCamStates.TryGetValue(vcam, out var state))
				{
					state.ResetUsing(vcam);
				}
			}

			public void cam_update_initial_state(DynValue cam)
			{
				if (cam.AsString(out string id) && proxy._cameras.TryGetValue(id, out var vcam))
				{
					proxy._restoreCamStates[vcam] = new InitialCamState(vcam);
				}
				else if (cam.AsUserdata(out CinemachineVirtualCamera vcam2))
				{
					proxy._restoreCamStates[vcam2] = new InitialCamState(vcam2);
				}
			}

			private void ModifyTweenUsingTable([CanBeNull] Tween tween, [CanBeNull] Table table)
			{
				if (table == null || tween == null) return;

				if (table.TryGet("ease", out Ease ease))
					tween.SetEase(ease);
				else if (table.TryGet("ease", out AnimationCurve curve))
					tween.SetEase(curve);
				else
					tween.SetEase(Ease.InOutQuad);
			}


			public void cam_reset_outgoing_blend()
			{
				proxy.outgoingBlend = null;
			}

			public void cam_outoing_blend(CinemachineBlendDefinition blend)
			{
				proxy.outgoingBlend = blend;
			}

			public void cam_outoing_blend(CinemachineBlendDefinition.Style style, float time)
			{
				proxy.outgoingBlend = new CinemachineBlendDefinition(style, time);
			}

			// Usable outside the main function. This doesn't have much use yet, but it will once I add the ability to spawn
			// cams in scripts, so you can spawn a cam and set it as the default for an entire cutscene. -C.L.
			public void main_cam(string id, CinemachineBlendDefinition.Style style, float time)
			{
				proxy.state.mainCameraName = id;
				proxy.vcamBlend            = new CinemachineBlendDefinition(style, time);
			}

			private void insureInteractCamera()
			{
				if (proxy.interactCamera == null)
					proxy.interactCamera = GameCams.RentCharacterInteraction();

				proxy._cameras["interaction"] = proxy.interactCamera.vcam;
			}

			public ManagedCinemachineBlend cam_interaction([CanBeNull] Transform interaction = null, float duration = 0.65f)
			{
				if (interaction == null)
					interaction = proxy.sourceObject.transform;

				insureInteractCamera();
				proxy.interactCamera.SetPlayerInteraction(ActorController.playerActor.transform, interaction);
				return cam_state("interaction", CinemachineBlendDefinition.Style.EaseInOut, duration);
			}

			public ManagedCinemachineBlend cam_interaction(DynValue interaction, float duration = 0.65f)
			{
				Transform trans = null;

				if (interaction.IsNil())
					trans = proxy.sourceObject.transform;
				else if (interaction.AsUserdata(out DirectedActor actor))
				{
					trans = actor.actor.transform;
				}
				else if (interaction.AsUserdata(out Transform t))
				{
					trans = t;
				}

				insureInteractCamera();
				proxy.interactCamera.SetPlayerInteraction(ActorController.playerActor.transform, trans);

				var blend = cam_state("interaction", CinemachineBlendDefinition.Style.EaseInOut, duration);

				proxy.ActiveUpdate();
				GameCams.Live.Brain.ManualUpdate();

				proxy.interactCamera.UpdateForward();

				return blend;
			}

			public ManagedCinemachineBlend cam_player_look_at(DynValue target, float duration = 0.65f, Table options = null)
			{
				insureInteractCamera();

				if (target.AsUserdata(out Transform trans))
				{
					if (trans == null)
						trans = proxy.sourceObject.transform;

					proxy.interactCamera.LookAtTarget(trans);
				}
				else if (target.AsUserdata(out WorldPoint wp))
				{
					proxy.interactCamera.LookAtTarget(wp);
				}
				else
				{
					return null;
				}

				if (options != null)
				{
					if (options.TryGet("fov", out float fov))
					{
						var lens = VCamProxyNormal.DefaultLens;
						lens.FieldOfView = fov;
						proxy.interactCamera.UpdateLens(lens);
					}
				}

				return cam_state("interaction", CinemachineBlendDefinition.Style.EaseInOut, duration);
			}

		#endregion


			[NotNull] public ProximityProcAnimation proximity_proc(GameObject self, int i, [NotNull] Fighter t, float distance = 2f)
			{
				var ret = new ProximityProcAnimation(self, i, t.actor.transform, distance);
				proxy.RegisterManaged(ret);
				return ret;
			}

			[NotNull] public ProximityProcAnimation proximity_proc(GameObject self, int i, [NotNull] Slot t, float distance = 2f)
			{
				var ret = new ProximityProcAnimation(self, i, t.actor.transform, distance);
				proxy.RegisterManaged(ret);
				return ret;
			}

			[NotNull] public ProximityProcAnimation proximity_proc(GameObject self, int i, Transform t, float distance = 2f)
			{
				var ret = new ProximityProcAnimation(self, i, t, distance);
				proxy.RegisterManaged(ret);
				return ret;
			}

			public void snap_home([NotNull] Fighter fighter)
			{
				fighter.snap_home();
			}

			public void highlight_slot(Slot slot, bool state = true)
			{
				throw new NotImplementedException();
			}

			public void despawn(List<CoroutineFX> fxes)
			{
				if (fxes == null) return;
				foreach (CoroutineFX fx in fxes)
				{
					fx.Despawn();
				}
			}

			public void despawn(CoroutineFX fx)
			{
				if (fx == null) return;
				fx.Despawn();
			}

			public void despawn_immediate(CoroutineFX fx)
			{
				if (fx == null) return;
				fx.Despawn(true);
			}

			[CanBeNull] public void retract_fx(CoroutineFX fx)
			{
				if (fx == null) return;
				fx.Retract();
			}

			[CanBeNull] public CoroutineFX fx(DynValue dv, Table conf)
			{
				bool istransform = dv.AsObject(out Transform tfprefab);

				if (dv.AsObject(out CoroutineFX fx))
				{
					fx.UpdateConfig(conf);
					return fx;
				}
				else
				{
					if (dv.AsObject(out GameObject prefab) || istransform)
					{
						if (istransform) prefab = tfprefab.gameObject; // This can some time happen, the transform of the prefab is assigned instead. no clue why that happens
						if (prefab == null) return null;

						var ret = new CoroutineFX(new FX
						{
							prefab = prefab,
							config = conf
						});

						proxy.RegisterManaged(ret);
						return ret;
					}
					else if (dv.AsObject(out string address))
					{
						var ret = new CoroutineFX(new FX
						{
							address = address,
							config  = conf
						});

						proxy.RegisterManaged(ret);
						return ret;
					}
				}

				throw new ArgumentException($"Invalid input to fx(): {dv.Type}, {dv.UserData?.Object?.GetType()?.Name} ");
			}

			[CanBeNull]
			public Proc gnextproc()
			{
				if (costate.procs.PopNext(out Proc proc))
					return proc;

				Debug.LogError("No proc remaining in the ProcTable.");
				return null;
			}

			/// <summary>
			/// Fire a specific proc.
			/// </summary>
			public void nextproc([CanBeNull] Proc proc)
			{
				if (costate.battle == null)
				{
					this.LogWarn("Cannot fire proc without battle.");
					return;
				}

				if (proc == null)
				{
					Debug.LogError("Cannot fire a null proc.");
					return;
				}

				costate.battle.battle.Proc(proc);
			}

			/// <summary>
			/// Fire the next proc.
			/// </summary>
			public void nextproc()
			{
				ProcTable table = costate.procs;
				if (!table.PopNext(out Proc proc))
				{
					this.LogError("Failed to PopNext proc.");
					return;
				}

				nextproc(proc);
			}

			/// <summary>
			/// Fire all remaining procs.
			/// </summary>
			public void nextprocs()
			{
				if (costate.battle == null)
				{
					this.LogWarn("Cannot fire proc without battle");
					return;
				}

				if (costate.procs == null)
				{
					this.LogWarn("Cannot fire procs without proctable");
					return;
				}

				while (costate.procs.PopNext(out Proc proc))
				{
					costate.battle.battle.Proc(proc);
				}
			}

			public void fireproc(Proc proc)
			{
				if (costate.procs.Pop(proc))
				{
					costate.battle.battle.Proc(proc);
				}
			}

			// /// <summary>
			// /// Spawn a combat effect
			// /// </summary>
			// [NotNull] public CombatEffectCoanimation<TEffect> spawn_effect<TEffect>(
			// 	GameObject                  onto,
			// 	TEffect                     combatEffectPrefab,
			// 	Vector3                     position,
			// 	[CanBeNull] Handler<TEffect> onCreate = nul
			// )
			// 	where TEffect : CombatEffect
			// {
			// 	var ret = new CombatEffectCoanimation<TEffect>(onto, combatEffectPrefab, position, onCreate);
			// 	proxy.RegisterManaged(ret);
			// 	return ret;
			// }


			public void punch_camera(
				float power,
				float duration,
				float elasticity = 0,
				int   vibrato    = 0,
				Ease  ease       = Ease.InOutQuad)
			{
				var tweenOrbit = new AdditiveTweenOrbit();

				Tween tween = DOTween.Punch(
						() => tweenOrbit.coordinate.Vector,
						vec3 => tweenOrbit.coordinate.Vector = vec3,
						new Vector3(0, 0, 1) * 1.7f,
						duration,
						vibrato,
						elasticity
					)
					.SetEase(ease);

				tweenOrbit.RemoveOnComplete(tween);

				if (costate.battle == null) return;
				costate.battle.camera.Orbit.AddAdditiveOrbit(tweenOrbit);
			}


		#region Timeline Management

			[CanBeNull]
			public ManagedPlayableDirector play_timeline(PlayableDirector director)
			{
				if (director == null) return null;
				director.GetComponent<TimeMarkerSystem>()?.Reset();
				director.Play();
				return new ManagedPlayableDirector(director);
			}

			[CanBeNull]
			public ManagedPlayableDirector play_timeline_skip([NotNull] PlayableDirector director)
			{
				ManagedPlayableDirector ret = play_timeline(director);

				if (!director.TryGetComponent(out PlayableDirectorSkipper skipper))
				{
					skipper           = director.AddComponent<PlayableDirectorSkipper>();
					skipper.temporary = true;
				}

				return ret;
			}


			[NotNull]
			public ManagedPlayableDirector play_timeline([NotNull] string name)
			{
				PlayableDirector director = proxy._directors[name];
				director.GetComponent<TimeMarkerSystem>()?.Reset();
				director.Play();
				return new ManagedPlayableDirector(director);
			}

			public void set_timeline([NotNull] string name)
			{
				proxy.state.director = proxy._directors[name];
				proxy.state.director.GetComponent<TimeMarkerSystem>()?.Reset();
				proxy.state.director.Play();
				proxy.state.director.Pause();
			}

			[NotNull]
			public ManagedPlayableDirector timeline_next(string name)
			{
				PlayableDirector director = proxy.state.director;

				TimeMarkerSystem system = director.GetComponent<TimeMarkerSystem>();
				system.Resume();
				system.SetTargetMarker(name);
				return new ManagedPlayableDirector(director);
			}

			[NotNull]
			public ManagedPlayableDirector timeline_next()
			{
				PlayableDirector director = proxy.state.director;

				TimeMarkerSystem system = director.GetComponent<TimeMarkerSystem>();
				system.Resume(director.state != PlayState.Playing);
				system.StepTargetMarker();
				return new ManagedPlayableDirector(director);
			}

		#endregion

			// public WaitableShop open_shop(GameObject go)
			// {
			// 	ShopNPC shop = go.GetComponent<ShopNPC>();
			// 	shop.Open();
			//
			// 	return new WaitableShop(shop);
			// }
			// public WaitableBattle start_battle(BattleRecipe recipe, string arena, string transitioner = "Combat/Default Transitioner")
			// {

			/// <summary>
			/// Return the single waitable in _scratchWaitables, or a WaitableGroup if there are more than one.
			/// </summary>
			private static ICoroutineWaitable FlushScratchWaitable()
			{
				ICoroutineWaitable ret = _scratchWaitables.Count == 1 ? _scratchWaitables[0] : new ManagedWaitableGroup(_scratchWaitables);
				_scratchWaitables.Clear();
				return ret;
			}
		}
	}
}