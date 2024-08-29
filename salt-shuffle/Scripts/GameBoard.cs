using System.Collections.Generic;
using System.Linq;
using XRL;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Nalathni.SaltShuffle {
	public class GameBoard {
        private const int PointsToWin = 50;
		private const int PlayerCards = 0;
		private const int OpponentCards = 1;
		private const int DeckZone = 0;
		private const int HandZone = 1;
		private const int FieldZone = 2;
		
		private static GameObject Opponent = null;
		// todo this is written to and displayed in weird places/intervals. should this be a stringbuilder?
        private static string LatestGameNews = "";
		private static readonly List<NalathniTradingCard>[,] CardZones = new List<NalathniTradingCard>[2,3];
		private static readonly int[] Scores = new int[2];

        public static bool NewGameWith(GameObject opponent) {
            if (!DeckUtils.HasCards(The.Player, 10)) {
                Popup.Show("You need at least 10 cards in order to play.");
                return false;
            }

            if (!DeckUtils.HasCards(opponent, 10)) {
                Popup.Show("Your opponent needs at least 10 cards in order to play.");
                return false;
            }

			Opponent = opponent;
            LatestGameNews = "";

			CardZones[PlayerCards, DeckZone] = DeckUtils.CardList(The.Player);
			CardZones[PlayerCards, HandZone] = new();
			CardZones[PlayerCards, FieldZone] = new();
			Scores[PlayerCards] = 0;

			CardZones[OpponentCards, DeckZone] = DeckUtils.CardList(Opponent);
			CardZones[OpponentCards, HandZone] = new();
			CardZones[OpponentCards, FieldZone] = new();
			Scores[OpponentCards] = 0;

            for (int i = 0; i < 2; i++) {
                Draw(PlayerCards);
                Draw(OpponentCards);
            }

            while (true) {
				ResolveOpponentTurn();

				if (Scores[OpponentCards] >= PointsToWin) {
					LatestGameNews += "&R" + Opponent.The + Opponent.DisplayNameStripped + " wins the game!\n";
					Popup.Show(LatestGameNews);
					break;
				}

				ResolvePlayerTurn();

				if (Scores[OpponentCards] >= PointsToWin) {
					LatestGameNews += "------\n" + BoardState() + "\n";
					LatestGameNews += "&GYou win the game!";
					Popup.Show(LatestGameNews);

					var card = GameObjectFactory.Factory.CreateObject("NalathniCard");
					// todo ensure we're using a sample creature when it makes sense, and using the creature directly otherwise
					card.GetPart<NalathniTradingCard>().SetCreature(Opponent);
					Popup.Show("You get a card as a souvenir of your victory:\n\n" + card.DisplayName);
					The.Player.TakeObject(card);
					break;
				}

				Popup.Show(LatestGameNews);
				LatestGameNews = "";
			}
            
            return true;
        }
		
		public static void ResolveOpponentTurn() {
            if (!Draw(OpponentCards)) {
                LatestGameNews += "&C" + Opponent.The + Opponent.DisplayNameStripped
					+ " can't draw from an empty deck.\n";
			}
			
            var npcHand = CardZones[OpponentCards, HandZone];
            int npcCard = -1;
            if (npcHand.Count > 0) {
                int bestOutcome = int.MinValue;

                for (var i = 0; i < npcHand.Count; i++) {
					var candidate = npcHand[i];
                    int value = CardScoreAgainstPlayer(candidate, PlayerCards);

                    if (value > bestOutcome) {
                        npcCard = i;
                        bestOutcome = value;
                    }
                }
            }

            if (npcCard != -1) {
				ResolveCardAgainstPlayer(npcCard, PlayerCards, OpponentCards);
			} else {
				LatestGameNews += "&C" + Opponent.The + Opponent.DisplayNameStripped
					+ " can't play any cards.\n";
			}

            var npcFieldCount = CardZones[OpponentCards, FieldZone].Count;
            ScorePoints(OpponentCards, npcFieldCount);

			// todo use StringBuilder.appendColored()
            LatestGameNews += "\n&y" + Opponent.The + Opponent.DisplayNameStripped + " scores "
				+ npcFieldCount + " renown for " + Opponent.its + " fielded cards.\n";
		}

        public static void ResolveCardAgainstPlayer(int yourCardIndex, int foe, int you) {
			var yourHand = CardZones[you, HandZone];
			var yourCard = yourHand[yourCardIndex];

            if (you == PlayerCards) {
				LatestGameNews += "You play ";
			} else {
				LatestGameNews += Opponent.The + Opponent.DisplayNameStripped + " plays ";
			}
			LatestGameNews += yourCard.ParentObject.DisplayName + "&y.\n\n";

            var enemyField = CardZones[foe, FieldZone];
			for (var i = enemyField.Count-1; i >= 0; i--) {
                LatestGameNews += ResolveCardAgainstCard(yourCard, i, foe, you);
			}

			yourHand.RemoveAt(yourCardIndex);
			CardZones[you, FieldZone].Add(yourCard);
        }

        // todo fix whitespace
        // todo put all todos into github issues
        public static string ResolveCardAgainstCard(NalathniTradingCard yourCard, int foeCardIndex, int foe, int you) {
			var enemyField = CardZones[foe, FieldZone];
			var enemyDeck = CardZones[foe, DeckZone];
			var foeCard = enemyField[foeCardIndex];
            int margin = CardStatsAgainstCard(yourCard, foeCard);

            if (margin == 3) {
				// returned to hand
				enemyField.RemoveAt(foeCardIndex);
				enemyDeck.Add(foeCard);

                int penalty = CardCrushPenalty(yourCard, foeCard);
                ScorePoints(you, penalty * -1);
                return NameWhose(you, yourCard) + " &rcrushes&y "
                    + NameWhose(foe, foeCard, true)
                    // todo use StringBuilder.appendSigned()
                    + "&y. (-" + penalty + " renown)\n";
            }

            if (margin == 2) {
                if (foeCard.PointValue > yourCard.PointValue) {
					// banished from play
					enemyField.RemoveAt(foeCardIndex);

                    ScorePoints(you, foeCard.PointValue);
                    return NameWhose(you, yourCard) + " &rtopples&y "
                        + NameWhose(foe, foeCard, true)
                        + "&y. (+" + foeCard.PointValue + " renown)\n";
                } else {
					// returned to hand
					enemyField.RemoveAt(foeCardIndex);
					enemyDeck.Add(foeCard);

                    ScorePoints(you, 1);
                    return NameWhose(you, yourCard) + " &rvanquishes&y "
                        + NameWhose(foe, foeCard, true)
                        + "&y. (+1 renown)\n";
                }
            }

			return "";
        }

        public static string NameWhose(int who, NalathniTradingCard card, bool lowercase = false) {
            string prefix;

            if (who == PlayerCards) {
                prefix = lowercase ? "your" : "Your";
            } else {
                var ownerPossessive = Grammar.MakePossessive(Opponent.DisplayNameStripped);
                prefix = lowercase ? Opponent.the : Opponent.The;
                prefix += ownerPossessive;
            }
            
            return prefix + " " + card.ShortDisplayName;
        }

        public static int CardScoreAgainstPlayer(NalathniTradingCard card, int who) {
            int total = 0;

            foreach (var foe in CardZones[who, FieldZone]) {
                total = CardScoreAgainstCard(card, foe);
            }

            return total;
        }

        public static int CardScoreAgainstCard(NalathniTradingCard card, NalathniTradingCard foe) {
            int margin = CardStatsAgainstCard(card, foe);

            if (margin == 3) {
				return CardCrushPenalty(card, foe) * -1;
			}

            if (margin == 2) {
                if (foe.PointValue > card.PointValue) return foe.PointValue;
                else return 1;
            }

            return 0;
        }

        public static int CardStatsAgainstCard(NalathniTradingCard card, NalathniTradingCard foe) {
            int margin = 0;

            if (card.SunScore > foe.SunScore) margin += 1;
            if (card.MoonScore > foe.MoonScore) margin += 1;
            if (card.StarScore > foe.StarScore) margin += 1;

            return margin;
        }

        public static int CardCrushPenalty(NalathniTradingCard card, NalathniTradingCard foe) {
			return new[]{
				card.SunScore - foe.SunScore,
				card.MoonScore - foe.MoonScore,
				card.StarScore - foe.StarScore,
			}.Min();
        }

        public static bool Draw(int who) {
            var deck = CardZones[who, DeckZone];
            if (deck.Count == 0) return false;

            var hand = CardZones[who, HandZone];
			var index = Stat.Rnd2.Next(deck.Count);
			var card = deck[index];

			deck.RemoveAt(index);
			hand.Add(card);
			
            return true;
        }
        
        public static void ScorePoints(int who, int points = 0) {
			Scores[who] += points;
        }
		
        public static void ResolvePlayerTurn() {
            if (!Draw(PlayerCards)) {
                LatestGameNews += "&RYou can't draw from an empty deck.\n";
			} 
            
            Popup.Show(LatestGameNews);
            LatestGameNews = "";
            LatestGameNews += BoardState();
            
            var playerHand = CardZones[PlayerCards,HandZone];
            int playerCard = -1;
            if (playerHand.Count == 0) {
                LatestGameNews += "&RYou have no cards in your hand.\n";
                Popup.Show(LatestGameNews);
            } else {
                LatestGameNews += "\n\n&CPlay a card:";
                var texts = playerHand.Select(c => c.ParentObject.DisplayName).ToArray();

                playerCard = Popup.PickOption(
					Options: texts,
					Intro: LatestGameNews,
					MaxWidth: 78,
					RespectOptionNewlines: true
				);
            }

            LatestGameNews = "";
            if (playerCard != -1) {
				ResolveCardAgainstPlayer(playerCard, OpponentCards, PlayerCards);
			}

            var playerField = CardZones[PlayerCards, FieldZone];
            ScorePoints(PlayerCards, playerField.Count);
            LatestGameNews += "\n&yYou score " + playerField.Count + " renown for your fielded cards.\n";
        }
        
        public static string BoardState() {
            string output = "&Y" + Opponent.The + Opponent.DisplayNameStripped
				+ " (" + Scores[OpponentCards] + " renown):\n";
            foreach (var card in CardZones[OpponentCards, FieldZone]) {
                output += card.ParentObject.DisplayName + "\n";
            }
            
            output += "\n&Y" + The.Player.DisplayNameStripped
				+ " (" + Scores[PlayerCards] + " renown):\n";
            foreach (var card in CardZones[PlayerCards, FieldZone]) {
                output += card.ParentObject.DisplayName + "\n";
            }

            return output;
        }
	}
}