using XRL.World;

namespace Plaidman.RecoverableArrows.Events {

    public class RA_ArrowLanded : ModPooledEvent<RA_ArrowLanded> {
        public bool HitWall = false;
        public bool HitBody = false;
        public Cell LandedCell;
    }
}