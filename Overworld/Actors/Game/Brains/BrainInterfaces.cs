namespace Anjin.Actors
{
	/// <summary>
	/// A brain that can send inputs to in-game characters.
	/// </summary>
	public interface ICharacterActorBrain
	{
		void ResetInputs(Actor character, ref CharacterInputs inputs);
		void PollInputs(Actor  character, ref CharacterInputs inputs);
	}

	public interface ICharacterActorBrain<T> : ICharacterActorBrain
		where T : Actor
	{
		void PollInputs(T  character, ref CharacterInputs Inputs);
		void ResetInputs(T character, ref CharacterInputs Inputs);
	}

	public interface IAnimOverrider
	{
	}

	public interface ICharacterInputProvider<TInputs>
	{
		void PollInputs(ref TInputs inputs);
	}

	/// <summary>
	///
	/// </summary>
	public interface IFirstPersonFlightBrain
	{
		void PollInputs(ref FirstPersonFlightInputs inputs);
	}

	public interface ICameraBrain
	{

	}
}