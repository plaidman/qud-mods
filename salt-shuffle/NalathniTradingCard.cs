using System;
using System.Collections.Generic;
using System.Threading;
using ConsoleLib.Console;
using UnityEngine;
using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.AI.GoalHandlers;
using XRL.World.Capabilities;
using XRL.Language;
using XRL.World.Parts.Mutation;
using XRL.World;
using Qud.API;
using System.Text;
namespace XRL.World.Parts
{
    [Serializable]
    public class NalathniTradingCard : IPart
    {
        public GameObject creature;
        public int SunScore = 0;
        public int MoonScore = 0;
        public int StarScore = 0;
        public int PointValue = 0;
        public int TopScore = -1000;
        public string Zone = "DECK";
        
        private static string LatestGameNews = "";
        private static int PointsToWin = 50;
        public static string GameName = "Salt Shuffle";
        
        public GameObject Owner()
        {
            return this.ParentObject.InInventory;
        }
        public string NameWhose(bool lowercase = false)
        {
            string prefix = "";
            if(Owner() == IPart.ThePlayer)
            {
                if(lowercase) prefix = "your ";
                else prefix = "Your ";
            }
            else
            {
                if(lowercase) prefix = Owner().the+Owner().DisplayNameStripped+"'s ";
                else prefix = Owner().The+Owner().DisplayNameStripped+"'s ";
            }
            return prefix + this.ParentObject.ShortDisplayName;
        }
        
        public static List<NalathniTradingCard> CardsInZoneOf(GameObject cardplayer, string zone=null)
        {
            
            List<NalathniTradingCard> foundCards = new List<NalathniTradingCard>();
            if(!cardplayer.HasPart("Inventory")) return foundCards;
            List<GameObject> allitems = cardplayer.GetPart<Inventory>().GetObjects();
            foreach(GameObject item in allitems)
            {
                NalathniTradingCard card = item.GetPart<NalathniTradingCard>();
                if(card != null && (card.Zone == zone || zone == null))
                {
                    foundCards.Add(card);
                }
            }
            return foundCards;
        }
        
        public static NalathniTradingCard Draw(GameObject player)
        {
            NalathniTradingCard card = null;
            List<NalathniTradingCard> deck = CardsInZoneOf(player, "DECK");
            if(deck.Count == 0) return null;
            card = deck.GetRandomElement();
            card.Zone = "HAND";
            return card;
        }
        
        public static int ScorePoints(GameObject player, int points=0)
        {
            int score = player.GetIntProperty("CardGameScore");
            score += points;
            player.SetIntProperty("CardGameScore", score);
            return score;
        }
        
        public int ValueAgainst(GameObject player)
        {
            int total = 0;
            foreach(NalathniTradingCard card in CardsInZoneOf(player, "FIELD"))
            {
                total = GetScoreAgainstCard(card);
            }
            return total;
        }
        
        public int CrushMarginOver(NalathniTradingCard foe)
        {
            int margin = Math.Min(Math.Min(SunScore - foe.SunScore, MoonScore - foe.MoonScore),StarScore - foe.StarScore);
            
            return margin;
        }
        
        public int GetScoreAgainstCard(NalathniTradingCard foe)
        {
            int margin = this.MarginOver(foe);
            if(margin == 3) return -this.CrushMarginOver(foe);
            if(margin == 2) 
            {
                if(foe.PointValue > this.PointValue) return foe.PointValue;
                return 1;
            }
            return 0;
        }
        
        public string Oppose(NalathniTradingCard foe)
        {
            int margin = this.MarginOver(foe);
            if(margin < 2) return "";
            if(margin == 3) 
            {
                foe.Zone = "DECK";
                int penalty = this.CrushMarginOver(foe);
                ScorePoints(this.Owner(), -penalty);
                return this.NameWhose()+" &rcrushes&y "+foe.NameWhose(true)+"&y. (-"+penalty+" renown)\n";
            }
            if(margin == 2) 
            {
                if(foe.PointValue > this.PointValue)
                {
                    ScorePoints(this.Owner(), foe.PointValue);
                    foe.Zone = "ANTE";
                    return this.NameWhose()+" &Ctopples&y "+foe.NameWhose(true)+"&y! (+"+foe.PointValue+" renown)\n";
                }
                else
                {
                    ScorePoints(this.Owner(), 1);
                    foe.Zone = "DECK";
                    return this.NameWhose()+" &gvanquishes&y "+foe.NameWhose(true)+"&y. (+1 renown)\n";
                }
            }
            return "A mistake was made.";
        }
        
