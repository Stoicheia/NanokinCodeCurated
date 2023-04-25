using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Overworld.Controllers;
using UnityEngine;
using Util;

namespace Anjin.Nanokin.ParkAI
{
	/*
	 * TYPES OF PEEP INTERACTION NODES:
	 *
	 * Resting:
	 * 		- Things like park benches/tables, but also surfaces where people might sit down (stairs, or the edge of a flowerbed, for example).
	 * 		- Resting nodes should have less or more desirability.
	 * 		- Peeps can either refill their energy, consume food/drink, or observe POIs with these nodes.
	 *
	 * Food:
	 * 		- Hungry/Thirsty peeps can buy food/drink at these locations.
	 *		- Depending on the type of food, they may sit down to eat, or just consume the food while continuing along their journey.
	 *		- Food nodes may have predefined seating (a restaurant will have tables), while an outside stall may not, in which case peeps will
	 * 		need to actually seek out seating.
	 *
	 * Points Of Interest (POIs):
	 *		- Anything that peeps take interest in to stop and observe.
	 * 		- Cool Rides, Landmarks, Scenery, Good views ect...
	 *
	 * Rides:
	 * 		- Activities that peeps can take part in for their enjoyment. This is not limited to thrill rides like roller coasters, but may
	 * 		be something as simple as a photo booth or hall of mirrors.
	 *
	 *
	 * Queue lines:
	 * 		- Food & Ride nodes can have queue lines for peeps to wait in.
	 * 		These lines have a set number of slots that peeps occupy and move through before the reach the actual node.
	 *
	 * TYPES OF PEEPS:
	 *
	 * Marathon:
	 * 		Picks a sequence from portal to portal within the graph and travels along it. Literally running a marathon.
	 * 		Only stops to rest, eat, or sight-see directly along their paths based on their stat levels, does not seek POIs out.
	 *
	 * Meander-er:
	 * 		Walks randomly around the graph. Tries not to visit nodes they've visited before if they can help it.
	 */

	public enum PeepLOD { LOD0, LOD1, LOD2, LOD3, LOD4 }

	[LuaEnum] public enum PeepRace 		: byte { Generic = 0, White, Asian, Black, Hispanic }
	[LuaEnum] public enum PeepBodyType 	: byte { Average, Round, Small }
	[LuaEnum] public enum PeepGender 	: byte { Male, Female }
	[LuaEnum] public enum PeepType 		: byte { Adult, Child }
	[LuaEnum] public enum PeepAccessory : byte { None, Hat }

	public enum PeepStat {
		None,
		Hunger,
		Thirst,
		Boredom,
		Tiredness,
		Bathroom
	}

	public enum PeepBehaviour { Marathon, }


	//public enum PeepGoal : byte { None, Wander, Destination, Refreshment, Entertainment }

	public struct PeepDef {

		// TODO: Maybe?
		//public string Name;

		public PeepType     	Type;
		public PeepGender   	Gender;
		public PeepBodyType 	BodyType;
		public PeepRace     	Race;

		public PeepAccessory    HeadAccessory;
		public PeepAccessory    BodyAccessory;

		public static PeepDef Default = new PeepDef {
			Type 	 		= PeepType.Adult,
			Gender 			= PeepGender.Male,
			BodyType 		= PeepBodyType.Average,
			Race 			= PeepRace.White,

			HeadAccessory 	= PeepAccessory.None,
			BodyAccessory 	= PeepAccessory.None,

		};
	}


	public struct PeepStats {

		public const int   NUM_STATS         = 5;
		public const float DEFAULT_STAT_CAP  = 100;
		public const float DEFAULT_STAT_RATE = 1 / 60f; //1 pt per minute

		private static Stat[] _scratchStats = new Stat[NUM_STATS];

		public         Stat   Hunger;
		public         Stat   Thirst;
		public         Stat   Boredom;
		public         Stat   Tiredness;
		public         Stat   Bathroom;

		public PeepStat Urgency_Highest;

		public void Update(float dt)
		{
			Hunger.Update(dt);
			Thirst.Update(dt);
			Boredom.Update(dt);
			Tiredness.Update(dt);
			Bathroom.Update(dt);

			_scratchStats[0] = Hunger;
			_scratchStats[1] = Thirst;
			_scratchStats[2] = Boredom;
			_scratchStats[3] = Tiredness;
			_scratchStats[4] = Bathroom;

			Urgency_Highest = PeepStat.None;
			float last_highest = Mathf.Epsilon;

			for (int i = 0; i < NUM_STATS; i++) {
				Stat stat = _scratchStats[i];
				if (stat.Urgency > last_highest) {
					Urgency_Highest = stat.id;
					last_highest    = stat.Urgency;
				}
			}

		}

		public static PeepStats RandomStats(float minStartVal = 0f, float maxStartVal = 1f)
		{
			var stats = Default;

			stats.Hunger.RandomizeValue(minStartVal, maxStartVal);
			stats.Thirst.RandomizeValue(minStartVal, maxStartVal);
			stats.Boredom.RandomizeValue(minStartVal, maxStartVal);
			stats.Tiredness.RandomizeValue(minStartVal, maxStartVal);
			stats.Bathroom.RandomizeValue(minStartVal, maxStartVal);

			return stats;
		}

		public static PeepStats Default = new PeepStats {
			Hunger                 = new Stat(PeepStat.Hunger,    40, DEFAULT_STAT_RATE * 3f,   DEFAULT_STAT_CAP),
			Thirst                 = new Stat(PeepStat.Thirst,    70, DEFAULT_STAT_RATE,        DEFAULT_STAT_CAP),
			Boredom                = new Stat(PeepStat.Boredom,   40, DEFAULT_STAT_RATE * 0.5f, DEFAULT_STAT_CAP),
			Tiredness              = new Stat(PeepStat.Tiredness, 40, DEFAULT_STAT_RATE * 2f,   DEFAULT_STAT_CAP),
			Bathroom               = new Stat(PeepStat.Bathroom,  60, DEFAULT_STAT_RATE * 1.5f, DEFAULT_STAT_CAP),
			Urgency_Highest        = PeepStat.None,

		};
	}

	public struct Stat {
		public PeepStat id;
		public float    value;
		public float    threshold;
		public float    rate;
		public float    cap;

		public float rate_mod;

		public static implicit operator float(Stat s) => s.value;

		public float Urgency => Mathf.Max(0, (value - threshold) / (cap - threshold) );

		public void Update(float dt)
		{
			value += rate * ParkAIController.Config.Peep_StatGainMod;
			value =  value.Clamp(0, cap);
		}

		public void RandomizeValue(float min = 0f, float max = 1f)
		{
			value = RNG.Range(min, max) * cap;
		}

		public Stat(PeepStat id, float threshold, float rate, float cap)
		{
			this.id        = id;
			value          = 0;
			this.threshold = threshold;
			this.rate      = rate;
			this.cap       = cap;
			rate_mod       = 0;
		}
	}

}