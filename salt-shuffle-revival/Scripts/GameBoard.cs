using System.Collections.Generic;
using System.Linq;
using System.Text;
using XRL;
using XRL.Language;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Plaidman.SaltShuffleRevival {
	public class GameBoard {
		private const int PointsToWin = 50;
		private const int PlayerCards = 0;
		private const int OpponentCards = 1;
		private const int DeckZone = 0;
		private const int HandZone = 1;
		private const int FieldZone = 2;

		private static GameObject Opponent = null;
		private static string OppoNameLower = "";
		private static string OppoNameUpper = "";
		private static StringBuilder LatestGameNews = new();
		private static readonly List<SSR_Card>[,] CardZones = new List<SSR_Card>[2,3];
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
			OppoNameUpper = Opponent.The + Opponent.DisplayNameStripped;
			OppoNameLower = Opponent.the + Opponent.DisplayNameStripped;

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
					LatestGameNews.Clear();
					LatestGameNews.Append(BoardState());
					LatestGameNews.Append("{{R|").Append(OppoNameUpper).Append(" wins the game!}}");
					Popup.Show(LatestGameNews.ToString());
					break;
				}

				ResolvePlayerTurn();

				if (Scores[PlayerCards] >= PointsToWin) {
					LatestGameNews.Clear();
					LatestGameNews.Append(BoardState());
					LatestGameNews.Append("{{G|You win the game!}}");
					Popup.Show(LatestGameNews.ToString());

					var card = SSR_Card.CreateCard(Opponent);
					Popup.Show("You get a card as a souvenir of your victory:\n\n" + card.DisplayName);
					The.Player.TakeObject(card);
					break;
				}

			}

			return true;
		}

		private static void ResolveOpponentTurn() {
			LatestGameNews.Clear();
			if (!Draw(OpponentCards)) {
				LatestGameNews.Append("{{C|").Append(OppoNameUpper).Append(" can't draw from an empty deck.}}\n");
			}

			var npcHand = CardZones[OpponentCards, HandZone];
			List<int> bestIndexes = new();
			if (npcHand.Count > 0) {
				int bestOutcome = int.MinValue;

				for (var i = 0; i < npcHand.Count; i++) {
					var candidate = npcHand[i];
					int value = AICardScoreAgainstPlayer(candidate);

					if (value > bestOutcome) {
						bestIndexes.Clear();
						bestIndexes.Add(i);
						bestOutcome = value;
					}
					
					if (value == bestOutcome) {
						bestIndexes.Add(i);
					}
				}
			}

			if (bestIndexes.Count > 0) {
				var npcCard = bestIndexes.GetRandomElementCosmetic();
				ResolveCardAgainstPlayer(npcCard, PlayerCards, OpponentCards);
			} else {
				LatestGameNews.Append("{{C|").Append(OppoNameUpper).Append(" doesn't have any cards to play.}}\n\n");
			}

			var npcFieldCount = CardZones[OpponentCards, FieldZone].Count;
			ScorePoints(OpponentCards, npcFieldCount);
			LatestGameNews.Append(OppoNameUpper).Append(" scores {{Y|").Append(npcFieldCount)
				.Append(" renown}} for ").Append(Opponent.its).Append(" fielded cards.");
			
			Popup.Show(LatestGameNews.ToString());
		}

		private static void ResolvePlayerTurn() {
			LatestGameNews.Clear();
			LatestGameNews.Append(BoardState());

			if (!Draw(PlayerCards)) {
				LatestGameNews.Append("{{R|You can't draw from an empty deck.}}\n");
			}

			var playerHand = CardZones[PlayerCards,HandZone];
			int playerCard = -1;
			if (playerHand.Count == 0) {
				LatestGameNews.Append("{{R|You have no cards in your hand.}}");
				Popup.Show(LatestGameNews.ToString());
			} else {
				LatestGameNews.Append("{{C|Play a card:}}");
				var cardNames = playerHand.Select(c => "{{|" + c.ParentObject.DisplayName + "}}").ToArray();

				playerCard = Popup.PickOption(
					Options: cardNames,
					Intro: LatestGameNews.ToString(),
					MaxWidth: 78,
					RespectOptionNewlines: true
				);
			}

			LatestGameNews.Clear();
			if (playerCard != -1) {
				ResolveCardAgainstPlayer(playerCard, OpponentCards, PlayerCards);
			}

			var playerFieldCount = CardZones[PlayerCards, FieldZone].Count;
			ScorePoints(PlayerCards, playerFieldCount);
			LatestGameNews.Append("You score {{Y|").Append(playerFieldCount)
				.Append(" renown}} for your fielded cards.");

			Popup.Show(LatestGameNews.ToString());
		}

		private static void ResolveCardAgainstPlayer(int yourCardIndex, int foe, int you) {
			var yourHand = CardZones[you, HandZone];
			var yourCard = yourHand[yourCardIndex];

			if (you == PlayerCards) {
				LatestGameNews.Append("You play ");
			} else {
				LatestGameNews.Append(OppoNameUpper).Append(" plays ");
			}
			LatestGameNews.Append("{{Y|").Append(yourCard.ParentObject.DisplayName)
				.Append("}}.\n\n");

			var cardOutput = new StringBuilder();
			var enemyField = CardZones[foe, FieldZone];
			for (var i = enemyField.Count-1; i >= 0; i--) {
				cardOutput.Append(ResolveCardAgainstCard(yourCard, i, foe, you));
			}

			if (cardOutput.Length > 0) {
				LatestGameNews.Append(cardOutput).Append("\n");
			}

			yourHand.RemoveAt(yourCardIndex);
			CardZones[you, FieldZone].Add(yourCard);
		}

		private static StringBuilder ResolveCardAgainstCard(SSR_Card yourCard, int foeCardIndex, int foe, int you) {
			var enemyField = CardZones[foe, FieldZone];
			var enemyDeck = CardZones[foe, DeckZone];
			var foeCard = enemyField[foeCardIndex];
			int margin = CardStatsAgainstCard(yourCard, foeCard);
			int points = 0;
			string verb = " doesn't affect ";
			
			if (margin < 2) {
				return new();
			}

			if (margin == 3) {
				// returned to deck
				enemyField.RemoveAt(foeCardIndex);
				enemyDeck.Add(foeCard);

				points = CardCrushPenalty(yourCard, foeCard);
				verb = " {{R|crushes}} ";
			}

			if (margin == 2) {
				if (foeCard.PointValue > yourCard.PointValue) {
					// banished from play
					enemyField.RemoveAt(foeCardIndex);

					points = 1 + foeCard.PointValue - yourCard.PointValue;
					verb = " {{O|topples}} ";
				} else {
					// returned to deck
					enemyField.RemoveAt(foeCardIndex);
					enemyDeck.Add(foeCard);

					points = 1;
					verb = " {{W|vanquishes}} ";
				}
			}
			
			ScorePoints(you, points);
			return new StringBuilder("- ").Append(NamePoss(you)).Append(" {{|").Append(yourCard.ShortDisplayName)
				 .Append("}}").Append(verb).Append(NamePoss(foe)).Append(" {{|").Append(foeCard.ShortDisplayName)
				.Append("}}. ({{Y|").Append(points.ToString("+0;-#")).Append(" renown}})\n");
		}
		
		private static string NamePoss(int who, bool lowercase = false) {
			if (who == PlayerCards) {
				return lowercase ? "your" : "Your";
			}

			return lowercase
				? Grammar.MakePossessive(OppoNameLower)
				: Grammar.MakePossessive(OppoNameUpper);
		}

		private static int AICardScoreAgainstPlayer(SSR_Card card) {
			int totalPoints = 0;
			int totalRemoved = 0;
			
			var fieldCards = CardZones[PlayerCards, FieldZone];
			var curScore = Scores[PlayerCards];
			int numTurns = AINumTurnsLeft(curScore, fieldCards.Count);

			foreach (var foe in fieldCards) {
				(int points, int removed) = AICardScoreAgainstCard(card, foe);
				totalPoints += points;
				totalRemoved += removed;
			}
			
			int newNumTurns = AINumTurnsLeft(curScore, fieldCards.Count - totalRemoved);
			int turnsDiffPoints = (newNumTurns - numTurns) * totalRemoved;

			return totalPoints + turnsDiffPoints;
		}
		
		private static int AINumTurnsLeft(int score, int cards) {
			int turns = 0;

			while (score < 50) {
				turns++; cards++;
				score += cards;
			}
			
			return turns;
		}
		
		private static (int, int) AICardScoreAgainstCard(SSR_Card card, SSR_Card foe) {
			int margin = CardStatsAgainstCard(card, foe);

			if (margin == 3) {
				return (CardCrushPenalty(card, foe), 1);
			}

			if (margin == 2) {
				if (foe.PointValue <= card.PointValue) return (1, 1);
				else return (1 + foe.PointValue - card.PointValue, 1);
			}

			return (0, 0);
		}

		private static int CardStatsAgainstCard(SSR_Card card, SSR_Card foe) {
			int margin = 0;

			if (card.SunScore > foe.SunScore) margin += 1;
			if (card.MoonScore > foe.MoonScore) margin += 1;
			if (card.StarScore > foe.StarScore) margin += 1;

			return margin;
		}

		private static int CardCrushPenalty(SSR_Card card, SSR_Card foe) {
			return new[]{
				card.SunScore - foe.SunScore,
				card.MoonScore - foe.MoonScore,
				card.StarScore - foe.StarScore,
			}.Min() * -1;
		}

		private static bool Draw(int who) {
			var deck = CardZones[who, DeckZone];
			if (deck.Count == 0) return false;

			var hand = CardZones[who, HandZone];
			var index = Stat.Rnd2.Next(deck.Count);
			var card = deck[index];

			deck.RemoveAt(index);
			hand.Add(card);

			return true;
		}

		private static void ScorePoints(int who, int points = 0) {
			Scores[who] += points;
		}

		private static StringBuilder BoardState() {
			var boardState = new StringBuilder(OppoNameUpper).Append(" ({{Y|")
				.Append(Scores[OpponentCards]).Append(" renown}}):\n");
			foreach (var card in CardZones[OpponentCards, FieldZone]) {
				boardState.Append("- {{|").Append(card.ParentObject.DisplayName).Append("}}\n");
			}

			boardState.Append("\n").Append(The.Player.DisplayNameStripped)
				.Append(" ({{Y|").Append(Scores[PlayerCards]).Append(" renown}}):\n");
			foreach (var card in CardZones[PlayerCards, FieldZone]) {
				boardState.Append("- {{|").Append(card.ParentObject.DisplayName).Append("}}\n");
			}

			return boardState.Append("\n");
		}
	}
}