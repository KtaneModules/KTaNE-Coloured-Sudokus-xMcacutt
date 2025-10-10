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
    public class RegularCipher : Cipher
    {
        private float hue = 0f;
        private readonly TextAsset regularSudokuData;

        private class CellCandidate
        {
            public int Row;
            public int Col;
            public char GridLetter;
            public int Shift;
        }

        public RegularCipher(TextAsset regularSudokuData)
        {
            this.regularSudokuData = regularSudokuData;
            Name = "Regular";
        }
        
        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            List<string> keyWords;
            string letterShifts;
            var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);

            var debugLogs = new List<string>();
            
            var indexedRegularPuzzles = JsonConvert.DeserializeObject<List<RegularSudokuData>>(regularSudokuData.text)
                .Select((puzzle, index) => new { Puzzle = puzzle, Index = index })
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();
            var currentPuzzle = indexedRegularPuzzles.PickRandom().Puzzle;

            var sudokuGrid = currentPuzzle.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            debugLogs.Add("Sudoku grid: " + string.Join("", sudokuGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            debugLogs.Add("Letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            
            var sudokuGridUnsolved = currentPuzzle.grid.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();

            var encryptedWord = "";
            var encryptableLetters = new HashSet<char>();
            for (var row = 0; row < 9; row++)
            {
                for (var col = 0; col < 9; col++)
                {
                    var shift = sudokuGrid[row][col];
                    var gridLetter = letterGrid[row][col];
                    if (gridLetter == '#')
                        continue;

                    var offset = (gridLetter - 'A' + shift) % 26;
                    var candidatePlain = (char)('A' + offset);

                    encryptableLetters.Add(candidatePlain);
                }
            }
            Func<string, int> scorer = word => word.All(c => encryptableLetters.Contains(c)) ? 1 : 0;
            var unencryptedWord = new Data().PickBestWord(6, scorer);
            encryptedWord = "";
            var encryptionData = "";
            
            foreach (var plain in unencryptedWord)
            {
                var candidates = new List<CellCandidate>();
                for (var row = 0; row < 9; row++)
                {
                    for (var col = 0; col < 9; col++)
                    {
                        var gridLetter = letterGrid[row][col];
                        if (gridLetter == '#')
                            continue;

                        var shift = sudokuGrid[row][col];
                        var offset = (gridLetter - 'A' + shift) % 26;
                        var candidatePlain = (char)('A' + offset);

                        if (candidatePlain == plain)
                        {
                            candidates.Add(new CellCandidate
                            {
                                Row = row,
                                Col = col,
                                GridLetter = gridLetter,
                                Shift = shift
                            });
                        }
                    }
                }
                
                if (candidates.Count == 0)
                    continue;
                
                var chosen = candidates.PickRandom();
                var clue = $"{(char)('A' + chosen.Col)}{chosen.Row + 1}";

                debugLogs.Add($"Clue {clue}: Letter Grid: '{chosen.GridLetter}' + Sudoku Grid: {chosen.Shift} => '{plain}'");

                encryptedWord += chosen.GridLetter;
                encryptionData += clue;
            }
            yield return null;

            var screenTexts = new List<string>();
            screenTexts.Add(encryptionData.Substring(0, 6));
            screenTexts.Add(encryptionData.Substring(6, 6));
            screenTexts.Add(letterShifts);
            screenTexts.AddRange(keyWords);
            var cluesString = "";
            for (var row = 0; row < 9; row++)
            {
                for (var col = 0; col < 9; col++)
                {
                    var cell = sudokuGridUnsolved[row][col];
                    if (cell != 0) cluesString += $"{(char)('A' + col)}{row + 1}{cell}";
                }
            }
            var chunks = Enumerable.Range(0, cluesString.Length / 9)
                .Select(i => cluesString.Substring(i * 9, 9))
                .ToList();
            screenTexts.AddRange(chunks);
            var result = new CipherResult()
            {
                EncryptedWord = encryptedWord,
                UnencryptedWord = unencryptedWord,
                ScreenTexts = screenTexts,
                DebugLogs =  debugLogs
            };
            onComplete(result);
            yield break;
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            hue += Time.deltaTime * 0.1f;
            if (hue > 1f) hue = 0f;
            renderer.material.color = Color.HSVToRGB(hue, 0.4f, 0.7f);
        }
    }
}