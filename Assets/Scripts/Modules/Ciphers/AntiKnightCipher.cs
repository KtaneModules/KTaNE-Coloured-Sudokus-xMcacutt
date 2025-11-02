using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KModkit;
using KModkit.Ciphers;
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
            sudokuGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            Name = "AntiKnight";
        }

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
            if (num == 1) return PieceType.King;
            if (num >= 2 && num <= 4) return PieceType.Bishop;
            if (num == 5 || num == 6) return PieceType.Rook;
            if (num >= 7) return PieceType.Knight;
            return PieceType.None;
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
                if (piece.Type == PieceType.King)
                {
                    for (int deltaRow = -1; deltaRow <= 1; deltaRow++)
                    for (int deltaColumn = -1; deltaColumn <= 1; deltaColumn++)
                    {
                        if (deltaRow == 0 && deltaColumn == 0) continue;
                        int targetRow = row + deltaRow, targetColumn = column + deltaColumn;
                        if (targetRow < 0 || targetRow >= rows || targetColumn < 0 || targetColumn >= columns) continue;
                        var targetIndex = targetRow * columns + targetColumn;
                        if (!occupiedPositions[targetIndex]) attackCounts[targetIndex]++;
                    }
                }
                else if (piece.Type == PieceType.Knight)
                {
                    int[,] knightOffsets = { { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 }, { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 } };
                    for (int i = 0; i < 8; i++)
                    {
                        int targetRow = row + knightOffsets[i, 0], targetColumn = column + knightOffsets[i, 1];
                        if (targetRow < 0 || targetRow >= rows || targetColumn < 0 || targetColumn >= columns) continue;
                        var targetIndex = targetRow * columns + targetColumn;
                        if (!occupiedPositions[targetIndex]) attackCounts[targetIndex]++;
                    }
                }
                else if (piece.Type == PieceType.Bishop)
                {
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, 1, 1, rows, columns);
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, 1, -1, rows, columns);
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, -1, 1, rows, columns);
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, -1, -1, rows, columns);
                }
                else if (piece.Type == PieceType.Rook)
                {
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, 1, 0, rows, columns);
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, -1, 0, rows, columns);
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, 0, 1, rows, columns);
                    AddRayIncrements(attackCounts, occupiedPositions, row, column, 0, -1, rows, columns);
                }
            }

            int multiAttackCount = 0;
            for (int i = 0; i < attackCounts.Length; i++)
                if (attackCounts[i] >= 2)
                    multiAttackCount++;
            
            if (multiAttackCount != 6) return multiAttackCount;
            multiAttackedPositions = new List<int>(6);
            for (int i = 0; i < attackCounts.Length; i++)
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
            int count = 0;
            if (type == PieceType.King)
            {
                for (int deltaRow = -1; deltaRow <= 1; deltaRow++)
                for (int deltaColumn = -1; deltaColumn <= 1; deltaColumn++)
                    if (deltaRow != 0 || deltaColumn != 0)
                        if (row + deltaRow >= 0 && row + deltaRow < rows && column + deltaColumn >= 0 && column + deltaColumn < columns)
                            count++;
            }
            else if (type == PieceType.Knight)
            {
                int[,] knightOffsets = { { 2, 1 }, { 2, -1 }, { -2, 1 }, { -2, -1 }, { 1, 2 }, { 1, -2 }, { -1, 2 }, { -1, -2 } };
                for (int i = 0; i < 8; i++)
                    if (row + knightOffsets[i, 0] >= 0 && row + knightOffsets[i, 0] < rows && column + knightOffsets[i, 1] >= 0 && column + knightOffsets[i, 1] < columns)
                        count++;
            }
            else if (type == PieceType.Bishop)
            {
                count += CountRayCells(row, column, 1, 1, rows, columns);
                count += CountRayCells(row, column, 1, -1, rows, columns);
                count += CountRayCells(row, column, -1, 1, rows, columns);
                count += CountRayCells(row, column, -1, -1, rows, columns);
            }
            else if (type == PieceType.Rook)
            {
                count += CountRayCells(row, column, 1, 0, rows, columns);
                count += CountRayCells(row, column, -1, 0, rows, columns);
                count += CountRayCells(row, column, 0, 1, rows, columns);
                count += CountRayCells(row, column, 0, -1, rows, columns);
            }
            return count;
        }

        private int CountRayCells(int row, int column, int deltaRow, int deltaColumn, int rows, int columns)
        {
            int count = 0;
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
            var candidate = data.PickBestWord(6, w => w.OrderBy(c => c).SequenceEqual(letters.OrderBy(c => c)) ? 1 : 0);
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
            List<string> keyWords;
            string letterShifts;
            var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
            List<string> debugLogs = new List<string>();
            debugLogs.Add($"Keywords: {string.Join("", keyWords.ToArray())}");
            debugLogs.Add("Letter shifts: " + letterShifts);
            debugLogs.Add("Letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            var random = new Random();
            const int maxAttempts = 30;

            var rows = letterGrid.Length;
            var columns = letterGrid[0].Length;

            var allCandidates = new List<PieceCandidate>();
            for (int r = 0; r < sudokuGrid.Length; r++)
                for (int c = 0; c < sudokuGrid[0].Length; c++)
                {
                    var type = MapNumberToPiece(sudokuGrid[r][c]);
                    if (type != PieceType.None)
                        allCandidates.Add(new PieceCandidate(type, r, c, EmptyBoardAttackCount(type, r, c, rows, columns)));
                }

            if (allCandidates.Count == 0)
                return new CipherResult
                {
                    EncryptedWord = null,
                    UnencryptedWord = null,
                    ScreenTexts = new List<string> { "Error" }
                };

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidatePoolSize = Math.Min(allCandidates.Count, 15 + attempt);
                var pool = allCandidates
                    .OrderByDescending(p => p.EmptyBoardAttackCount)
                    .ThenBy(p => random.Next())
                    .Take(candidatePoolSize)
                    .ToList();

                var chosenPieces = new List<PieceCandidate>();
                List<int> multiAttackedPositions = new List<int>();
                string bestUnencrypted = null;
                string bestEncrypted = null;
                string bestChessData = null;
                string orderString = null;

                Func<int, bool> dfs = null;
                dfs = startIndex =>
                {
                    if (chosenPieces.Count > 8) return false;

                    
                    var multiAttackCount = ComputeAttackCount(chosenPieces, rows, columns, out multiAttackedPositions);
                    if (multiAttackCount > 6) return false;

                    if (multiAttackCount == 6)
                    {
                        var letters = new char[6];
                        int k = 0;
                        foreach (var index in multiAttackedPositions)
                        {
                            int posRow = index / columns, posColumn = index % columns;
                            var ch = letterGrid[posRow][posColumn];
                            if (ch == '#') return false;
                            letters[k++] = ch;
                        }

                        string foundWordCandidate;
                        if (!TryFindAnagramWord(letters, out foundWordCandidate)) return false;
                        bestUnencrypted = foundWordCandidate;
                        multiAttackedPositions.Sort();
                        bestEncrypted = new string(multiAttackedPositions.Select(idx => letterGrid[idx / columns][idx % columns]).ToArray());
                        
                        var scrambledLetters = multiAttackedPositions
                            .Select(idx => letterGrid[idx / columns][idx % columns])
                            .ToArray();
                        
                        var order = new List<int>();
                        var used = new bool[scrambledLetters.Length];
                        foreach (var target in bestUnencrypted)
                        {
                            for (int i = 0; i < scrambledLetters.Length; i++)
                            {
                                if (used[i] || scrambledLetters[i] != target) continue;
                                order.Add(i + 1);
                                used[i] = true;
                                break;
                            }
                        }
                        orderString = string.Join(" ", order.Select(x => x.ToString()).ToArray());

                        bestChessData = string.Join("", chosenPieces
                            .Select(pc => (char)('A' + pc.Column) + (pc.Row + 1).ToString()).ToArray());
                        return true;
                    }

                    int knightCount = chosenPieces.Count(p => p.Type == PieceType.Knight);
                    int rookCount = chosenPieces.Count(p => p.Type == PieceType.Rook);
                    if (knightCount == 0 && chosenPieces.Count >= 2) return false;
                    if (rookCount > 1) return false;

                    for (int i = startIndex; i < pool.Count; i++)
                    {
                        if (rookCount + (pool[i].Type == PieceType.Rook ? 1 : 0) > 1) continue;
                        chosenPieces.Add(pool[i]);
                        if (dfs(i + 1)) return true;
                        chosenPieces.RemoveAt(chosenPieces.Count - 1);
                    }
                    return false;
                };

                if (dfs(0))
                {
                    var screenTexts = new List<string>();
                    for (int i = 0; i < bestChessData.Length; i += 8)
                        screenTexts.Add(bestChessData.Substring(i, Math.Min(8, bestChessData.Length - i)));
                    screenTexts.Add(orderString);
                    screenTexts.Add(letterShifts);
                    screenTexts.AddRange(keyWords);
                    debugLogs.AddRange(chosenPieces.Select(piece => $"{piece.Type} placed at {(char)('A' + piece.Column)}, {piece.Row + 1}"));
                    debugLogs.AddRange(multiAttackedPositions.Select(pos => $"{(char)('A' + pos % 9)}, {pos / 9 + 1} is attacked by more than one piece."));
                    return new CipherResult
                    {
                        EncryptedWord = bestEncrypted,
                        UnencryptedWord = bestUnencrypted,
                        ScreenTexts = screenTexts,
                        DebugLogs = debugLogs
                    };
                }
            }

            return new CipherResult
            {
                EncryptedWord = null,
                UnencryptedWord = null,
                ScreenTexts = new List<string> { "Error" },
                DebugLogs = new List<string> { "Error, generation failed." }
            };
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Cyan;
        }
    }
}