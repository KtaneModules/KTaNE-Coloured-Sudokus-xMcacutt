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
    public class PalindromeCipher : Cipher
    {
        private PalindromeSudokuData sudokuData;
        private CellRef currentCell;
        private MazeNode[] mazeArray;

        public PalindromeCipher(PalindromeSudokuData sudokuData)
        {
            this.sudokuData = sudokuData;
            Name = "Palindrome";
            IsMaze = false;
        }

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            const int maxAttempts = 50;
            var data = new Data();

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                List<string> keyWords;
                string letterShifts;
                var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
                
                List<string> debugLogs = new List<string>();
                debugLogs.Add($"Keywords: {string.Join("", keyWords.ToArray())}");
                debugLogs.Add("Letter shifts: " + letterShifts);
                debugLogs.Add("Letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
                    
                var unencryptedWord = data.PickBestWord(6,
                    w => w.Distinct().Count() == w.Length ? 1 : 0);
                var encryptedWord = "";

                var palindromeLines = sudokuData.lines
                    .Select(x => x.Select(y => new CellRef(y / 9, y % 9)).ToList())
                    .ToList();

                foreach (var letter in unencryptedWord)
                {
                    var candidates = Enumerable.Range(0, letterGrid.Length)
                        .SelectMany(row => Enumerable.Range(0, letterGrid[row].Length)
                            .Where(col => letterGrid[row][col] == letter)
                            .Select(col => new CellRef(row, col)))
                        .ToList();

                    var letterRef = candidates.PickRandom();
                    debugLogs.Add($"Selected letter {letter} at {letterRef}");
                    letterRef = new CellRef(letterRef.Col, letterRef.Row);
                    debugLogs.Add($"Flipped position to {letterRef}");
                    
                    foreach (var line in palindromeLines)
                    {
                        for (var l = 0; l < line.Count; l++)
                        {
                            if (line[l].Row != letterRef.Row || line[l].Col != letterRef.Col) 
                                continue;
                            if (2 * l != line.Count - 1)
                                letterRef = line[line.Count - l - 1];
                            debugLogs.Add($"Position is on palindrome line. Moving to {letterRef}");
                            goto ReflectionComplete;
                        }
                    }
                    ReflectionComplete:
                    debugLogs.Add($"Encrypted letter {letter} to {letterGrid[letterRef.Row][letterRef.Col]}");
                    encryptedWord += letterGrid[letterRef.Row][letterRef.Col];
                }

                if (HasAlternativeDecryptions(encryptedWord, palindromeLines, unencryptedWord, letterGrid, data))
                    continue;
                
                var screenTexts = new List<string>();
                screenTexts.Add(encryptedWord);
                screenTexts.Add(letterShifts);
                screenTexts.AddRange(keyWords);

                var result = new CipherResult()
                {
                    EncryptedWord = encryptedWord,
                    UnencryptedWord = unencryptedWord,
                    ScreenTexts = screenTexts,
                    DebugLogs = debugLogs
                };

                onComplete(result);
                yield break;
            }
        }

        private bool HasAlternativeDecryptions(string encryptedWord, List<List<CellRef>> palindromeLines, 
            string unencryptedWord, char[][] letterGrid, Data data)
        {
            var possibleLetters = new List<HashSet<char>>();
            foreach (var letter in encryptedWord)
            {
                var options = new HashSet<char>();
                var candidates = Enumerable.Range(0, letterGrid.Length)
                    .SelectMany(row => Enumerable.Range(0, letterGrid[row].Length)
                        .Where(col => letterGrid[row][col] == letter)
                        .Select(col => new CellRef(row, col)))
                    .ToList();
                foreach (var candidate in candidates)
                {
                    var newRef = candidate;
                    foreach (var line in palindromeLines)
                    {
                        for (var l = 0; l < line.Count; l++)
                        {
                            if (line[l].Row != candidate.Row || line[l].Col != candidate.Col) 
                                continue;
                            if (2 * l != line.Count - 1)
                                newRef = line[line.Count - l - 1];
                            goto ReflectionComplete;
                        }
                    }
                    ReflectionComplete:
                    options.Add(letterGrid[newRef.Col][newRef.Row]);
                }
                possibleLetters.Add(options);
            }
            
            return data.AnyWordMatches(6, w => w != unencryptedWord
                                               && !w.Where((t, i) => !possibleLetters[i].Contains(t)).Any());
        }


        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Orange;
        }
    }
}