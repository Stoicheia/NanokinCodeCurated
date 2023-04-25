using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Anjin.Actors
{
	[LuaUserdata(StaticName = "Actors")]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class ActorsLua
	{
		[LuaGlobalFunc, CanBeNull]
		public static Actor get_actor(ActorRef reference) => ActorRegistry.Get(reference);

		[LuaGlobalFunc, CanBeNull]
		public static PropActor get_prop(string path) => ActorRegistry.FindByPath<PropActor>(path);

		[LuaGlobalFunc]
		public static Component get_actor(string path, [CanBeNull] Type type = null)
		{
			Actor actor = ActorRegistry.FindByPath(path);
			if (actor == null || type == null) return actor;
			return actor.GetComponent(type);
		}

		[NotNull]
		public static NPCActor rent_guest(Table options)
		{
			var guest = ActorController.RentGuest();

			guest.designer.SetSpritesUsingTable(options);

			if (options.TryGet("state", out string state))
			{
				state = state.ToLower();
				switch (state)
				{
					case "sitting":
						guest.State = NPCState.Sitting;
						break;
				}
			}

			return guest;
		}

		public static DynValue grab_random_npc(int? number)
		{
			if (number == null)
			{
				return UserData.Create(ActorController.RentGuest());
			}
			else
			{
				List<NPCActor> actors = ActorController.GetRandomGuests(number.Value);
				Table          table  = new Table(Lua.envScript);

				for (int i = 0; i < actors.Count; i++)
					table[i + 1] = actors[i];

				return DynValue.NewTable(table);
			}
		}

		static Vector3[] temp_points = new Vector3[800];

		static List<int> rand_indicies_source = new List<int>();
		static List<int> rand_indicies        = new List<int>();

		public static DynValue spawn_random_npcs_in_region([NotNull] DynValue region, int max = 100)
		{
			if (region.Type != DataType.UserData || !region.UserData.TryGet(out RegionShape2D shape)) return DynValue.Nil;

			// TEMP
			//TODO: Refactor out to utility
			void FillRandom(int number)
			{
				rand_indicies_source.Clear();
				rand_indicies.Clear();

				for (int i = 0; i < number; i++)
				{
					rand_indicies_source.Add(i);
				}

				while (rand_indicies_source.Count > 0)
				{
					int ind = Random.Range(0, rand_indicies_source.Count);
					rand_indicies.Add(rand_indicies_source[ind]);
					rand_indicies_source.RemoveAt(ind);
				}
			}

			int num = 0;
			if (shape.TryGetMetadata(out WalkableSurfaceGrid grid))
			{
				if (grid.WorldPoints != null)
				{
					FillRandom(grid.WorldPoints.Count);
					for (int i = 0; i < grid.WorldPoints.Count && i < max; i++)
					{
						//temp_points[i] = grid.WorldPoints.RandomElement(rand);
						temp_points[i] = grid.WorldPoints[rand_indicies[i]];
						num++;
					}
				}
				else
				{
					FillRandom(grid.Points.Count);
					for (int i = 0; i < grid.Points.Count && i < max; i++)
					{
						temp_points[i] = shape.AreaPosToWorldPos(grid.Points[rand_indicies[i]].xz());
						num++;
					}
				}
			}
			else if (shape.TryGetMetadata(out PointDistributionMetadata distribution))
			{
				num = distribution.GetFor(shape, temp_points, distribution.number);
			}
			else
			{
				return DynValue.Nil;
			}

			if (num == 0) return DynValue.Nil;

			/*PointDistributionMetadata distribution = shape.Metadata.FirstOrDefault(x => x is PointDistributionMetadata) as PointDistributionMetadata;
			if (distribution == null) return DynValue.Nil;*/

			int spawned = num.Maximum(max);

			List<NPCActor> actors = ActorController.GetRandomGuests(spawned);
			for (int i = 0; i < spawned; i++)
				actors[i].Teleport(temp_points[i]); // BUG:

			Table table = new Table(Lua.envScript);
			for (int i = 0; i < spawned; i++)
				table[i + 1] = actors[i];

			return DynValue.NewTable(table);
		}

		public static Actor Player => ActorController.playerActor;

		//TODO: Non alloc, non LINQ.
		[CanBeNull]
		public static SpeechBubble say(Actor actor, DynValue text, float seconds)
		{
			(SpeechBubble sp, bool ok) = Say(actor, text, seconds, null);
			return sp;
		}

		[MoonSharpHidden]
		public static (SpeechBubble, bool) Say(Actor actor, DynValue text, float seconds, Table bubble_settings)
		{
			SpeechBubble result = null;

			if (actor != null)
			{
				if (text.Type == DataType.String)
					result = actor.Say(text.String, seconds);
				else if (text.Type == DataType.Table)
					result = actor.SayLines(text.Table.Values.Select(x => x.String).ToArray(), seconds);
				else
				{
					result = actor.Say("Invalid input to Say(...) !", seconds);
					actor.LogError("Invalid input to ActorsLua.Say(...)");
				}

				result.ApplySettings(bubble_settings);
			}

			return (result, result != null);
		}

		[CanBeNull]
		public static SpeechBubble say(Actor actor, DynValue text)
		{
			(SpeechBubble sp, bool ok) = Say(actor, text, null);
			return sp;
		}

		[MoonSharpHidden]
		public static (SpeechBubble, bool) Say(Actor actor, DynValue text, Table bubble_settings)
		{
			SpeechBubble result = null;

			if (actor != null)
			{
				if (text.Type == DataType.String)
					result = actor.SayManual(text.String);
				else if (text.Type == DataType.Table)
					result = actor.SayManualLines(text.Table.Values.Select(x => x.String).ToArray());
				else
				{
					result = actor.SayManual("Invalid input to Say(...) !");
					actor.LogError("Invalid input to ActorsLua.Say(...)");
				}

				result.ApplySettings(bubble_settings);
			}

			return (result, result != null);
		}

		[NotNull]
		public static Table Spawn(string asset_address)
		{
			Table t = new Table(Lua.envScript);
			return t;
		}
	}
}