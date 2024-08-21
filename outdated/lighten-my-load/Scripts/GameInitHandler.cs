using XRL;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.LightenMyLoad.Handlers
{
	[HasCallAfterGameLoaded]
	public class LoadGameHandler {
		[CallAfterGameLoaded]
		public static void AfterLoaded() {
			if (The.Player == null) return;

			The.Player.RequirePart<LML_LoadLightener>();
			var part = The.Player.GetPart<LML_LoadLightener>();
			part.ToggleAbility();
		}
	}

	[PlayerMutator]
	public class NewCharacterHandler : IPlayerMutator {
		public void mutate(GameObject player) {
			player.RequirePart<LML_LoadLightener>();
			var part = player.GetPart<LML_LoadLightener>();
			part.ToggleAbility();
		}
	}
	
	[HasOptionFlagUpdate]
	public class OptionChangeHandler {
		[OptionFlagUpdate]
		public static void FlagUpdate() {
			if (The.Player == null) return;
			if (!The.Player.HasPart<LML_LoadLightener>()) return;
			
			var part = The.Player.GetPart<LML_LoadLightener>();
			part.ToggleAbility();
		}
	}
}