        public static bool NewGameWith(GameObject opponent)
        {
            LatestGameNews = "";
            if(CardsInZoneOf(IPart.ThePlayer).Count <10)
            {
                Popup.Show("You need at least 10 cards in order to play.", true);
                return false;
            }
            if(CardsInZoneOf(opponent).Count <10)
            {
                Popup.Show("Your opponent needs at least 10 cards in order to play.", true);
                return false;
            }
            foreach(NalathniTradingCard card in CardsInZoneOf(IPart.ThePlayer))
            {
                card.Zone = "DECK";
            }
            foreach(NalathniTradingCard card in CardsInZoneOf(opponent))
            {
                card.Zone = "DECK";
            }
            IPart.ThePlayer.SetIntProperty("CardGameScore", 0);
            opponent.SetIntProperty("CardGameScore", 0);
            for(int i=0; i<2; i++)
            {
                Draw(IPart.ThePlayer);
                Draw(opponent);
            }
            GameObject winner = null;
            while(winner == null) winner = ResolveSingleTurn(opponent);
            
            if(winner == ThePlayer)
            {
                GameObject card = GameObjectFactory.Factory.CreateObject("NalathniCard");
                card.GetPart<NalathniTradingCard>().SetCreature(GameObjectFactory.Factory.CreateObject(opponent.Blueprint));
                Popup.Show("You get a card as a souvenir of your victory:\n\n"+card.DisplayName);
                ThePlayer.TakeObject(card);
            }
            
            return true;
        }
        
        public void ResolveCardAgainst(GameObject opponent)
        {
            List<NalathniTradingCard> enemyField = CardsInZoneOf(opponent, "FIELD");
            if(Owner() == ThePlayer) LatestGameNews += "You play "+this.ParentObject.DisplayName+"&y.\n\n";
            else LatestGameNews += Owner().The+Owner().DisplayNameStripped+" plays "+this.ParentObject.DisplayName+"&y.\n\n";
            foreach(NalathniTradingCard foe in enemyField)
            {
                LatestGameNews += this.Oppose(foe);
            }
            this.Zone = "FIELD";
        }
        
