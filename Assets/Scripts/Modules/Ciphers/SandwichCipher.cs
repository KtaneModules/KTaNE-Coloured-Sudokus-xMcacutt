using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Utility;
using Words;
using Random = System.Random;

namespace KModkit.Ciphers
{
    public class SandwichCipher : Cipher
    {
        private int[][] sudokuGrid;
        private MazeNode[] mazeArray;
        private List<string> outerGrid;
        private List<string> innerGrid;
        private char[][] letterGrid;
        private Dictionary<char, List<CellRef>> positionsByLetter;

        public SandwichCipher(SandwichSudokuData sudokuData)
        {
            var expandedGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            sudokuGrid = expandedGrid;
            Name = "Sandwich";
            IsMaze = true;
        }
        
        private int level;
        private int currentCell;
        private int currentBox;
        private int validBox;
        public override string Move(int moduleId, string direction)
        {
            switch (direction)
            {
                case "R":
                    if (level == 0)
                        currentBox = (currentBox + 1).Mod9();
                    else
                        currentCell = (currentCell + 1).Mod9();
                    break;
                case "L":
                    if (level == 0)
                        currentBox = (currentBox - 1).Mod9();
                    else
                        currentCell = (currentCell - 1).Mod9();
                    break;
                case "D":
                    if (level != 0 || currentBox != validBox)
                        return "*";
                    level++;
                    break;
            }
            return level == 0 ? outerGrid[currentBox] : innerGrid[currentCell];
        }

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
                catch (Exception)
                {
                    result = new CipherResult
                    {
                        EncryptedWord = null,
                        UnencryptedWord = null,
                        ScreenTexts = new List<string> { "Error" }
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
            var data = new Data();

            List<string> keyWords;
            string letterShifts;
            do
            {
                letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
            } while (!data.AnyWordMatches(6, w => ScoreWord(w, letterGrid) == 1));
            List<string> debugLogs = new List<string>();
            debugLogs.Add($"Keywords: {string.Join("", keyWords.ToArray())}");
            debugLogs.Add("Letter shifts: " + letterShifts);
            debugLogs.Add("Letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            var unencryptedWord = data.PickBestWord(6, w => ScoreWord(w, letterGrid));

            outerGrid = new List<string>();
            innerGrid = new List<string>();

            validBox = random.Next(9);
            for (var box = 0; box < 9; box++)
            {
                var row = 1 + 3 * (box / 3);
                var col = 1 + 3 * (box % 3);
                var value = sudokuGrid[row][col];
                if (outerGrid.Count == validBox)
                    outerGrid.Add(data.AnyWordMatches(4, 9, w => CalculateSandwichSum(w) == value)
                        ? data.PickBestWord(4, 8, w => CalculateSandwichSum(w) == value ? 1 : 0)
                        : "Error");
                else
                    outerGrid.Add(data.PickBestWord(4, 8, w => CalculateSandwichSum(w) != value ? 1 : 0));
            }
            

            innerGrid = new List<string>();
            var usedRows = new HashSet<int>();
            letterPositions = new Dictionary<char, CellRef>();
            FindAssignment(unencryptedWord, 0, usedRows);
            
            List<string> validInnerGridWords = new List<string>();
            
            var baseRow = (validBox / 3) * 3;
            var baseCol = (validBox % 3) * 3;
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 3; col++)
                {
                    var value = sudokuGrid[baseRow + row][baseCol + col];
                    if (unencryptedWord.Any(c => letterPositions[c].Row == row * 3 + col))
                    {
                        var letter = unencryptedWord.First(c => letterPositions[c].Row == row * 3 + col);
                        var word = data.PickBestWord(4, 8, w => 
                            w[0] % 9 == (letter - 'A') % 9 && CalculateSandwichSum(w) == value ? 1 : 0);
                        innerGrid.Add(word);
                        validInnerGridWords.Add(word);
                    }
                    else
                        innerGrid.Add(data.PickBestWord(4, 8, w => CalculateSandwichSum(w) != value ? 1 : 0));
                }
            }
            
            debugLogs.Add($"Valid Outer Grid Word: {outerGrid[validBox]}");
            debugLogs.Add("Valid Inner Grid Words: " + string.Join(", ", validInnerGridWords.ToArray()));

            var screenTexts = new List<string>();
            screenTexts.Add(letterShifts);
            screenTexts.AddRange(keyWords);

            return new CipherResult()
            {
                UnencryptedWord = unencryptedWord,
                ScreenTexts = screenTexts,
                DebugLogs = debugLogs
            };
        }

        private Dictionary<char, CellRef> letterPositions;
        bool FindAssignment(string word, int index, HashSet<int> usedRows)
        {
            positionsByLetter = new Dictionary<char, List<CellRef>>();
            for (var r = 0; r < letterGrid.Length; r++)
            {
                for (var c = 0; c < letterGrid[r].Length; c++)
                {
                    var ch = letterGrid[r][c];
                    if (!word.Contains(ch)) 
                        continue;
                    if (!positionsByLetter.ContainsKey(ch))
                        positionsByLetter.Add(ch, new List<CellRef>());
                    positionsByLetter[ch].Add(new CellRef(r, c));
                }
            }
            
            if (index == word.Length)
                return true;
            var letter = word[index];
            foreach (var cellRef in positionsByLetter[letter].Where(cellRef => !usedRows.Contains(cellRef.Row)))
            {
                letterPositions[letter] = cellRef;
                usedRows.Add(cellRef.Row);
                if (FindAssignment(word, index + 1, usedRows))
                    return true;
                usedRows.Remove(cellRef.Row);
                letterPositions.Remove(letter);
            }
            return false;
        }
        
        private int CalculateSandwichSum(string word)
        {
            var sum = word.Skip(1).Take(word.Length - 2).Select(x => (int)(x - 'A' + 1)).Sum() % 9;
            return sum == 0 ? 9 : sum;
        }

        private int ScoreWord(string word, char[][] letterGrid)
        {
            letterPositions = new Dictionary<char, CellRef>();
            return FindAssignment(word, 0, new HashSet<int>()) ? 1 : 0;
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Red;
        }
    }
}