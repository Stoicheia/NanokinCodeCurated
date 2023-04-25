using Anjin.Scripting;

namespace Data.Overworld
{
	[LuaEnum]
	public enum Areas
	{
		None           	= 0,
		Bioscape       	= 1,
		CandycornCourt 	= 2,
		Decropolis     	= 3,
		Enchanteros    	= 4,
		Oceanus        	= 5,
		Prehistorica   	= 6,
		RawrVerse      	= 7,
		Staridium      	= 8,
		Valumia			= 9,
		Ect				= 10,
		Developer		= 50,
	}


	//NOTE: Add new levels as needed
	/// <summary>
	/// This should contain a unique ID for every level in the game.
	/// </summary>
	[LuaEnum]
	public enum LevelID
	{
		None = 0,

		// Prehistorica
		ChronicleCliffside 		= 10,
		RiverRapidsRetreat 		= 11,
		GiantsGraveyard			= 12,
		MtMassiveus 			= 15,
		CrystalCaverns 			= 16,

		// Oceanus
		Freeport 		= 20,
		MermaidMarina 	= 21,
		Aquarium 		= 22,
		ShakeysShack 	= 23,

		// Decropolis

		// Enchanteros

		// RawrVerse

		// Staridium

		// CandycornCourt

		// Bioscape

		// Valumia


		// Misc
		Crossways = 200,

		// Dev
		CL_Testing_General	= 500,
		EncounterTesting	= 501
	}
}