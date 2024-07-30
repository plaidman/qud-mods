using Plaidman.ZoneLootList.Parts;
using XRL;
using XRL.World;

namespace Plaidman.ZoneLootList.Handlers
{
	[HasCallAfterGameLoaded]
	public class LoadGameHandler {
		[CallAfterGameLoaded]
		public static void AfterLoaded() {
			if (The.Player == null) return;

			The.Player.RequirePart<ZLL_ZoneLootFinder>();
			var part = The.Player.GetPart<ZLL_ZoneLootFinder>();
			part.ToggleAbility();
		}
	}

	[PlayerMutator]
	public class NewCharacterHandler : IPlayerMutator {
		public void mutate(GameObject player) {
			player.RequirePart<ZLL_ZoneLootFinder>();
			var part = player.GetPart<ZLL_ZoneLootFinder>();
			part.ToggleAbility();
		}
	}
	
	[HasOptionFlagUpdate]
	public class OptionChangeHandler {
		[OptionFlagUpdate]
		public static void FlagUpdate() {
			if (The.Player == null) return;
			if (!The.Player.HasPart<ZLL_ZoneLootFinder>()) return;
			
			var part = The.Player.GetPart<ZLL_ZoneLootFinder>();
			part.ToggleAbility();
		}
	}
}
