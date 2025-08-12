using System.Collections;
using UnityEngine;

namespace KModkit
{
    public class EvenOddModule : Sudoku<EvenOddSudokuData>
    {
        public GameObject circlePrefab;
        
        protected override IEnumerator GenerateSquares()
        {
            var offset = Vector3.zero;
            for (var row = 0; row < 9; row++)
            {
                for (var col = 0; col < 9; col++)
                {
                    var index = row * 9 + col;
                    var square = Instantiate(
                        SudokuData.solution[index] % 2 == 0 ? squarePrefab : circlePrefab, 
                        Vector3.zero, Quaternion.identity);
                    square.transform.SetParent(topLeft, false); 
                    square.transform.localPosition = new Vector3(offset.x, 0, offset.z);
                    var value = SudokuData.grid[index];
                    SquareIndices[index] = value;
                    square.GetComponent<MeshRenderer>().material.color = SquareColours[value];
                    if (colorblindMode.ColorblindModeActive || settings.babyMode)
                        square.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(SquareColours[SelectedPaletteColour], 
                            settings.babyMode && SelectedPaletteColour != 0 ? SelectedPaletteColour.ToString() : null);
                    AddSquare(square, index, value == 0);
                    if (col % 3 == 2)
                        offset.x += 0.014f;
                    else
                        offset.x += 0.012f;
                    yield return new WaitForSeconds(0.001f);
                }

                offset.x = 0;
                if (row % 3 == 2)
                    offset.z -= 0.014f;
                else
                    offset.z -= 0.012f;
            }
        }
    }
}