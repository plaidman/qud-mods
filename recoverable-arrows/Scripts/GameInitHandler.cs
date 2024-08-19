using XRL;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.RecoverableArrows.Handlers
{
	[HasCallAfterGameLoaded]
	public class LoadGameHandler {
		[CallAfterGameLoaded]
		public static void AfterLoaded() {
			if (The.Player == null) return;
			The.Player.RequirePart<RA_ArrowTracking>();
		}
	}

	[PlayerMutator]
	public class NewCharacterHandler : IPlayerMutator {
		public void mutate(GameObject player) {
			player.RequirePart<RA_ArrowTracking>();
		}
	}
}
