using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Utility;
using Words;
using Random = System.Random;

public class CellRef
{
    public int Row;
    public int Col;
    public CellRef(int row, int col) { Row = row; Col = col; }
}

namespace KModkit.Ciphers
{
    public class SkyscraperCipher : Cipher
    {
        private float hue = 0f;
        private int[][] sudokuGrid;

        public SkyscraperCipher(SkyscraperSudokuData sudokuData)
        {
            var expandedGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            sudokuGrid = expandedGrid;
        }

        public string Name => "Regular";

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            List<string> keyWords;
            string letterShifts;
            var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
            var random = new Random();
            var maxAttempts = 30;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var unencryptedWord = new Data().PickBestWord(6, w => ScoreWord(w, letterGrid, sudokuGrid));
                List<char> encryptedLetters = new List<char>();
                var encryptionData = "";
                var puzzleValid = true;

                foreach (var letter in unencryptedWord)
                {
                    var success = false;
                    var candidates = Enumerable.Range(0, letterGrid.Length)
                        .SelectMany(row => Enumerable.Range(0, letterGrid[row].Length)
                            .Where(col => letterGrid[row][col] == letter && sudokuGrid[row][col] != 1)
                            .Select(col => new CellRef(row, col)))
                        .ToList();

                    if (candidates.Count == 0)
                    {
                        puzzleValid = false;
                        break;
                    }

                    var cellRef = candidates.PickRandom();
                    var targetHeight = sudokuGrid[cellRef.Row][cellRef.Col];

                    var direction = random.Next(4);
                    for (var dirAttempt = 0; dirAttempt < 4 && !success; dirAttempt++)
                    {
                        var dir = (direction + dirAttempt) % 4;
                        var farCell = FindFurthestValidCell(cellRef, dir, targetHeight, sudokuGrid, letterGrid);

                        if (farCell == null) continue;
                        var encLetter = letterGrid[farCell.Row][farCell.Col];
                        if (encLetter == '#') continue;
                        encryptedLetters.Add(encLetter);
                        encryptionData += (char)('A' + farCell.Col) + (farCell.Row + 1).ToString() + DirectionToString((dir + 2) % 4);
                        success = true;
                    }

                    if (success) continue;
                    puzzleValid = false;
                    break;
                }

                yield return null;
                if (!puzzleValid) continue;
                var screenTexts = new List<string>();
                screenTexts.Clear();
                screenTexts.Add(encryptionData.Substring(0, 9));
                screenTexts.Add(encryptionData.Substring(9, 9));
                screenTexts.Add(letterShifts);
                screenTexts.AddRange(keyWords);
                var encryptedWord = new string(encryptedLetters.ToArray());
                Debug.Log(new string(letterGrid.SelectMany(row => row).ToArray()));
                Debug.Log(string.Join("", sudokuGrid.SelectMany(row => row).Select(n => n.ToString()).ToArray()));
                var result = new CipherResult()
                {
                    EncryptedWord = encryptedWord,
                    UnencryptedWord = unencryptedWord,
                    ScreenTexts = screenTexts,
                };
                onComplete(result);
                yield break;
            }

            Debug.LogWarning("Failed to generate a valid puzzle after multiple attempts.");
            var resultFail = new CipherResult()
            {
                EncryptedWord = null,
                UnencryptedWord = null,
                ScreenTexts = new List<string>() {"ERROR"},
            };
            onComplete(resultFail);
            yield break;
        }

        private CellRef FindFurthestValidCell(CellRef start, int direction, int targetHeight, int[][] solvedGrid, char[][] letterGrid)
        {
            int dr = 0, dc = 0;
            if (direction == 0) dr = -1;
            else if (direction == 1) dc = 1;
            else if (direction == 2) dr = 1;
            else if (direction == 3) dc = -1;

            int rows = solvedGrid.Length;
            int cols = solvedGrid[0].Length;
            int r = start.Row;
            int c = start.Col;
            int maxBetween = 0;
            CellRef lastValid = null;

            for (int steps = 1; steps < rows * cols; steps++)
            {
                r = (r + dr + rows) % rows;
                c = (c + dc + cols) % cols;
                int value = solvedGrid[r][c];
                if (letterGrid[r][c] == '#') return null;
                if (value >= targetHeight)
                {
                    if (steps == 1) return null;
                    return lastValid;
                }
                if (value > maxBetween && value < targetHeight)
                {
                    maxBetween = value;
                    lastValid = new CellRef(r, c);
                }
            }

            return lastValid;
        }

        private string DirectionToString(int dir)
        {
            switch (dir)
            {
                case 0: return "U";
                case 1: return "R";
                case 2: return "D";
                case 3: return "L";
                default: return string.Empty;
            }
        }

        private int ScoreWord(string word, char[][] letterGrid, int[][] solvedGrid)
        {
            var score = 0;
            foreach (var letter in word)
            {
                var validCells = 0;
                for (var r = 0; r < letterGrid.Length; r++)
                {
                    for (var c = 0; c < letterGrid[r].Length; c++)
                    {
                        if (letterGrid[r][c] != letter || solvedGrid[r][c] == 1) continue;
                        for (var dir = 0; dir < 4; dir++)
                        {
                            if (FindFurthestValidCell(new CellRef(r, c), dir, solvedGrid[r][c], solvedGrid,
                                    letterGrid) == null) continue;
                            validCells++;
                            break;
                        }
                    }
                }
                score += validCells;
            }
            return score;
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Purple;
        }
    }
}