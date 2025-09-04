using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using Utility;
using Words;
using Random = System.Random;

namespace KModkit.Ciphers
{
    public class ThermoCipher : Cipher
    {
        private float hue = 0f;
        private int[][] sudokuGrid;
        private List<List<int>> thermos;
        private readonly Random random = new Random();
        private volatile CipherResult generationResult;
        private volatile bool generationComplete;

        public ThermoCipher(ThermoSudokuData sudokuData)
        {
            sudokuGrid = sudokuData.solution
                .Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            thermos = sudokuData.thermos;
        }

        public string Name => "Thermo";

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            CipherResult result = null;
            var finished = false;
            
            new Thread(() =>
            {
                try
                {
                    result = RunGeneration();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error during puzzle generation: " + ex);
                    result = new CipherResult
                    {
                        EncryptedWord = null,
                        UnencryptedWord = null,
                        ScreenTexts = new List<string> { "ERROR" }
                    };
                }
                finally
                {
                    finished = true;
                }
            }).Start();
            
            while (!finished)
                yield return null;
            
            onComplete(result);
        }

        private CipherResult RunGeneration()
        {
            var testedWords = new HashSet<string>();
            for (var attempt = 0; attempt < 60; attempt++)
            {
                List<string> keyWords;
                string letterShifts;
                var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
                var unencryptedWord = new Data().PickBestWord(6, w => ScoreWord(w, letterGrid, testedWords));
                testedWords.Add(unencryptedWord);
                
                var matches = letterGrid
                    .SelectMany((row, rowIndex) => row.Select((ch, colIndex) => new { rowIndex, colIndex, ch }))
                    .Where(x => unencryptedWord.Contains(x.ch))
                    .ToList();
                var forbiddenNumbers = new HashSet<int>();
                foreach (var match in matches)
                    forbiddenNumbers.Add(sudokuGrid[match.rowIndex][match.colIndex]);

                var validStops = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                foreach (var value in forbiddenNumbers)
                    validStops.Remove(value);
                validStops = validStops.OrderBy(_ => random.Next()).Take(3).ToList();
                var validStopsString = string.Join("", validStops.Select(x => x.ToString()).ToArray());

                var snakeOrder = new List<CellRef>();
                for (var r = 8; r >= 0; r--)
                {
                    if ((8 - r) % 2 == 0)
                        for (var c = 0; c < 9; c++)
                            snakeOrder.Add(new CellRef(r, c));
                    else
                        for (var c = 8; c >= 0; c--)
                            snakeOrder.Add(new CellRef(r, c));
                }

                var letterSnakeArray = snakeOrder.Select(pos => letterGrid[pos.Row][pos.Col]).ToArray();
                var sudokuSnakeArray = snakeOrder.Select(pos => sudokuGrid[pos.Row][pos.Col]).ToArray();

                var coordToSnakeIndex = new Dictionary<int, int>();
                for (var i = 0; i < snakeOrder.Count; i++)
                    coordToSnakeIndex[snakeOrder[i].Row * 9 + snakeOrder[i].Col] = i;
                
                var thermoBulbs = new Dictionary<int, int>();
                foreach (var thermo in thermos)
                {
                    var bulbCoord = coordToSnakeIndex[thermo[0]];
                    var endCoord = coordToSnakeIndex[thermo[thermo.Count - 1]];
                    thermoBulbs[bulbCoord] = endCoord;
                }
                
                //Debug.Log(string.Join("", letterSnakeArray.Select(x => x.ToString()).ToArray()));
                //Debug.Log(string.Join("", sudokuSnakeArray.Select(x => x.ToString()).ToArray()));
                var result = FindValidPath(unencryptedWord, letterSnakeArray, sudokuSnakeArray, validStops, thermoBulbs);
                if (!result.Success) continue;

                var screenTexts = new List<string>();
                for (var i = 0; i < result.EncryptionData.Length; i += 8)
                    screenTexts.Add(result.EncryptionData.Substring(i, Math.Min(8, result.EncryptionData.Length - i)));
                screenTexts.Add(validStopsString);
                screenTexts.Add(letterShifts);
                screenTexts.AddRange(keyWords);
                return new CipherResult
                {
                    EncryptedWord = null,
                    UnencryptedWord = unencryptedWord,
                    ScreenTexts = screenTexts,
                };
            }

            return new CipherResult
            {
                EncryptedWord = null,
                UnencryptedWord = null,
                ScreenTexts = new List<string> { "ERROR" },
            };
        }

        private struct PathResult
        {
            public bool Success;
            public string EncryptionData;
        }

