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
    public class Walls
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
        
        public override string ToString()
        {
            var output = "URDL";
            if (Up)
                output = output.Replace("U", "");
            if (Down)
                output = output.Replace("D", "");
            if (Left)
                output = output.Replace("L", "");
            if (Right)
                output = output.Replace("R", "");
            return output;
        }
    }
    
    public class JigsawCipher : Cipher
    {
        private float hue = 0f;
        private int[][] sudokuGrid;
        private List<int> boxes;
        private char[][] hiddenGrid;
        private char[][] combinedGrid;
        private Random random = new Random();
        private CellRef currentCell;

        public JigsawCipher(JigsawSudokuData sudokuData)
        {
            var expandedGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            sudokuGrid = expandedGrid;
            boxes = sudokuData.boxes;
            Name = "Jigsaw";
            IsMaze = true;
        }
        
        public override string Move(int moduleId, string direction)
        {
            var currentBox = boxes[currentCell.Row * 9 + currentCell.Col];
            if (direction == "S")
                return combinedGrid[currentCell.Row][currentCell.Col].ToString();
            switch (direction)
            {
                case "U":
                    if (currentCell.Row == 0)
                        return combinedGrid[currentCell.Row][currentCell.Col].ToString();
                    currentCell.Row -= 1;
                    break;
                case "R":
                    if (currentCell.Col == 8)
                        return combinedGrid[currentCell.Row][currentCell.Col].ToString();
                    currentCell.Col += 1;
                    break;
                case "D":
                    if (currentCell.Row == 8)
                        return combinedGrid[currentCell.Row][currentCell.Col].ToString();
                    currentCell.Row += 1;
                    break;
                case "L":
                    if (currentCell.Col == 0)
                        return combinedGrid[currentCell.Row][currentCell.Col].ToString();
                    currentCell.Col -= 1;
                    break;
            }
            var newBox = boxes[currentCell.Row * 9 + currentCell.Col];
            if (currentBox == newBox)
                return combinedGrid[currentCell.Row][currentCell.Col].ToString();
            var caesarShift = sudokuGrid[currentCell.Row][currentCell.Col];
            for (var r = 0; r < 9; r++)
            {
                for (var c = 0; c < 9; c++)
                {
                    var value = combinedGrid[r][c];
                    if (value == '#')
                        continue;
                    var valNum = value - 'A' + 1;
                    valNum = (valNum + caesarShift - 1 + 26) % 26 + 1;
                    combinedGrid[r][c] = (char)('A' + valNum - 1);
                }
            }
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Moved across a boundary onto a {caesarShift}");
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"New grid after shift: {string.Join("", combinedGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray())}");
            return combinedGrid[currentCell.Row][currentCell.Col].ToString();
        }

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            var data = new Data();
            List<string> keyWords;
            string letterShifts;
            List<string> hiddenKeywords;
            string hiddenLetterShifts;
            var initialOffset = random.Next(26);
            hiddenGrid = GenerateLetterGrid(out hiddenKeywords, out hiddenLetterShifts);
            List<string> debugLogs = new List<string>();
            debugLogs.Add("Hidden grid: " + string.Join("", hiddenGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
            debugLogs.Add($"Keywords: {string.Join("", keyWords.ToArray())}");
            debugLogs.Add("Letter shifts: " + letterShifts);
            debugLogs.Add("Letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            combinedGrid = letterGrid.Select(row => row.ToArray()).ToArray();
            
            for (var r = 0; r < 9; r++)
            {
                for (var c = 0; c < 9; c++)
                {
                    if (letterGrid[r][c] == '#')
                        continue;
                    if (hiddenGrid[r][c] == '#')
                    {
                        combinedGrid[r][c] = '#';
                        continue;
                    }
                    var letterGridValue = letterGrid[r][c] - 'A' + 1;
                    var hiddenGridValue = hiddenGrid[r][c] - 'A' + 1;
                    var sudokuValue = sudokuGrid[r][c];
                    combinedGrid[r][c] = (char)('A' + (letterGridValue + hiddenGridValue + sudokuValue + initialOffset - 1) % 26);
                }
            }
            
            var word = data.PickBestWord(6, ScoreWord);
            debugLogs.Add("Initial Combined Grid: " + string.Join("", combinedGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            currentCell = new CellRef(4, 4);
            
            var selectedCells = word
                .Select(letter =>
                {
                    var candidates = Enumerable.Range(0, 9)
                        .SelectMany(row => Enumerable.Range(0, 9) 
                            .Where(col => hiddenGrid[row][col] == letter)
                            .Select(col => new CellRef(row, col)))
                        .ToList();

                    return candidates.Count > 0 
                        ? candidates[random.Next(candidates.Count)] 
                        : null; 
                })
                .Where(cell => cell != null)
                .ToList();
            
            var encryptionData = selectedCells
                .Aggregate("", (current, cellRef) => current + ((char)('A' + cellRef.Col) + (cellRef.Row + 1).ToString()));
            
            var screenTexts = new List<string>();
            for (var i = 0; i < encryptionData.Length; i += 6)
                screenTexts.Add(encryptionData.Substring(i, Math.Min(6, encryptionData.Length - i)));
            screenTexts.Add(initialOffset.ToString());
            screenTexts.Add(letterShifts);
            screenTexts.AddRange(keyWords);
            onComplete(new CipherResult
            {
                UnencryptedWord = word,
                ScreenTexts = screenTexts,
                DebugLogs = debugLogs
            });

            yield break;
        }

        private int ScoreWord(string word)
        {
            var coordinates = word.Select(letter =>
                {
                    var candidates = Enumerable.Range(0, 9)
                        .SelectMany(row => Enumerable.Range(0, 9) 
                            .Where(col => hiddenGrid[row][col] == letter)
                            .Select(col => new CellRef(row, col)))
                        .ToList();

                    return candidates.Count > 0 
                        ? candidates[random.Next(candidates.Count)] 
                        : null; 
                })
                .Where(cell => cell != null)
                .ToList();
            if (coordinates.Any(coordinate => combinedGrid[coordinate.Row][coordinate.Col] == '#'))
                return 0;
            var set = new HashSet<char>();
            foreach (var letter in word)
                set.Add(letter);
            return set.Count < 6 ? 0 : 1;
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Yellow;
        }
    }
}