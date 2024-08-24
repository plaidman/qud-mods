using System;
using XRL.UI;

namespace XRL.World.Parts
{
    [Serializable]
    public class NalathniCardChallenger : IPart
    {
        public override void Register(GameObject Object)
		{
            Object.RegisterPartEvent(this, "OwnerGetInventoryActions");
            Object.RegisterPartEvent(this, "InvCommandCardGame");
			base.Register(Object);
		}
        public override bool FireEvent(Event E)
        {
            if(E.ID == "OwnerGetInventoryActions")
            {
                if(NalathniTradingCard.CardsInZoneOf(ThePlayer).Count == 0) return true;
                EventParameterGetInventoryActions actions = E.GetParameter("Actions") as EventParameterGetInventoryActions;
                GameObject target = E.GetGameObjectParameter("Object");
                if(target == ThePlayer) return true;
                if(NalathniTradingCard.CardsInZoneOf(target).Count == 0)
                {
                    NalathniBoosterPack.GenerateDeckFor(target);
                }
                if(NalathniTradingCard.CardsInZoneOf(target).Count > 0)
                    actions.AddAction("playcards", 'P', true, "&WP&ylay Salt Shuffle", "InvCommandCardGame");//, 2, false, false, false, false);
            }
            if(E.ID == "InvCommandCardGame")
            {
                
                GameObject With = E.GetGameObjectParameter("Object");
                Brain pBrain = With.pBrain;
                if (pBrain != null && pBrain.IsHostileTowards(IPart.ThePlayer))
                {
                    
                        Popup.Show(string.Concat(new string[]
                        {
                            With.The,
                            With.ShortDisplayName,
                            "&y",
                            With.GetVerb("refuse", true, false),
                            " to play card games with you."
                        }));
                    
                    return false;
                }
                if (With.IsEngagedInMelee())
                {
                        Popup.Show(string.Concat(new string[]
                        {
                            With.The,
                            With.ShortDisplayName,
                            "&y",
                            With.Is,
                            " engaged in hand-to-hand combat and",
                            With.Is,
                            " too busy to play card games with you."
                        }));
                    
                    return false;
                }
                NalathniTradingCard.NewGameWith(With);
            }
            return true;
        }
    }

}