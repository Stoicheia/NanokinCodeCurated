using Overworld.Cutscenes;

namespace Anjin.Nanokin
{
	public class CutsceneSpawnPoint : SpawnPoint
	{
		public Cutscene Cutscene;

		public override string SpawnPointName => Cutscene ? Cutscene.gameObject.name : name;

		public override void OnEnable()
		{
			base.OnEnable();
			Cutscene = GetComponent<Cutscene>();
			if (Cutscene == null)
				Cutscene = GetComponentInParent<Cutscene>();
		}

		public override void OnSpawn()
		{
			if (Cutscene) {
				Cutscene.StartDelayFrames = 60;
				Cutscene.Play();
			}
		}
	}
}