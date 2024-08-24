using System;
using System.Collections.Generic;
using System.Linq;
using XRL.Collections;
using XRL.UI;
using XRL.World.AI;

namespace XRL.World.Parts
{
    [Serializable]
    public class NalathniBoosterPack : IPart
    {
        public Faction faction;
        public bool Starter = false;
		
        public override void Register(GameObject Object) {
			Object.RegisterPartEvent(this, "GetInventoryActions");
            Object.RegisterPartEvent(this, "InvCommandUnwrap");
            Object.RegisterPartEvent(this, "ObjectCreated");
            
            
			base.Register(Object);
		}

        public static List<GameObjectBlueprint> GetFactionMembersIncludingUniques(string Faction) {
			List<GameObjectBlueprint> list = new();
			foreach (GameObjectBlueprint gameObjectBlueprint in GameObjectFactory.Factory.BlueprintList)
			{
				if (!gameObjectBlueprint.Tags.ContainsKey("BaseObject")
                    && gameObjectBlueprint.HasPart("Brain")
                    && gameObjectBlueprint.GetPart("Brain").Parameters.ContainsKey("Factions")
                    && (gameObjectBlueprint.GetPart("Brain").Parameters["Factions"].Contains(Faction + "-100")
                        || gameObjectBlueprint.GetPart("Brain").Parameters["Factions"].Contains(Faction + "-50")
                        || gameObjectBlueprint.GetPart("Brain").Parameters["Factions"].Contains(Faction + "-25")
                    )
                ) {
                    if(!gameObjectBlueprint.DisplayName().Contains("["))
                        list.Add(gameObjectBlueprint);
				}
			}
			return list;
		}
        
        public static void GenerateDeckFor(GameObject creature)
        {
            if(creature.Brain == null) return;

            var factions = creature.Brain.Allegiance
                .Where(faction => Brain.GetAllegianceLevel(faction.Value) == Brain.AllegianceLevel.Member)
                .Select(faction => faction.Key)
                .ToList();

            if(factions.Count == 0) return;
             for(int i=0; i<12; i++)
             {
                 string faction = factions.GetRandomElement();
                  GameObject card = GameObjectFactory.Factory.CreateObject("NalathniCard");
                  card.GetPart<NalathniTradingCard>().SetCreature(
                    GetFactionMembersIncludingUniques(faction).GetRandomElement().createSample()
                  );
                  creature.TakeObject(card, true);
             }
        }
        
        public override bool FireEvent(Event E)
        {
            if(E.ID == "ObjectCreated")
            {
               faction = Factions.GetRandomFactionWithAtLeastOneMember();
               ParentObject.DisplayName = "pack of Salt Shuffle cards: "+faction.DisplayName;
               if(Starter) ParentObject.DisplayName = "Salt Shuffle starter deck";
            }

            if(E.ID == "GetInventoryActions")
            {
                if (The.Player.OnWorldMap()) return true;
                EventParameterGetInventoryActions actions = E.GetParameter("Actions") as EventParameterGetInventoryActions;
                actions.AddAction(
                    Name: "Unwrap",
                    Key: 'o',
                    FireOnActor: false,
                    Display: "&Wo&ypen",
                    Command: "InvCommandUnwrap",
                    Default: 2
                );
                return true;
            }

            if(E.ID == "InvCommandUnwrap")
            {
                
                if(!ThePlayer.HasPart("NalathniCardChallenger")) ThePlayer.AddPart<NalathniCardChallenger>();
                List<GameObject> cards = new List<GameObject>();
                
                string tally = "You unwrap "+this.ParentObject.the + this.ParentObject.DisplayName+" and get:\n";
                if(Starter)
                {
                    for(int i=0; i<12; i++)
                    {
                        GameObject card = GameObjectFactory.Factory.CreateObject("NalathniCard");
                        cards.Add(card);
                        IPart.ThePlayer.TakeObject(card, true);
                        tally += card.DisplayName+"\n";
                    }
                }
                else
                {
                    for(int i=0; i<5; i++)
                    {
                        GameObject card = GameObjectFactory.Factory.CreateObject("NalathniCard");
                        card.GetPart<NalathniTradingCard>().SetCreature(GetFactionMembersIncludingUniques(faction.Name).GetRandomElement().createSample());
                        cards.Add(card);
                        IPart.ThePlayer.TakeObject(card, true);
                        tally += card.DisplayName+"\n";
                    }
                }
                Popup.Show(tally);
                this.ParentObject.Destroy("Unwrapped", true);
                return true;
            }

            return true;
        }
        
    }
}