        public static string BoardState(GameObject npc)
        {
            string output = "&Y"+npc.The+npc.DisplayNameStripped+" ("+ScorePoints(npc)+" renown):\n";
            foreach(NalathniTradingCard card in CardsInZoneOf(npc, "FIELD"))
            {
                output += card.ParentObject.DisplayName+"\n";
            }
            
            output += "\n&Y"+IPart.ThePlayer.DisplayNameStripped+" ("+ScorePoints(IPart.ThePlayer)+" renown):\n";
            foreach(NalathniTradingCard card in CardsInZoneOf(IPart.ThePlayer, "FIELD"))
            {
                output += card.ParentObject.DisplayName+"\n";
            }
            return output;
        }
        public static GameObject ResolveSingleTurn(GameObject npc)
        {
            //opponent decides and plays card
            NalathniTradingCard npcPlay = null;
            if(Draw(npc) == null) 
                LatestGameNews += "&C"+npc.The+npc.DisplayNameStripped+" can't draw from an empty deck.\n";
            List<NalathniTradingCard> npcHand = CardsInZoneOf(npc, "HAND");
            if(npcHand.Count > 0) 
            {
                int bestOutcome = -1000;
                npcPlay = npcHand[0];
                foreach(NalathniTradingCard candidate in npcHand)
                {
                    int value = candidate.ValueAgainst(ThePlayer);
                    if(value > bestOutcome)
                    {
                        npcPlay = candidate;
                        bestOutcome = value;
                    }
                }
            }
            if(npcPlay != null) npcPlay.ResolveCardAgainst(IPart.ThePlayer);
            else LatestGameNews += "&C"+npc.The+npc.DisplayNameStripped+" can't play any cards.\n";
            //score opponent's end turn
            List<NalathniTradingCard> npcField = CardsInZoneOf(npc, "FIELD");
            int npcScore = ScorePoints(npc, npcField.Count);
            LatestGameNews += "\n&y"+npc.The+npc.DisplayNameStripped+" scores "+npcField.Count+" renown for "+npc.its+" fielded cards.\n";
            
            if(npcScore >= PointsToWin)
            {
                LatestGameNews += "&R"+npc.The+npc.DisplayNameStripped+" wins the game!\n";
                Popup.Show(LatestGameNews, true);
                return npc;
            }
            NalathniTradingCard drawn = Draw(IPart.ThePlayer);
            if(drawn == null) 
                LatestGameNews += "&RYou can't draw from an empty deck.\n";
            //else LatestGameNews += "&CYou draw "+drawn.ParentObject.DisplayName+".\n";
            
            Popup.Show(LatestGameNews, true);
            LatestGameNews = "";
            
            
            LatestGameNews += BoardState(npc);
            
            List<NalathniTradingCard> playerHand = CardsInZoneOf(ThePlayer, "HAND");
            NalathniTradingCard playerCard = null;
            if(playerHand.Count == 0)
            {
                LatestGameNews += "&RYou have no cards in your hand.\n";
                Popup.Show(LatestGameNews, true);
            }
            else
            {
                LatestGameNews += "\n\n&CPlay a card:";
                int choice = -1;
                string[] texts = new string[playerHand.Count];
                for(int i=0; i<playerHand.Count; i++)
                {
                    texts[i] = playerHand[i].ParentObject.DisplayName;
                }
                for (choice = -1; choice < 0; choice = Popup.ShowOptionList(string.Empty, texts, null, 0, LatestGameNews, 78, true, false, 0, string.Empty)){};
                playerCard = playerHand[choice];
            }
            LatestGameNews = "";
            if(playerCard != null) playerCard.ResolveCardAgainst(npc);
            List<NalathniTradingCard> playerField = CardsInZoneOf(ThePlayer, "FIELD");
            int playerScore = ScorePoints(ThePlayer, playerField.Count);
            LatestGameNews +="\n&yYou score "+playerField.Count+" renown for your fielded cards.\n";
            if(playerScore >= PointsToWin)
            {
                LatestGameNews += "------\n"+BoardState(npc)+"\n";
                LatestGameNews += "&GYou win the game!";
                Popup.Show(LatestGameNews, true);
                return ThePlayer;
            }
            Popup.Show(LatestGameNews, true);
            LatestGameNews = "";
            //declare victory for opponent if over target score, popup final news            
            return null;
        }
        
