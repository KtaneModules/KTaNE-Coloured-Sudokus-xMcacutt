using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Utility;
using Words;

namespace KModkit.Ciphers
{
    public class LittleKillerCipher : Cipher
    {
        private int[][] sudokuGrid;

        public LittleKillerCipher(LittleKillerSudokuData sudokuData)
        {
            var expandedGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            sudokuGrid = expandedGrid;
            Name = "Little Killer";
        }

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            CipherResult result = null;
            bool finished = false;

            new System.Threading.Thread(() =>
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
            var success = true;
            var data = new Data();
            const int attempts = 30;
            for (var i = 0; i < attempts; i++)
            {
                List<string> keyWords;
                string letterShifts;
                var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);

                var targetWord = data.PickBestWord(6, w => w.Distinct().Count() == w.Length ? 1 : 0);
                if (string.IsNullOrEmpty(targetWord))
                    return ErrorResult;

                var letterPositions = new Dictionary<char, List<CellRef>>();
                var selectedPositions = new Dictionary<char, CellRef>();
                foreach (var ch in targetWord)
                    letterPositions[ch] = FindLetterPositions(letterGrid, ch);
                
                var initialPosition = letterPositions[targetWord[0]].First();
                selectedPositions[targetWord[0]] = initialPosition;
                var usedRows = new List<int> { initialPosition.Row };
                var rowShifts = new int[9];
                
                var additionalShift = 0;
                if (initialPosition.Col > 3)
                    additionalShift = 10 - initialPosition.Col;
                rowShifts[initialPosition.Row] = additionalShift;
                additionalShift++;

                foreach (var letter in targetWord.Skip(1))
                {
                    var letterPosition = letterPositions[letter].FirstOrDefault(x => !usedRows.Contains(x.Row));
                    if (letterPosition == null)
                    {
                        success = false;
                        break;
                    }
                    selectedPositions[letter] = letterPosition;
                    usedRows.Add(letterPosition.Row);
                    var shiftToFirst = (initialPosition.Col - letterPosition.Col).Mod9();
                    rowShifts[letterPosition.Row] = (shiftToFirst + additionalShift).Mod9();
                    additionalShift++;
                }
                if (!success)
                {
                    if (i != attempts - 1)
                        success = true;
                    continue;
                }
                
                for (var rowIndex = 0; rowIndex < rowShifts.Length; rowIndex++)
                    if (!usedRows.Contains(i))
                        rowShifts[i] = Random.Next(0, 9);

                for (var rowIndex = 0; rowIndex < 9; rowIndex++)
                    letterGrid[rowIndex] = ShiftRow(letterGrid[rowIndex], rowShifts[rowIndex]);

                foreach (var letter in targetWord)
                    selectedPositions[letter].Col = (selectedPositions[letter].Col + rowShifts[selectedPositions[letter].Row]).Mod9();
                
                var columnShifts = new int[9];
                initialPosition = selectedPositions[targetWord[0]]; 
                var usedColumns = new List<int> { initialPosition.Col };

                foreach (var letter in targetWord)
                {
                    var letterPosition = selectedPositions[letter];
                    var shift = (initialPosition.Row - letterPosition.Row).Mod9();
                    columnShifts[letterPosition.Col] = shift;
                    usedColumns.Add(letterPosition.Col);
                }
                
                for (var colIndex = 0; colIndex < 9; colIndex++)
                    if (!usedColumns.Contains(colIndex))
                        columnShifts[colIndex] = Random.Next(0, 9);
                
                for (var colIndex = 0; colIndex < 9; colIndex++)
                {
                    var colShift = columnShifts[colIndex];
                    if (colShift == 0) continue;
                    var newCol = new char[9];
                    for (var rowIndex = 0; rowIndex < 9; rowIndex++)
                    {
                        var newRowIndex = (rowIndex + colShift).Mod9();
                        newCol[newRowIndex] = letterGrid[rowIndex][colIndex];
                    }
                    for (var rowIndex = 0; rowIndex < 9; rowIndex++)
                    {
                        letterGrid[rowIndex][colIndex] = newCol[rowIndex];
                    }
                }
                
                var finalGrid = new StringBuilder();
                finalGrid.AppendLine("Final Letter Grid:");
                for (var rowIndex = 0; rowIndex < 9; rowIndex++)
                {
                    finalGrid.AppendLine(new string(letterGrid[rowIndex]));
                }
                
                string rowShiftsString = "";
                string colShiftsString = "";

                for (var row = 0; row < 9; row++)
                {
                    var shift = rowShifts[row];
                    if (shift == 0) shift = 9;
                    rowShiftsString += Array.IndexOf(sudokuGrid[row], shift) + 1;
                }
                
                for (var col = 0; col < 9; col++)
                {
                    var shift = columnShifts[col];
                    if (shift == 0) shift = 9;
                    var column = sudokuGrid.Select(row => row[col]).ToArray();
                    var index = Array.IndexOf(column, shift);
                    colShiftsString += index + 1;
                }
                
                var screenTexts = new List<string>() {
                    rowShiftsString, colShiftsString, letterShifts, 
                    keyWords[0], keyWords[1], keyWords[2]
                };
                
                List<string> debugLogs = new List<string>();
                debugLogs.Add($"Keywords: {string.Join("", keyWords.ToArray())}");
                debugLogs.Add("Letter shifts: " + letterShifts);
                debugLogs.Add("Letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
                debugLogs.Add("Final grid: " + finalGrid);

                return new CipherResult
                {
                    UnencryptedWord = targetWord,
                    ScreenTexts = screenTexts,
                    DebugLogs = debugLogs,
                };
            }

            return new CipherResult
            {
                UnencryptedWord = null,
                ScreenTexts = new List<string>() { "Error" }
            };
        }
        
        private char[] ShiftRow(char[] row, int shift)
        {
            var result = new char[9];
            for (var i = 0; i < 9; i++)
            {
                var newIndex = (i + shift).Mod9();
                result[newIndex] = row[i];
            }
            return result;
        }

        private List<CellRef> FindLetterPositions(char[][] grid, char letter)
        {
            var list = new List<CellRef>();
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (grid[r][c] == letter)
                        list.Add(new CellRef(r, c));
            return list;
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Blue;
        }
    }
}
