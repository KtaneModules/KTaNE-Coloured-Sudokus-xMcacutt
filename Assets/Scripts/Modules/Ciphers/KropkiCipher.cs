using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Utility;
using Words;
using Random = System.Random;

namespace KModkit.Ciphers
{
    public class KropkiCipher : Cipher
    {
        private int[][] sudokuGrid;
        private Random random = new Random();

        public KropkiCipher(KropkiSudokuData sudokuData)
        {
            sudokuGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            Name = "Kropki";
            IsMaze = false;
            TwitchPlaysPoints = 20;
        }

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            var data = new Data();
            var screenTexts = new List<string>();
            var player1 = data.PickWord(5);
            var player2 = data.PickWord(5);
            screenTexts.Add(player1);
            screenTexts.Add("~♠~ vs ~♠~");
            screenTexts.Add(player2);

            var player1Wins = 0;
            var player2Wins = 0;
            var gameIndex = 0;
            
            var debugLogs = new List<string>();
            
            foreach (var row in sudokuGrid)
            {
                if (player1Wins > 4 || player2Wins > 4)
                    break;
                gameIndex++;
                debugLogs.Add($"Game Number {gameIndex}");
                var player1Cards = new List<Card> { new Card(row[0]), new Card(row[1]) };
                debugLogs.Add($"{player1} Cards: " + string.Join(", ", player1Cards.Select(card => card.ToString()).ToArray()));
                var player2Cards = new List<Card> { new Card(row[2]), new Card(row[3]) };
                debugLogs.Add($"{player2} Cards: " + string.Join(", ", player2Cards.Select(card => card.ToString()).ToArray()));
                var flop = new List<Card> { new Card(row[4]), new Card(row[5]), new Card(row[6]) };
                debugLogs.Add("Flop: " + string.Join(", ", flop.Select(card => card.ToString()).ToArray()));
                var turn = new Card(row[7]);
                debugLogs.Add("Turn: " + turn);
                var river = new Card(row[8]);
                debugLogs.Add("River: " + river);

                bool p1Win, p2Win;
                int p1Score, p2Score;
                
                var tableCards = new List<Card>(flop);
                RunStage(player1Cards, player2Cards, tableCards, out p1Win, out p2Win, out p1Score, out p2Score);
                debugLogs.Add($"Flop - {player1} Hand Score: {p1Score}, {player2} Hand Score: {p2Score}");
                if (p1Win)
                {
                    player1Wins++;
                    debugLogs.Add($"Game {gameIndex} winner is {player1} - {player2} folds after flop");
                    continue;
                }
                if (p2Win)
                {
                    player2Wins++;
                    debugLogs.Add($"Game {gameIndex} winner is {player2} - {player1} folds after flop");
                    continue;
                }

                tableCards.Add(turn);
                RunStage(player1Cards, player2Cards, tableCards, out p1Win, out p2Win, out p1Score, out p2Score);
                debugLogs.Add($"Turn - {player1} Hand Score: {p1Score}, {player2} Hand Score: {p2Score}");
                if (p1Win)
                {
                    player1Wins++;
                    debugLogs.Add($"Game {gameIndex} winner is {player1} - {player2} folds after turn");
                    continue;
                }
                if (p2Win)
                {
                    player2Wins++;
                    debugLogs.Add($"Game {gameIndex} winner is {player2} - {player1} folds after turn");
                    continue;
                }
                
                tableCards.Add(river);
                RunStage(player1Cards, player2Cards, tableCards, out p1Win, out p2Win, out p1Score, out p2Score);
                debugLogs.Add($"River - {player1} Hand Score: {p1Score}, {player2} Hand Score: {p2Score}");
                if (p1Score == p2Score)
                {
                    debugLogs.Add($"Neither player has a higher score, using card values.");
                    p1Score = Math.Max(row[0], row[1]); 
                    p2Score = Math.Max(row[2], row[3]);
                }
                debugLogs.Add($"Tie Break - {player1} Score: {p1Score}, {player2} Score: {p2Score}");
                if (p1Score > p2Score)
                {
                    player1Wins++;
                    debugLogs.Add($"Game {gameIndex} winner is {player1}");
                }
                else if (p1Score < p2Score)
                {
                    player2Wins++;
                    debugLogs.Add($"Game {gameIndex} winner is {player2}");
                }
            }

