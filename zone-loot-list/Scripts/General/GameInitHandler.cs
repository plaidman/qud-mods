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
		}
	}

	[PlayerMutator]
	public class NewCharacterHandler : IPlayerMutator {
		public void mutate(GameObject player) {
			player.RequirePart<AEFV_ItemKnowledge>();
			player.RequirePart<AEFV_LoadLightener>();
			player.RequirePart<AEFV_LootFinder>();
		}
	}

	[HasOptionFlagUpdate]
	public class OptionChangeHandler {
		[OptionFlagUpdate]
		public static void FlagUpdate() {
			if (The.Player == null) return;

			if (The.Player.TryGetPart(out AEFV_LoadLightener lmlPart)) {
				lmlPart.ToggleAbility();
			}

			if (The.Player.TryGetPart(out AEFV_LootFinder zllPart)) {
				zllPart.ToggleAbility();
			}
		}
	}
}
