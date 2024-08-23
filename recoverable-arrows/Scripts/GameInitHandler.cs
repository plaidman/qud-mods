using System.Collections.Generic;
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
			The.Player.RequirePart<RA_ArrowTracker>();
		}
	}

	[PlayerMutator]
	public class NewCharacterHandler : IPlayerMutator {
		public void mutate(GameObject player) {
			player.RequirePart<RA_ArrowTracker>();
		}
	}
	
	[HasModSensitiveStaticCache]
	public class ProjectileBlueprint {
		[ModSensitiveStaticCache]
		public static Dictionary<string, string> Mapping = null;

		[ModSensitiveCacheInit]
		public static void InitMapping() {
			Dictionary<string, string> _mapping = new();

			foreach (var bp in GameObjectFactory.Factory.BlueprintList) {
				if (bp.IsBaseBlueprint()) continue;
				if (bp.HasPart("HindrenClueItem")) continue;
				
				if (bp.TryGetPartParameter("AmmoArrow", "ProjectileObject", out string projectileName)) {
					_mapping[projectileName] = bp.Name;
				}
			}
			
			Mapping = _mapping;
		}
	}
}
