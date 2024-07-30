using XRL;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.AnEyeForValue.Handlers
{
	[HasCallAfterGameLoaded]
	public class LoadGameHandler {
		[CallAfterGameLoaded]
		public static void AfterLoaded() {
			if (The.Player == null) return;

			The.Player.RequirePart<AEFV_ItemKnowledge>();
			The.Player.RequirePart<AEFV_LoadLightener>();
			The.Player.RequirePart<AEFV_LootFinder>();

			The.Player.GetPart<AEFV_LoadLightener>().ToggleAbility();
			The.Player.GetPart<AEFV_LootFinder>().ToggleAbility();
		}
	}

	[PlayerMutator]
	public class NewCharacterHandler : IPlayerMutator {
		public void mutate(GameObject player) {
			player.RequirePart<AEFV_ItemKnowledge>();
			player.RequirePart<AEFV_LoadLightener>();
			player.RequirePart<AEFV_LootFinder>();

			player.GetPart<AEFV_LoadLightener>().ToggleAbility();
			player.GetPart<AEFV_LootFinder>().ToggleAbility();
		}
	}

	[HasOptionFlagUpdate]
	public class OptionChangeHandler {
		[OptionFlagUpdate]
		public static void FlagUpdate() {
			if (The.Player == null) return;
			
			if (The.Player.HasPart<AEFV_LoadLightener>()) {
				The.Player.GetPart<AEFV_LoadLightener>().ToggleAbility();
			}

			if (The.Player.HasPart<AEFV_LootFinder>()) {
				The.Player.GetPart<AEFV_LootFinder>().ToggleAbility();
			}
		}
	}
}
