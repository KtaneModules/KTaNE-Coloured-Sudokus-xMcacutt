using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KModkit;
using KModkit.Ciphers;
using UnityEditor.VersionControl;
using UnityEngine;
using Words;
using Random = System.Random;

namespace Modules.Ciphers
{
    public class AntiKnightCipher : Cipher
    {
        private int[][] sudokuGrid;

        public AntiKnightCipher(AntiKnightSudokuData sudokuData)
        {
            var expandedGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            sudokuGrid = expandedGrid;
        }

        public string Name => "Regular";

        private enum PieceType
        {
            None,
            Knight,
            Bishop,
            King,
            Rook
        }

        private class PieceCandidate
        {
            public readonly PieceType Type;
            public readonly int Row;
            public readonly int Column;
            public readonly int EmptyBoardAttackCount;

            public PieceCandidate(PieceType type, int row, int column, int count)
            {
                Type = type;
                Row = row;
                Column = column;
                EmptyBoardAttackCount = count;
            }
        }

        private static PieceType MapNumberToPiece(int num)
        {
            switch (num)
            {
                case 1: return PieceType.King;
                case 2: 
                case 3: return PieceType.Bishop;
                case 4: 
                case 5: return PieceType.Rook;
                case 6: 
                case 7: 
                case 8:
                case 9: return PieceType.Knight;
                default: return PieceType.None;
            }
        }

        private int ComputeAttackCount(List<PieceCandidate> selectedPieces, int rows, int columns,
            out List<int> multiAttackedPositions)
        {
            multiAttackedPositions = null;
            var attackCounts = new int[rows * columns];
            var occupiedPositions = new bool[rows * columns];
            foreach (var piece in selectedPieces)
                occupiedPositions[piece.Row * columns + piece.Column] = true;

            foreach (var piece in selectedPieces)
            {
                int row = piece.Row, column = piece.Column;
                switch (piece.Type)
                {
                    case PieceType.King:
                        for (var deltaRow = -1; deltaRow <= 1; deltaRow++)
                        for (var deltaColumn = -1; deltaColumn <= 1; deltaColumn++)
                        {
                            if (deltaRow == 0 && deltaColumn == 0) continue;
                            int targetRow = row + deltaRow, targetColumn = column + deltaColumn;
                            if (targetRow < 0 || targetRow >= rows || targetColumn < 0 || targetColumn >= columns)
                                continue;
                            var targetIndex = targetRow * columns + targetColumn;
                            if (!occupiedPositions[targetIndex]) attackCounts[targetIndex]++;
                        }

                        break;

                    case PieceType.Knight:
                        int[][] knightOffsets =
                        {
                            new[] { 2, 1 }, new[] { 2, -1 }, new[] { -2, 1 }, new[] { -2, -1 },
                            new[] { 1, 2 }, new[] { 1, -2 }, new[] { -1, 2 }, new[] { -1, -2 }
                        };
                        foreach (var offset in knightOffsets)
                        {
                            int targetRow = row + offset[0], targetColumn = column + offset[1];
                            if (targetRow < 0 || targetRow >= rows || targetColumn < 0 || targetColumn >= columns)
                                continue;
                            var targetIndex = targetRow * columns + targetColumn;
                            if (!occupiedPositions[targetIndex]) attackCounts[targetIndex]++;
                        }

                        break;

                    case PieceType.Bishop:
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, 1, 1, rows, columns);
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, 1, -1, rows, columns);
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, -1, 1, rows, columns);
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, -1, -1, rows, columns);
                        break;
                    
