using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KModkit
{
    public class SkyscraperModule : Sudoku<SkyscraperSudokuData>
    {
        public GameObject clueLightPrefab;
        public Transform topCluesParent;
        public Transform rightCluesParent;
        public Transform bottomCluesParent;
        public Transform leftCluesParent;

        protected override void GenerateObjects() { StartCoroutine(GenerateClues()); }
        
        private IEnumerator GenerateClues()
        {
            var offset = Vector3.zero;

            var i = 0;
            foreach (var clue in SudokuData.clues_t)
            {
                yield return CreateClueLight(clueLightPrefab, topCluesParent, offset, clue);
                if (i % 3 == 2)
                    offset.x += 0.014f;
                else
                    offset.x += 0.012f;
                i++;
            }
            offset = Vector3.zero;
            foreach (var clue in SudokuData.clues_r)
            {
                yield return CreateClueLight(clueLightPrefab, rightCluesParent, offset, clue);
                if (i % 3 == 2)
                    offset.z -= 0.014f;
                else
                    offset.z -= 0.012f;
                i++;
            }
            offset = Vector3.zero;
            foreach (var clue in SudokuData.clues_b)
            {
                yield return CreateClueLight(clueLightPrefab, bottomCluesParent, offset, clue);
                if (i % 3 == 2)
                    offset.x += 0.014f;
                else
                    offset.x += 0.012f;
                i++;
            }
            offset = Vector3.zero;
            foreach (var clue in SudokuData.clues_l)
            {
                yield return CreateClueLight(clueLightPrefab, leftCluesParent, offset, clue);
                if (i % 3 == 2)
                    offset.z -= 0.014f;
                else
                    offset.z -= 0.012f;
                i++;
            }
        }


        protected override IEnumerator GenerateSquares()
        {
            var offset = Vector3.zero;
            for (var row = 0; row < 9; row++)
            {
                for (var col = 0; col < 9; col++)
                {
                    var index = row * 9 + col;
                    var square = Instantiate(squarePrefab, Vector3.zero, Quaternion.identity);
                    square.transform.SetParent(topLeft, false); 
                    var value = SudokuData.grid[index];
                    var height = value == 0 ? 0.001f : value * 0.004f;
                    square.transform.localScale = new Vector3(1f, height * 1000, 1f);
                    square.transform.localPosition = new Vector3(offset.x, height / 4f, offset.z);
                    SquareIndices[index] = value;
                    square.GetComponent<MeshRenderer>().material.color = SquareColours[value];
                    var text = square.GetComponentInChildren<TextMesh>();
                    var pos = text.transform.localPosition;
                    text.transform.localPosition = new Vector3(pos.x, 0.00051f, pos.z);
                    if ((colorblindMode.ColorblindModeActive || settings.babyMode) && value != 0)
                        square.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(SquareColours[value], settings.babyMode ? value.ToString() : null);
                    
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


        protected override void AddSquare(GameObject square, int gridIndex, bool canInteract)
        {
            Squares.Add(square);
        
            var squareSelectable = square.GetComponent<KMSelectable>();
            if (!canInteract)
            {
                squareSelectable.enabled = false;
                return;
            }
            squareSelectable.Parent = moduleSelectable;
            moduleSelectable.Children = moduleSelectable.Children.Concat(new[] { squareSelectable }).ToArray();
            moduleSelectable.UpdateChildrenProperly();

            squareSelectable.OnInteract += () =>
            {
                square.GetComponent<MeshRenderer>().material.color = SquareColours[SelectedPaletteColour];
                if ((colorblindMode.ColorblindModeActive || settings.babyMode) && SelectedPaletteColour != 0)
                    square.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(SquareColours[SelectedPaletteColour], settings.babyMode ? SelectedPaletteColour.ToString() : null);
                SquareIndices[gridIndex] = SelectedPaletteColour;
                var height = SelectedPaletteColour == 0 ? 0.001f : SelectedPaletteColour * 0.004f;
                var scale = square.transform.localScale;
                scale.y = height * 1000f;
                square.transform.localScale = scale;
                var position = square.transform.localPosition;
                position.y = height / 4f;
                square.transform.localPosition = position;
        
                var highlight = square.GetComponentInChildren<KMHighlightable>().gameObject;
                highlight.transform.localScale = SelectedPaletteColour == 0 ? new Vector3(1.1f, 0.1f, 1.1f) : new Vector3(1.2f, height * 20f, 1.2f);
                highlight.transform.localPosition = SelectedPaletteColour == 0 ? new Vector3(0, 0.001f, 0f) : new Vector3(0f, height * 0.005f, 0f);
        
                return false;
            };
        }
    }
}