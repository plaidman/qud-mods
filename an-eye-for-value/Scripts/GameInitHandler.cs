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
		}
	}

	[PlayerMutator]
	public class NewCharacterHandler : IPlayerMutator {
		public void mutate(GameObject player) {
			player.RequirePart<AEFV_ItemKnowledge>();
		}
	}
}