        private PathResult FindValidPath(string word, char[] letterSnake, int[] sudokuSnake, List<int> validStops, Dictionary<int, int> thermoBulbs)
        {
            var currentPosition = -1;
            var currentLetter = 0;
            var encryptionData = "";
            var attempts = 0;
            var visited = new HashSet<int>(); 
            const int maxAttempts = 100;

            while (currentLetter < word.Length && attempts < maxAttempts)
            {
                var validMoves = new List<int>();
                for (var d = 1; d <= 9; d++)
                {
                    var nextPos = currentPosition + d;
                    if (nextPos >= 81 || nextPos < 0 || visited.Contains(nextPos))
                        continue;
                    
                    if (nextPos >= letterSnake.Length || nextPos >= sudokuSnake.Length)
                        continue;

                    var letter = letterSnake[nextPos];
                    var number = sudokuSnake[nextPos];
                    if (letter == word[currentLetter] && !thermoBulbs.ContainsKey(nextPos))
                    {
                        validMoves.Clear();
                        validMoves.Add(d);
                        break;
                    }
                    if (letter == '#' || validStops.Contains(number) || thermoBulbs.ContainsKey(nextPos))
                        validMoves.Add(d);
                }

                if (validMoves.Count == 0)
                {
                    if (encryptionData.Length == 0)
                        return new PathResult { Success = false };
                    
                    var lastMoveIndex = encryptionData.Length - 1;
                    encryptionData = encryptionData.Substring(0, lastMoveIndex);
                    visited.Remove(currentPosition);
                    currentLetter = currentLetter > 0 ? currentLetter - 1 : 0;
                    
                    currentPosition = -1;
                    foreach (var nextPos in encryptionData.Select(c => c - '0').Select(revMove => currentPosition + revMove))
                    {
                        if (nextPos >= 81 || nextPos < 0 || nextPos >= letterSnake.Length)
                            return new PathResult { Success = false };
                        currentPosition = nextPos;
                        int revJumpTo;
                        if (!thermoBulbs.TryGetValue(currentPosition, out revJumpTo) || visited.Contains(revJumpTo))
                            continue;
                        if (revJumpTo >= 81 || revJumpTo < 0 || revJumpTo >= letterSnake.Length)
                            return new PathResult { Success = false };
                        currentPosition = revJumpTo;
                    }
                    attempts++;
                    continue;
                }
                
                var move = validMoves[random.Next(validMoves.Count)];
                currentPosition += move;
                
                int jumpTo;
                if (thermoBulbs.TryGetValue(currentPosition, out jumpTo) && !visited.Contains(jumpTo))
                {
                    if (jumpTo >= 81 || jumpTo < 0 || jumpTo >= letterSnake.Length)
                        return new PathResult { Success = false };
                    currentPosition = jumpTo;
                    visited.Add(jumpTo);
                }

                encryptionData += move.ToString();
                visited.Add(currentPosition);
                if (letterSnake[currentPosition] == word[currentLetter])
                    currentLetter++;
                attempts++;
            }

            return new PathResult
            {
                Success = currentLetter == word.Length,
                EncryptionData = encryptionData
            };
        }

        private int ScoreWord(string word, char[][] letterGrid, HashSet<string> testedWords)
        {
            if (testedWords.Contains(word) || word.Any(letter => word.Count(x => x == letter) > 2))
                return 0;
            
            var matches = letterGrid
                .SelectMany((row, rowIndex) => row.Select((ch, colIndex) => new { rowIndex, colIndex, ch }))
                .Where(x => word.Contains(x.ch))
                .ToList();

            var numberSet = new HashSet<int>();
            foreach (var match in matches)
                numberSet.Add(sudokuGrid[match.rowIndex][match.colIndex]);
            if (numberSet.Count > 5)
                return 0;

            var snakeOrder = new List<CellRef>();
            for (var r = 8; r >= 0; r--)
            {
                if ((8 - r) % 2 == 0)
                    for (var c = 0; c < 9; c++)
                        snakeOrder.Add(new CellRef(r, c));
                else
                    for (var c = 8; c >= 0; c--)
                        snakeOrder.Add(new CellRef(r, c));
            }

            var coordToSnakeIndex = new Dictionary<int, int>();
            for (var i = 0; i < snakeOrder.Count; i++)
                coordToSnakeIndex[snakeOrder[i].Row * 9 + snakeOrder[i].Col] = i;
            
            var letterSnakeArray = snakeOrder.Select(pos => letterGrid[pos.Row][pos.Col]).ToArray();
            var letterPositions = new List<int>[word.Length];
            for (var i = 0; i < word.Length; i++)
                letterPositions[i] = new List<int>();

            for (var i = 0; i < letterSnakeArray.Length; i++)
            {
                var letter = letterSnakeArray[i];
                for (var j = 0; j < word.Length; j++)
                {
                    if (letter == word[j])
                        letterPositions[j].Add(i);
                }
            }
            
            return 9 - numberSet.Count;
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Black;
        }
    }
}