            string unencryptedWord;
            if (player1Wins > player2Wins)
            {
                debugLogs.Add($"{player1} wins the match after {player1Wins + player2Wins} games.");
                unencryptedWord = player1 + (char)('A' + player1Wins + player2Wins - 1);
            }
            else
            {
                debugLogs.Add($"{player2} wins the match after {player1Wins + player2Wins} games.");
                unencryptedWord = player2 + (char)('A' + player1Wins + player2Wins - 1);
            }
            
            var result = new CipherResult()
            {
                UnencryptedWord = unencryptedWord,
                ScreenTexts = screenTexts,
                DebugLogs = debugLogs
            };
            onComplete(result);
            yield break;
        }

        private void RunStage(List<Card> player1Cards, List<Card> player2Cards, List<Card> tableCards, 
            out bool player1Wins, out bool player2Wins, out int player1Score, out int player2Score)
        {
            player1Score = ScoreHand(player1Cards, tableCards);
            player2Score = ScoreHand(player2Cards, tableCards);
            player1Wins = player1Score > player2Score + 1;
            player2Wins = player2Score > player1Score + 1;
        }
        
        private int ScoreHand(List<Card> player, List<Card> table)
        {
            var all = new List<Card>();
            all.AddRange(player);
            all.AddRange(table);
            
            var ranks = all.Select(c => c.Rank).ToList();
            // Full House
            if (ranks.Any(r => ranks.Count(x => x == r) == 3) &&
                ranks.Any(r => ranks.Count(x => x == r) == 2))
                return 5;
            
            // Straight Flush
            var order = new Dictionary<string, int> { { "A", 1 }, { "2", 2 }, { "3", 3 } };
            if (new[] { "R", "G", "B" }.Select(col => all
                    .Where(c => c.Colour == col)
                    .Select(c => order[c.Rank])
                    .ToList()).Select(colCards => new HashSet<int>(colCards)).Any(set => set.SetEquals(new[] { 1, 2, 3 })))
                return 4;
            
            // Three of a Kind
            if (ranks.Any(r => ranks.Count(x => x == r) >= 3))
                return 3;
            
            // Two Pair
            if (ranks.Distinct().Count(r => ranks.Count(x => x == r) >= 2) >= 2)
                return 2;
            
            // Set
            if ((from combo in Combinations(all, 3) 
                    let row = combo.Select(c => c.Rank).ToList() 
                    let col = combo.Select(c => c.Colour).ToList() 
                    where row.Distinct().Count() == 3 && col.Distinct().Count() == 3 select row).Any())
                return 1;
            
            return 0;
        }
        
        private IEnumerable<List<T>> Combinations<T>(List<T> list, int length)
        {
            if (length == 0) 
                yield return new List<T>();
            else
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var head = list[i];
                    var tail = list.Skip(i + 1).ToList();
                    foreach (var tailCombo in Combinations(tail, length - 1))
                    {
                        tailCombo.Insert(0, head);
                        yield return tailCombo;
                    }
                }
            }
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Green;
        }
    }
    
    class Card
    {
        public readonly string Rank;
        public readonly string Colour;
        public Card(string rank, string colour) { Rank = rank; Colour = colour; }
        public override string ToString() { return Rank + Colour; }

        public Card(int value)
        {
            Rank = ValueToRank(value);
            Colour = ValueToColour(value);
        }

        static string ValueToRank(int value)
        {
            switch ((value - 1) % 3)
            {
                case 0:
                    return "A";
                case 1:
                    return "2";
                case 2:
                    return "3";
            }
            return value.ToString();
        }

        static string ValueToColour(int value)
        {
            switch ((value - 1) / 3)
            {
                case 0:
                    return "R";
                case 1:
                    return "G";
                case 2:
                    return "B";
            }
            return value.ToString();
        }
    }
}