using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KModkit
{
    public class KillerModule : Sudoku<KillerSudokuData>
    {
        public GameObject clueLightPrefab;
        public GameObject horizontalLinePrefab;
        public Transform linesParent;
        public Transform cluesParent;

        protected override void GenerateObjects()
        {
            StartCoroutine(GenerateLines());
            StartCoroutine(GenerateClues());
        }
        
        private IEnumerator GenerateClues()
        {
            for (var cageIndex = 0; cageIndex < SudokuData.cage_sums.Count; cageIndex++)
            {
                var cageCells = new List<int>();
                for (var i = 0; i < SudokuData.cages.Count; i++)
                    if (SudokuData.cages[i] == cageIndex)
                        cageCells.Add(i);
                var topLeftCell = cageCells.OrderBy(i => i / 9).ThenBy(i => i % 9).First();
                var cageSum = SudokuData.cage_sums[cageIndex];
                var xOffset = (topLeftCell % 9) * 0.012f;
                if (topLeftCell % 9 > 2)
                    xOffset += 0.002f;
                if (topLeftCell % 9 > 5)
                    xOffset += 0.002f;
                var zOffset = -0.012f * (topLeftCell / 9);
                if (topLeftCell / 9 > 2)
                    zOffset -= 0.002f;
                if (topLeftCell / 9 > 5)
                    zOffset -= 0.002f;
                yield return CreateClueLight(clueLightPrefab, cluesParent,
                    new Vector3(xOffset, 0, zOffset), cageSum, true);
                yield return new WaitForSeconds(0.001f);
            }
        }
        
        private IEnumerator GenerateLines()
        {
            float zOffset = 0;
            float xOffset = 0; 
            
            for (var row = 0; row < 9; row++)
            {
                for (var i = 0; i < 8; i++)
                {
                    var cell1 = SudokuData.cages[row * 9 + i];
                    var cell2 = SudokuData.cages[row * 9 + i + 1];
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
                    var cell1 = SudokuData.cages[i * 9 + col];
                    var cell2 = SudokuData.cages[(i + 1) * 9 + col];
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