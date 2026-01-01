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
        
        private static readonly int[][] BishopDirections = new int[][]
        {
            new int[] { 1,  1 }, new int[] { 1, -1 },
            new int[] {-1,  1 }, new int[] {-1, -1 }
        };

        private static readonly int[][] RookDirections = new int[][]
        {
            new int[] { 1,  0 }, new int[] {-1,  0 },
            new int[] { 0,  1 }, new int[] { 0, -1 }
        };

        private static readonly int[,] KnightMoves = new int[,]
        {
            { 1, 2 }, { 1, -2 }, {-1, 2 }, {-1, -2 },
            { 2, 1 }, { 2, -1 }, {-2, 1 }, {-2, -1 }
        };
        
        public AntiKnightCipher(AntiKnightSudokuData sudokuData)
        {
            sudokuGrid = sudokuData.solution.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
            Name = "AntiKnight";
            TwitchPlaysPoints = 10;
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
                    result = ErrorResult;
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
            var letterGrid = GenerateLetterGrid(out keyWords, out letterShifts);
            var debugLogs = new List<string>();
            debugLogs.Add($"Keywords: {string.Join("", keyWords.ToArray())}");
            debugLogs.Add("Letter shifts: " + letterShifts);
            debugLogs.Add("Letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            
            var candidates = new List<PieceCandidate>();
            for (var r = 0; r < 9; r++)
            for (var c = 0; c < 9; c++)
            {
                var type = MapNumberToPiece(sudokuGrid[r][c]);
                if (type == PieceType.None) continue;
                var attacks = EmptyBoardAttackCount(type, r, c, 9, 9);
                candidates.Add(new PieceCandidate(type, r, c, attacks));
            }
            
            var selected = new List<PieceCandidate>();
            var pool = new List<PieceCandidate>();
            var multi = new List<int>();
            var knights = new List<PieceCandidate>();
            var bishops = new List<PieceCandidate>();
            var rooks   = new List<PieceCandidate>();
            var kings   = new List<PieceCandidate>();
            
            for (var attempt = 0; attempt < 200000; attempt++)
            {
                selected.Clear();
                pool.Clear();
                multi.Clear();
                knights.Clear();
                bishops.Clear();
                rooks.Clear();
                kings.Clear();
                var hasRook = false;
                var hasKnight = false;
                
                foreach (var p in candidates)
                {
                    switch (p.Type)
                    {
                        case PieceType.Knight: 
                            knights.Add(p); 
                            break;
                        case PieceType.Bishop: 
                            bishops.Add(p); 
                            break;
                        case PieceType.Rook:   
                            rooks.Add(p); 
                            break;
                        case PieceType.King:   
                            kings.Add(p); 
                            break;
                    }
                }

                pool.AddRange(knights.Take(8));
                pool.AddRange(bishops.Take(8));
                pool.AddRange(rooks.Take(3));
                pool.AddRange(kings.Take(4));

                pool = pool.OrderBy(_ => Random.Next()).ToList();

                foreach (var candidate in pool)
                {
                    if (selected.Count >= 8) break;
                    if (candidate.Type == PieceType.Rook && hasRook) continue;
                    if (candidate.Type == PieceType.Knight) hasKnight = true;
                    if (candidate.Type == PieceType.Rook) hasRook = true;
                    selected.Add(candidate);
                }

                if (!hasKnight && knights.Count > 0)
                {
                    if (selected.Count >= 8) selected.RemoveAt(Random.Next(selected.Count));
                    selected.Add(knights[0]);
                }

                var occupied = new bool[81];
                var attackCount = new int[81];
                foreach (var p in selected)
                    MarkAttacks(p, occupied, attackCount, 9, 9);

                for (var i = 0; i < 81; i++)
                    if (attackCount[i] >= 2 && !occupied[i])
                        multi.Add(i);

                if (multi.Count != 6) continue;

                var letters = multi.Select(i => letterGrid[i/9][i%9]).ToArray();
                if (letters.Any(ch => ch == '#')) continue;

                Array.Sort(letters);
                var sorted = new string(letters);

                var word = data.PickBestWord(6, w =>
                {
                    var charArr = w.ToUpper().ToCharArray();
                    Array.Sort(charArr);
                    return new string(charArr) == sorted ? 1 : 0;
                });
                if (word == null) continue;

                word = word.ToUpper();

                var anagramVerify = word.ToCharArray();
                Array.Sort(anagramVerify);
                if (new string(anagramVerify) != sorted) continue;
                word = word.ToUpper();

                multi.Sort();
                var encrypted = new string(multi.Select(i => letterGrid[i/9][i%9]).ToArray());

                var order = new List<int>();
                var usedLetter = new bool[6];
                var target = word.ToCharArray();
                var source = multi.Select(i => letterGrid[i/9][i%9]).ToArray();

                for (var i = 0; i < 6; i++)
                {
                    var need = target[i];
                    for (var j = 0; j < 6; j++)
                    {
                        if (usedLetter[j]) continue;
                        if (source[j] != need) continue;
                        order.Add(j + 1);
                        usedLetter[j] = true;
                        break;
                    }
                }

                var orderStr = string.Join(" ", order.Select(x => x.ToString()).ToArray());
                var chessData = string.Join("", selected
                    .OrderBy(p => p.Row * 9 + p.Column)
                    .Select(p => (char)('A' + p.Column) + (p.Row + 1).ToString()).ToArray());
                var pieceStrings = string.Join(", ", selected
                    .OrderBy(p => p.Row * 9 + p.Column)
                    .Select(p => $"{(char)('A' + p.Column)}{p.Row + 1}:{p.Type}")
                    .ToArray());
                
                
                debugLogs.Add("Pieces: " + pieceStrings);
                debugLogs.Add("Order: " + orderStr);
                
                var screens = new List<string>();
                for (var i = 0; i < chessData.Length; i += 8)
                    screens.Add(chessData.Substring(i, Math.Min(8, chessData.Length - i)));
                screens.Add(orderStr);
                screens.Add(letterShifts);
                screens.AddRange(keyWords);

                return new CipherResult
                {
                    EncryptedWord = encrypted,
                    UnencryptedWord = word,
                    ScreenTexts = screens,
                    DebugLogs = debugLogs
                };
            }

            return ErrorResult;
        }

        private void MarkAttacks(PieceCandidate p, bool[] occupied, int[] attackCount, int rows, int cols)
        {
            int r = p.Row, c = p.Column;
            var idx = r * cols + c;
            occupied[idx] = true;

            switch (p.Type)
            {
                case PieceType.King:
                {
                    for (var dr = -1; dr <= 1; dr++)
                    for (var dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int nr = r + dr, nc = c + dc;
                        if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && !occupied[nr * cols + nc])
                            attackCount[nr * cols + nc]++;
                    }

                    break;
                }
                case PieceType.Knight:
                {
                    for (int i = 0; i < 8; i++)
                    {
                        int nr = r + KnightMoves[i, 0];
                        int nc = c + KnightMoves[i, 1];
                        if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                        {
                            int targetIdx = nr * cols + nc;
                            // Knights jump → only care if target is empty
                            if (!occupied[targetIdx])
                                attackCount[targetIdx]++;
                        }
                    }

                    break;
                }
                case PieceType.Bishop:
                case PieceType.Rook:
                {
                    var dirs = p.Type == PieceType.Bishop ? BishopDirections : RookDirections;

                    foreach (var d in dirs)
                    {
                        var nr = r + d[0];
                        var nc = c + d[1];

                        while (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                        {
                            var targetIdx = nr * cols + nc;

                            if (occupied[targetIdx])
                                break;

                            attackCount[targetIdx]++;

                            nr += d[0];
                            nc += d[1];
                        }
                    }

                    break;
                }
            }
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Cyan;
        }
    }
}