using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KModkit
{
    public class JigsawModule : Sudoku<JigsawSudokuData>
    {
        public GameObject horizontalLinePrefab; 
        public Transform linesParent;

        protected override void GenerateObjects()
        {
            StartCoroutine(GenerateLines());
        }

        protected override bool IsValid()
        {
            if (SquareIndices.Any(s => s == 0))
                return false;
            
            if (SquareIndices.Any(s => s == 0))
            {
                $"Strike! There was an empty square in the input.".Log(this);
                return false;
            }

            var grid = new int[9][];
            for (var index = 0; index < 9; index++)
                grid[index] = new int[9];

            for (var i = 0; i < 81; i++)
                grid[i / 9][i % 9] = SquareIndices[i];
            
            for (var row = 0; row < 9; row++)
            {
                var nums = new HashSet<int>();
                for (var col = 0; col < 9; col++)
                {
                    if (nums.Add(grid[row][col])) 
                        continue;
                    $"Strike! Row {row + 1} has a duplicate colour.".Log(this);
                    return false;
                }
            }
            
            for (var col = 0; col < 9; col++)
            {
                var nums = new HashSet<int>();
                for (var row = 0; row < 9; row++)
                {
                    if (nums.Add(grid[row][col]))
                        continue;
                    $"Strike! Column {col + 1} has a duplicate colour.".Log(this);
                    return false;
                }
            }
            
            for (var boxIndex = 0; boxIndex < 9; boxIndex++)
            {
                var currentBoxIndex = boxIndex;
                var cellIndicesInBox = Enumerable.Range(0, 81)
                    .Where(i => SudokuData.boxes[i] == currentBoxIndex);
                var values = new HashSet<int>();
                if (!cellIndicesInBox.Select(index => SquareIndices[index]).Any(v => !values.Add(v))) 
                    continue;
                var duplicateIndex = cellIndicesInBox.First(index => !values.Add(SquareIndices[index]));
                var row = duplicateIndex / 9;
                var col = duplicateIndex % 9;
                $"Strike! There is a duplicate colour in the box at row {row} column {col}.".Log(this);
                return false;
            }
            
            for (var i = 0; i < 81; i++)
            {
                if (SquareIndices[i] == SudokuData.solution[i]) continue;
                var mismatchedIndices = Enumerable.Range(0, 81)
                    .Where(j => SquareIndices[j] != SudokuData.solution[j])
                    .ToList();
                $"Strike! The following square indices do not match the solution: {mismatchedIndices.Join(", ")}".Log(this);
                return false;
            }
            return true;
        }
        
        
        private IEnumerator GenerateLines()
        {
            float zOffset = 0;
            float xOffset = 0; 
            
            for (var row = 0; row < 9; row++)
            {
                for (var i = 0; i < 8; i++)
                {
                    var cell1 = SudokuData.boxes[row * 9 + i];
                    var cell2 = SudokuData.boxes[row * 9 + i + 1];
                    if (cell1 != cell2)
                    {
                        if (row != 0 && row % 3 == 0)
                            yield return CreateLine(horizontalLinePrefab, linesParent,
                                new Vector3(0.006f + xOffset, 0, zOffset + 0.0005f), true, true);
                        else if (row != 8 && row % 3 == 2)
                            yield return CreateLine(horizontalLinePrefab, linesParent,
                                new Vector3(0.006f + xOffset, 0, zOffset - 0.0005f), true, true);
                        else
                            yield return CreateLine(horizontalLinePrefab, linesParent, 
                                new Vector3(0.006f + xOffset, 0, zOffset), true, false);
                    }
                    xOffset += 0.012f;
                    if (i % 3 != 0 && i != 7)
                        xOffset += 0.001f;
                }
                xOffset = 0;
                zOffset -= 0.012f;
                if (row % 3 == 2)
                    zOffset -= 0.002f;
            }

            zOffset = 0;
            for (var col = 0; col < 9; col++)
            {
                for (var i = 0; i < 8; i++)
                {
                    var cell1 = SudokuData.boxes[i * 9 + col];
                    var cell2 = SudokuData.boxes[(i + 1) * 9 + col];
                    if (cell1 != cell2)
                    {
                        if (col != 0 && col % 3 == 0)
                            yield return CreateLine(horizontalLinePrefab, linesParent,
                                new Vector3(xOffset - 0.0005f, 0, -0.006f - zOffset), false, true);
                        else if (col != 8 && col % 3 == 2)
                            yield return CreateLine(horizontalLinePrefab, linesParent,
                                new Vector3(xOffset + 0.0005f, 0, -0.006f - zOffset), false, true);
                        else
                            yield return CreateLine(horizontalLinePrefab, linesParent, 
                                new Vector3(xOffset, 0, -0.006f - zOffset), false, false);
                    }
                    zOffset += 0.012f;
                    if (i % 3 != 0 && i != 7)
                        zOffset += 0.001f;
                }
                zOffset = 0;
                xOffset += 0.012f;
                if (col % 3 == 2)
                    xOffset += 0.002f;
            }
            yield return new WaitForSeconds(0.001f);
        }
    }
}