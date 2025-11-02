using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Utility;
using Words;
using Random = System.Random;

namespace KModkit.Ciphers
{
    public class MazeNode
    {
        public Walls Walls;
        public int Region;
        public string Direction;

        public MazeNode(Walls walls, int region, string direction)
        {
            Walls = walls;
            Region = region;
            Direction = direction;
        }

        public override string ToString() { return Walls + " " + Region + " " + Direction; }
    }
    
    public class KillerCipher : Cipher
    {
        private float hue = 0f;
        private char[][] letterGrid;
        private List<int> cages;
        private Random random = new Random();
        private CellRef currentCell;
        LinkedList<MazeNode> maze = new LinkedList<MazeNode>();
        private MazeNode[] mazeArray;

        public KillerCipher(KillerSudokuData sudokuData)
        {
            cages = sudokuData.cages;
            Name = "Killer";
            IsMaze = true;
        }
        
        public override string Move(int moduleId, string direction)
        {
            if (direction == "S")
                return letterGrid[currentCell.Row][currentCell.Col].ToString();
            switch (direction)
            {
                case "U":
                    if (mazeArray[currentCell.Row * 9 + currentCell.Col].Walls.Up)
                    {
                        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Attempted to move up from {currentCell} into wall.");
                        return "!";
                    }
                    currentCell.Row -= 1;
                    break;
                case "R":
                    if (mazeArray[currentCell.Row * 9 + currentCell.Col].Walls.Right)
                    {
                        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Attempted to move right from {currentCell} into wall.");
                        return "!";
                    }
                    currentCell.Col += 1;
                    break;
                case "D":
                    if (mazeArray[currentCell.Row * 9 + currentCell.Col].Walls.Down)
                    {
                        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Attempted to move down from {currentCell} into wall.");
                        return "!";
                    }
                    currentCell.Row += 1;
                    break;
                case "L":
                    if (mazeArray[currentCell.Row * 9 + currentCell.Col].Walls.Left)
                    {
                        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Attempted to move left from {currentCell} into wall.");
                        return "!";
                    }
                    currentCell.Col -= 1;
                    break;
            }
            return letterGrid[currentCell.Row][currentCell.Col].ToString();
        }

        private void GenerateMaze()
        {
            var order = Enumerable.Range(0, 81)
                .Select((i) =>
                {
                    var row = i / 9;
                    var col = i % 9;
                    var snakeCol = row % 2 == 0 ? col : 8 - col;
                    return row * 9 + snakeCol;
                }).ToArray();
            for (var r = 0; r < 9; r++)
            {
                for (var c = 0; c < 9; c++)
                {
                    var index = order[r * 9 + c];
                    var orderRow = index / 9;
                    var orderCol = index % 9;
                    var walls = new Walls();
                    var cage = cages[index];
                    if (orderRow == 0 || cage != cages[(orderRow - 1) * 9 + orderCol])
                        walls.Up = true;
                    if (orderRow == 8 || cage != cages[(orderRow + 1) * 9 + orderCol])
                        walls.Down = true;
                    if (orderCol == 0 || cage != cages[orderRow * 9 + (orderCol - 1)])
                        walls.Left = true;
                    if (orderCol == 8 || cage != cages[orderRow * 9 + (orderCol + 1)])
                        walls.Right = true;
                    var direction = "R";
                    if (r % 2 == 1)
                        direction = "L";
                    if (c == 8)
                        direction = "D";
                    var node = new MazeNode(walls, cage, direction);
                    if (!maze.Any())
                        maze.AddFirst(node);
                    else 
                        maze.AddAfter(maze.Last, node);
                }
            }

            for (var node = maze.First; node?.Next != null; node = node.Next)
            {
                var current = node.Value;
                switch (current.Direction)
                {
                    case "R":
                    {
                        if (!current.Walls.Right || current.Region == node.Next.Value.Region)
                            continue;
                        current.Walls.Right = false;
                        node.Next.Value.Walls.Left = false;
                        break;
                    }
                    case "L":
                    {
                        if (!current.Walls.Left || current.Region == node.Next.Value.Region)
                            continue;
                        current.Walls.Left = false;
                        node.Next.Value.Walls.Right = false;
                        break;
                    }
                    case "D":
                    {
                        if (!current.Walls.Down || current.Region == node.Next.Value.Region)
                            continue;
                        current.Walls.Down = false;
                        node.Next.Value.Walls.Up = false;
                        break;
                    }
                }

                foreach (var otherNode in maze.Where(n => n.Region == node.Next.Value.Region).ToList())
                    otherNode.Region = current.Region; 
            }

            var snakeArray = maze.ToArray();
            var normalArray = new MazeNode[81];
            for (var r = 0; r < 9; r++)
            {
                for (var c = 0; c < 9; c++)
                {
                    var snakeCol = r % 2 == 0 ? c : 8 - c;
                    var snakeIndex = r * 9 + c;
                    var normalIndex = r * 9 + snakeCol;
                    normalArray[normalIndex] = snakeArray[snakeIndex];
                }
            }
            maze = new LinkedList<MazeNode>(normalArray);
            mazeArray = normalArray;
        }
        
        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            GenerateMaze();
            const string key = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#ABCDEFGHIJKLMNOPQRSTUVWXYZ#ABCDEFGHIJKLMNOPQRSTUVWXYZ#";
            var shuffledKey = key.OrderBy(x => random.Next()).ToArray();
            letterGrid = Enumerable.Range(0, 9)
                .Select(r => shuffledKey.Skip(r * 9).Take(9).ToArray())
                .ToArray();
            List<string> debugLogs = new List<string>();
            debugLogs.Add("Hidden letter grid: " + string.Join("", letterGrid.SelectMany(x => x.Select(n => n.ToString()).ToArray()).ToArray()));
            var word = new Data().PickWord(6);
            currentCell = new CellRef(random.Next(0, 9), random.Next(0, 9));
            
            var selectedCells = word
                .Select(letter =>
                {
                    var candidates = Enumerable.Range(0, 9)
                        .SelectMany(row => Enumerable.Range(0, 9) 
                            .Where(col => letterGrid[row][col] == letter)
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

            var lives = CalculateRequiredLives();
            
            debugLogs.Add($"Calculated Starting Lives: {lives}");
            
            onComplete(new CipherResult
            {
                UnencryptedWord = word,
                ScreenTexts = screenTexts,
                MazeLives = lives,
                DebugLogs = debugLogs
            });

            yield break;
        }
        
        private int CalculateRequiredLives()
        {
            var signatures = mazeArray
                .Select(m =>
                {
                    var w = m.Walls;
                    return (w.Up ? "U" : "") + (w.Right ? "R" : "") + (w.Down ? "D" : "") + (w.Left ? "L" : "");
                })
                .Distinct()
                .Count();
            
            return Math.Max(1, signatures - 1);
        }
        
        private void PrintMaze()
        {
            for (var r = 0; r < 9; r++)
                for (var c = 0; c < 9; c++)
                    Debug.Log((char)('A' + c) + "" + r + " " + mazeArray[r * 9 + c]);
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.White;
        }
    }
}