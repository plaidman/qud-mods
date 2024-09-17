using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qud.UI;
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
		private static string OppName = "";
		private static readonly StringBuilder NewsBuilder = new();
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
			OppName = Opponent.The + Opponent.DisplayNameOnlyDirectAndStripped;

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
					NewsBuilder.Clear();
					NewsBuilder.Append(BoardState()).Append("{{R|=oppName= wins the game!}}");
					NewsBuilder.StartReplace()
						.AddReplacer("oppName", OppName)
						.Execute();
					Popup.Show(NewsBuilder.ToString());
					break;
				}

				var playerCard = PickPlayerCard();

				if (playerCard == -3) {
					NewsBuilder.Clear();
					NewsBuilder.Append(BoardState())
						.Append("You throw your cards on the table.\n{{R|=oppName= wins the game!}}");
					NewsBuilder.StartReplace()
						.AddReplacer("oppName", OppName)
						.Execute();
					Popup.Show(NewsBuilder.ToString());
					break;
				}

				ResolvePlayerTurn(playerCard);

				if (Scores[PlayerCards] >= PointsToWin) {
					NewsBuilder.Clear();
					NewsBuilder.Append(BoardState()).Append("{{G|You win the game!}}");
					Popup.Show(NewsBuilder.ToString());

					var card = SSR_Card.CreateCard(Opponent);
					Popup.Show("You get a card as a souvenir of your victory:\n\n" + card.DisplayName);
					The.Player.TakeObject(card);
					break;
				}
			}

			return true;
		}

		private static void ResolveOpponentTurn() {
			NewsBuilder.Clear();

			if (!Draw(OpponentCards)) {
				NewsBuilder.Append("{{C|=oppName= can't draw from an empty deck.}}\n");
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
				NewsBuilder.Append(ResolveCardAgainstPlayer(npcCard, PlayerCards, OpponentCards));
			} else {
				NewsBuilder.Append("{{C|=oppName= doesn't have any cards to play.}}\n\n");
			}

			var npcFieldCount = CardZones[OpponentCards, FieldZone].Count;
			ScorePoints(OpponentCards, npcFieldCount);
			NewsBuilder.Append("=oppName= scores {{Y|=fieldCount= renown}} for =oppIts= fielded cards.");

			NewsBuilder.StartReplace()
				.AddReplacer("oppName", OppName)
				.AddReplacer("fieldCount", npcFieldCount.ToString())
				.AddReplacer("oppIts", Opponent.its)
				.Execute();
			Popup.Show(NewsBuilder.ToString());
		}

		private static int PickPlayerCard() {
			NewsBuilder.Clear();
			NewsBuilder.Append(BoardState());

			if (!Draw(PlayerCards)) {
				NewsBuilder.Append("{{R|You can't draw from an empty deck.}}\n\n");
			}

			var playerHand = CardZones[PlayerCards, HandZone];
			if (playerHand.Count == 0) {
				NewsBuilder.Append("{{R|You have no cards in your hand.}}");
				Popup.Show(NewsBuilder.ToString());
				return -1;
			}

			NewsBuilder.Append("{{C|Play a card:}}");

			var cardNames = playerHand
				.Select(c => "{{|" + c.ParentObject.DisplayName + "}}")
				.ToArray();
			var forfeitButton = new QudMenuItem[] {
				new() {
					text = "Forfeit Game",
					command = "option:-2",
					hotkey = "Cancel",
				},
			};

			int playerCard = -2;
			while (playerCard == -2) {
				playerCard = Popup.PickOption(
					Options: cardNames,
					Intro: NewsBuilder.ToString(),
					MaxWidth: 78,
					RespectOptionNewlines: true,
					Buttons: forfeitButton
				);

				if (playerCard == -2 || playerCard == -1) {
					Popup.WaitNewPopupMessage(
						message: "Really quit playing?",
						buttons: PopupMessage.YesNoButton,
						DefaultSelected: 1,
						callback: delegate (QudMenuItem item) {
							if (item.command == "Yes") playerCard = -3;
						}
					);
				}
			}
			
			return playerCard;
		}
		
		private static void ResolvePlayerTurn(int playerCard) {
			NewsBuilder.Clear();

			if (playerCard != -1) {
				NewsBuilder.Append(ResolveCardAgainstPlayer(playerCard, OpponentCards, PlayerCards));
			}

			var playerFieldCount = CardZones[PlayerCards, FieldZone].Count;
			ScorePoints(PlayerCards, playerFieldCount);
			NewsBuilder.Append("You score {{Y|=fieldCount= renown}} for your fielded cards.");

			NewsBuilder.StartReplace()
				.AddReplacer("fieldCount", playerFieldCount.ToString())
				.Execute();
			Popup.Show(NewsBuilder.ToString());
		}

		private static StringBuilder ResolveCardAgainstPlayer(int yourCardIndex, int foe, int you) {
			var builder = new StringBuilder();
			var yourHand = CardZones[you, HandZone];
			var yourCard = yourHand[yourCardIndex];

			if (you == PlayerCards) {
				builder.Append("You play");
			} else {
				builder.Append("=oppName= plays");
			}
			builder.Append(" {{Y|=cardName=}}.\n\n");

			var enemyField = CardZones[foe, FieldZone];
			var cardResults = new StringBuilder();
			for (var i = enemyField.Count-1; i >= 0; i--) {
				var cardResult = ResolveCardAgainstCard(yourCard, i, foe, you);
				if (cardResult == null) continue;
				cardResults.Append(cardResult);
			}

			if (cardResults.Length > 0) {
				builder.Append("=youPoss= card:\n");
				builder.Append(cardResults);
				builder.Append("\n");
			}

			yourHand.RemoveAt(yourCardIndex);
			CardZones[you, FieldZone].Add(yourCard);

			builder.StartReplace()
				.AddReplacer("oppName", OppName)
				.AddReplacer("cardName", yourCard.ParentObject.DisplayName)
				.AddReplacer("youPoss", NamePoss(you))
				.Execute();
			return builder;
		}

		private static StringBuilder ResolveCardAgainstCard(SSR_Card yourCard, int foeCardIndex, int foe, int you) {
			var enemyField = CardZones[foe, FieldZone];
			var enemyDeck = CardZones[foe, DeckZone];
			var foeCard = enemyField[foeCardIndex];
			int margin = CardStatsAgainstCard(yourCard, foeCard);
			int points = 0;
			string verb = " doesn't affect ";

			if (margin < 2) {
				return null;
			}

			if (margin == 3) {
				// returned to deck
				enemyField.RemoveAt(foeCardIndex);
				enemyDeck.Add(foeCard);

				points = CardCrushPenalty(yourCard, foeCard);
				verb = "{{R|crushes}}";
			}

			if (margin == 2) {
				if (foeCard.PointValue > yourCard.PointValue) {
					// banished from play
					enemyField.RemoveAt(foeCardIndex);

					points = 1 + foeCard.PointValue - yourCard.PointValue;
					verb = "{{O|topples}}";
				} else {
					// returned to deck
					enemyField.RemoveAt(foeCardIndex);
					enemyDeck.Add(foeCard);

					points = 1;
					verb = "{{W|vanquishes}}";
				}
			}

			ScorePoints(you, points);
			var builder = new StringBuilder("~ =verb= =foePoss= {{|=foeCard=}}. ({{Y|=points= renown}})\n");
			builder.StartReplace()
				.AddReplacer("verb", verb)
				.AddReplacer("foePoss", NamePoss(foe, true))
				.AddReplacer("foeCard", foeCard.ShortDisplayName)
				.AddReplacer("points", points.ToString("+0;-#"))
				.Execute();
			return builder;
		}

		private static StringBuilder BoardState() {
			var boardState = new StringBuilder("=oppName= ({{Y|=oppScore= renown}}):\n");
			foreach (var card in CardZones[OpponentCards, FieldZone]) {
				boardState.Append("- {{|").Append(card.ParentObject.DisplayName).Append("}}\n");
			}

			boardState.Append("\n=playerName= ({{Y|=playerScore= renown}}):\n");
			foreach (var card in CardZones[PlayerCards, FieldZone]) {
				boardState.Append("- {{|").Append(card.ParentObject.DisplayName).Append("}}\n");
			}

			boardState.StartReplace()
				.AddReplacer("oppName", OppName)
				.AddReplacer("oppScore", Scores[OpponentCards].ToString())
				.AddReplacer("playerName", The.Player.DisplayNameOnlyDirectAndStripped)
				.AddReplacer("playerScore", Scores[PlayerCards].ToString())
				.Execute();

			return boardState.Append("\n");
		}

		private static string NamePoss(int who, bool lowercase = false) {
			if (who == PlayerCards) {
				return lowercase ? "your" : "Your";
			}

			return lowercase
				? Grammar.MakePossessive(Opponent.the + Opponent.DisplayNameOnlyDirectAndStripped)
				: Grammar.MakePossessive(OppName);
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

			// crushing will be preferred if the time to win is reduced by more than the penalty per card
			// e.g.
			//   if the player has 2 turns to win from fielded renown,
			//   removing enemies will raise that to 5 turns to win,
			//   but average -2 points per card removed, then choose to crush
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
	}
}