        public override void Register(GameObject Object)
		{
            Object.RegisterPartEvent(this, "ObjectCreated");
            Object.RegisterPartEvent(this, "GetShortDescription");
            Object.RegisterPartEvent(this, "GetDisplayName");
            Object.RegisterPartEvent(this, "GetShortDisplayName");
			base.Register(Object);
		}
        public int MarginOver(NalathniTradingCard other)
        {
            int margin = 0;
            if(this.SunScore > other.SunScore) margin += 1;
            if(this.MoonScore > other.MoonScore) margin += 1;
            if(this.StarScore > other.StarScore) margin += 1;
            return margin;
        }
        public void SetCreature(GameObject go)
        {
            creature = go;
            if(go == null) creature = EncountersAPI.GetASampleCreature(null);
            SunScore = 2;
            MoonScore = 2;
            StarScore = 2;
            int XPLevel = 1;
            if (creature.Statistics.ContainsKey("Strength")) SunScore += creature.Statistics["Strength"].Value;
            if (creature.Statistics.ContainsKey("Ego")) StarScore += creature.Statistics["Ego"].Value;
            if (creature.Statistics.ContainsKey("Toughness")) SunScore += creature.Statistics["Toughness"].Value;
            if (creature.Statistics.ContainsKey("Willpower")) StarScore += creature.Statistics["Willpower"].Value;
            if (creature.Statistics.ContainsKey("Intelligence")) MoonScore += creature.Statistics["Intelligence"].Value;
            if (creature.Statistics.ContainsKey("Agility")) MoonScore += creature.Statistics["Agility"].Value;
            if (creature.Statistics.ContainsKey("Level")) XPLevel = creature.Statistics["Level"].Value;
            
            int minScore = Math.Min(Math.Min(SunScore, MoonScore), StarScore);
            int maxScore = Math.Max(Math.Max(SunScore, MoonScore), StarScore);
            int spread = maxScore - minScore;
            
            if(XPLevel < 5) XPLevel = 5;
            
            SunScore -= minScore * 2 / 3;
            MoonScore -= minScore * 2 / 3;
            StarScore -= minScore * 2 / 3;
            float total = SunScore + MoonScore + StarScore;
            SunScore = (int) Math.Round(SunScore * XPLevel / total);
            MoonScore = (int) Math.Round(MoonScore * XPLevel / total);
            StarScore = (int) Math.Round(StarScore * XPLevel / total);
            
            int error = XPLevel - (SunScore + MoonScore + StarScore);
            SunScore += error;
            
            if(creature.pBrain != null && creature.pBrain.Factions.Contains("Baetyl"))
            {
                SunScore = -5;
                MoonScore = -5;
                StarScore = -5;
            }
            
            PointValue = SunScore + MoonScore + StarScore;
            
            TopScore = SunScore;
            if(TopScore < MoonScore) TopScore = MoonScore;
            if(TopScore < StarScore) TopScore = StarScore;
            string color = "&y";
                if(TopScore == SunScore) color = "&W";
                if(TopScore == MoonScore) color = "&C";
                if(TopScore == StarScore) color = "&M";
            this.ParentObject.pRender.ColorString = ConsoleLib.Console.ColorUtility.StripBackgroundFormatting(creature.pRender.ColorString);
			this.ParentObject.pRender.DetailColor = creature.pRender.DetailColor;
									
             //   this.ParentObject.GenderName = creature.GetPropertyOrTag("Gender", null);
           // this.ParentObject.DisplayName = "&ytrading card: &Y"+creature.DisplayNameStripped;
           // this.ParentObject.ShortDisplayName = "&Y"+creature.DisplayNameStripped;
		}
        
        public override bool FireEvent(Event E)
        {
            if(E.ID == "ObjectCreated")
            {
                if(creature == null) SetCreature(EncountersAPI.GetASampleCreature());
                this.ParentObject.SetIntProperty("NeverStack", 1);
            }
            if(E.ID == "GetShortDescription")
            {
                string desc = "A trading card with a stylized illustration of "+creature.a+creature.DisplayNameStripped+" plus various cryptic statistics.\n\n";
                List<string> factions = new List<string>(creature.pBrain.FactionMembership.Keys);
                desc += "&GAllegiance: "+string.Join(", ", factions.ToArray())+"\n";
                desc += "&WSun: &Y"+SunScore+" &CMoon: &Y"+MoonScore+" &MStar: &Y"+StarScore+"\n";
                Mutations muts = creature.GetPart<Mutations>();
                if(muts != null) desc += "&c"+muts.ToString();
                desc += "\n&K"+ConsoleLib.Console.ColorUtility.StripFormatting(creature.GetPart<Description>().Short);
                E.SetParameter("Prefix", desc);
                
              return true;  
            }
            if(E.ID == "GetDisplayName" || E.ID == "GetShortDisplayName")
            {
                StringBuilder stringBuilder = E.GetParameter("DisplayName") as StringBuilder;
                stringBuilder.Clear();
                string stats = " &W"+SunScore+"&Y/&C"+MoonScore+"&Y/&M"+StarScore;
                //if(E.ID == "GetDisplayName") stringBuilder.Append("&ytrading card: ");
                stringBuilder.Append("&Y"+creature.DisplayNameStripped);
                stringBuilder.Append(stats);
                if(E.ID == "GetDisplayName") stringBuilder.Append(" &K(Lv"+PointValue+")");
                
                return true;
            }
            return true;
        }
        
    }
}