                    case PieceType.Rook:
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, 1, 0, rows, columns);
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, -1, 0, rows, columns);
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, 0, 1, rows, columns);
                        AddRayIncrements(attackCounts, occupiedPositions, row, column, 0, -1, rows, columns);
                        break;
                }
            }

            var multiAttackCount = attackCounts.Count(t => t >= 2);
            if (multiAttackCount != 6) return multiAttackCount;
            multiAttackedPositions = new List<int>(6);
            for (var i = 0; i < attackCounts.Length; i++)
                if (attackCounts[i] >= 2)
                    multiAttackedPositions.Add(i);

            return multiAttackCount;
        }

        private void AddRayIncrements(int[] attackCounts, bool[] occupiedPositions, int row, int column, int deltaRow,
            int deltaColumn, int rows, int columns)
        {
            int currentRow = row + deltaRow, currentColumn = column + deltaColumn;
            while (currentRow >= 0 && currentRow < rows && currentColumn >= 0 && currentColumn < columns)
            {
                var currentIndex = currentRow * columns + currentColumn;
                if (occupiedPositions[currentIndex]) break;
                attackCounts[currentIndex]++;
                currentRow += deltaRow;
                currentColumn += deltaColumn;
            }
        }

        private int EmptyBoardAttackCount(PieceType type, int row, int column, int rows, int columns)
        {
            var count = 0;
            switch (type)
            {
                case PieceType.King:
                    for (var deltaRow = -1; deltaRow <= 1; deltaRow++)
                    for (var deltaColumn = -1; deltaColumn <= 1; deltaColumn++)
                        if (!(deltaRow == 0 && deltaColumn == 0))
                        {
                            int targetRow = row + deltaRow, targetColumn = column + deltaColumn;
                            if (targetRow >= 0 && targetRow < rows && targetColumn >= 0 && targetColumn < columns)
                                count++;
                        }

                    break;
                case PieceType.Knight:
                    int[][] knightOffsets =
                    {
                        new[] { 2, 1 }, new[] { 2, -1 }, new[] { -2, 1 }, new[] { -2, -1 },
                        new[] { 1, 2 }, new[] { 1, -2 }, new[] { -1, 2 }, new[] { -1, -2 }
                    };
                    foreach (var offset in knightOffsets)
                    {
                        int targetRow = row + offset[0], targetColumn = column + offset[1];
                        if (targetRow >= 0 && targetRow < rows && targetColumn >= 0 && targetColumn < columns) count++;
                    }

                    break;
                case PieceType.Bishop:
                    count += CountRayCells(row, column, 1, 1, rows, columns);
                    count += CountRayCells(row, column, 1, -1, rows, columns);
                    count += CountRayCells(row, column, -1, 1, rows, columns);
                    count += CountRayCells(row, column, -1, -1, rows, columns);
                    break;
                
                case PieceType.Rook:
                    count += CountRayCells(row, column, 1, 0, rows, columns);
                    count += CountRayCells(row, column, -1, 0, rows, columns);
                    count += CountRayCells(row, column, 0, 1, rows, columns);
                    count += CountRayCells(row, column, 0, -1, rows, columns);
                    break;
            }

            return count;
        }

        private int CountRayCells(int row, int column, int deltaRow, int deltaColumn, int rows, int columns)
        {
            var count = 0;
            int currentRow = row + deltaRow, currentColumn = column + deltaColumn;
            while (currentRow >= 0 && currentRow < rows && currentColumn >= 0 && currentColumn < columns)
            {
                count++;
                currentRow += deltaRow;
                currentColumn += deltaColumn;
            }

            return count;
        }

        private bool TryFindAnagramWord(char[] letters, out string foundWord)
        {
            foundWord = null;
            var data = new Data();
            Func<string, int> scoreFunc = w => w.OrderBy(c => c).SequenceEqual(letters.OrderBy(c => c)) ? 1 : 0;
            var candidate = data.PickBestWord(6, scoreFunc);
            if (candidate == null || !candidate.OrderBy(c => c).SequenceEqual(letters.OrderBy(c => c)))
                return false;
            foundWord = candidate;
            return true;
        }

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            CipherResult result = null;
            bool finished = false;
            
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
            List<string> keyWords;
            string letterShifts;
            var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
            var random = new Random();
            const int maxAttempts = 60;

            var rows = letterGrid.Length;
            var columns = letterGrid[0].Length;

            var allCandidates = Enumerable.Range(0, sudokuGrid.Length)
                .SelectMany(r => Enumerable.Range(0, sudokuGrid[0].Length)
                    .Select(c => new { r, c, type = MapNumberToPiece(sudokuGrid[r][c]) }))
                .Where(x => x.type != PieceType.None)
                .Select(x => new PieceCandidate(x.type, x.r, x.c, EmptyBoardAttackCount(x.type, x.r, x.c, rows, columns)))
                .ToList();

            if (allCandidates.Count == 0)
            {
                return new CipherResult
                {
                    EncryptedWord = null,
                    UnencryptedWord = null,
                    ScreenTexts = new List<string> { "ERROR" }
                };
            }

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidatePoolSize = Math.Min(allCandidates.Count, 20 + attempt * 2);
                var pool = allCandidates
                    .OrderByDescending(p => p.EmptyBoardAttackCount)
                    .ThenBy(p => random.Next())
                    .Take(candidatePoolSize)
                    .ToList();

                var chosenPieces = new List<PieceCandidate>();
                string bestUnencrypted = null;
                string bestEncrypted = null;
                string bestChessData = null;
                string orderString = null;

                Func<int, bool> dfs = null;
                dfs = startIndex =>
                {
                    List<int> multiAttackedPositions;
                    var multiAttackCount = ComputeAttackCount(chosenPieces, rows, columns, out multiAttackedPositions);

                    if (multiAttackCount > 6) return false;

                    if (multiAttackCount == 6)
                    {
                        var letters = new char[6];
                        var hasHash = false;
                        var k = 0;
                        foreach (var index in multiAttackedPositions)
                        {
                            int posRow = index / columns, posColumn = index % columns;
                            var ch = letterGrid[posRow][posColumn];
                            if (ch == '#')
                            {
                                hasHash = true;
                                break;
                            }
                            letters[k++] = ch;
                        }
                        if (hasHash) return false;

                        string foundWordCandidate;
                        if (!TryFindAnagramWord(letters, out foundWordCandidate)) return false;
                        bestUnencrypted = foundWordCandidate;
                        multiAttackedPositions.Sort();
                        bestEncrypted = new string(multiAttackedPositions.Select(idx => letterGrid[idx / columns][idx % columns]).ToArray());
                        
                        multiAttackedPositions.Sort();
                        
                        var scrambledLetters = multiAttackedPositions
                            .Select(idx => letterGrid[idx / columns][idx % columns])
                            .ToArray();
                        
                        var order = new List<int>();
                        var used = new bool[scrambledLetters.Length];

                        foreach (var target in bestUnencrypted)
                        {
                            for (var i = 0; i < scrambledLetters.Length; i++)
                            {
                                if (used[i] || scrambledLetters[i] != target) continue;
                                order.Add(i + 1);
                                used[i] = true;
                                break;
                            }
                        }
                        orderString = string.Join(" ", order.Select(x => x.ToString()).ToArray());

                        bestChessData = string.Join("", (from pc in chosenPieces
                                                         let file = (char)('A' + pc.Column)
                                                         let rank = (pc.Row + 1).ToString()
                                                         select file + rank).ToArray());
                        return true;
                    }

                    if (chosenPieces.Count >= 8) return false;

                    for (var i = startIndex; i < pool.Count; i++)
                    {
                        chosenPieces.Add(pool[i]);
                        var ok = dfs(i + 1);
                        if (ok) return true;
                        chosenPieces.RemoveAt(chosenPieces.Count - 1);
                    }
                    return false;
                };

                if (!dfs(0)) continue;

                var screenTexts = new List<string>();
                for (var i = 0; i < bestChessData.Length; i += 8)
                    screenTexts.Add(bestChessData.Substring(i, Math.Min(8, bestChessData.Length - i)));
                screenTexts.Add(orderString);
                screenTexts.Add(letterShifts);
                screenTexts.AddRange(keyWords);

                return new CipherResult
                {
                    EncryptedWord = bestEncrypted,
                    UnencryptedWord = bestUnencrypted,
                    ScreenTexts = screenTexts
                };
            }

            return new CipherResult
            {
                EncryptedWord = null,
                UnencryptedWord = null,
                ScreenTexts = new List<string> { "ERROR" }
            };
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Cyan;
        }
    }
}