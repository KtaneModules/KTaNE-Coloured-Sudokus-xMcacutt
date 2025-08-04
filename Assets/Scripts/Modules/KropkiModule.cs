using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace KModkit
{
    public class KropkiModule : Sudoku<KropkiSudokuData>
    {
        public Transform columnCluesParent;
        public Transform rowCluesParent;
        public GameObject clueLightPrefab;

        protected override void GenerateObjects()
        {
            StartCoroutine(GenerateClues());
        }

        private IEnumerator GenerateClues()
        {
            var offset = Vector3.zero;
            for (var row = 0; row < 9; row++) 
            {
                for (var col = 0; col < 8; col++) 
                {
                    var clue = SudokuData.horizontal_clues[row][col];
                    if (clue == 0)
                    {
                        if (col % 3 != 0)
                            offset.x += 0.013f;
                        else
                            offset.x += 0.012f;
                        continue;
                    }
                    var modifiedClue = Enumerable.Range(0, 10).Where(x => x % 2 + 1 != clue).PickRandom();
                    yield return CreateClueLight(clueLightPrefab, columnCluesParent, offset, modifiedClue);
                    if (col % 3 != 0)
                        offset.x += 0.013f;
                    else
                        offset.x += 0.012f;
                }
                offset.x = 0;
                if (row % 3 != 0)
                    offset.z -= 0.013f;
                else
                    offset.z -= 0.012f;
            }
            offset = Vector3.zero;
            for (var col = 0; col < 9; col++)
            {
                for (var row = 0; row < 8; row++)
                {
                    var clue = SudokuData.vertical_clues[col][row];
                    if (clue == 0)
                    {
                        if (row % 3 != 0)
                            offset.z -= 0.013f;
                        else
                            offset.z -= 0.012f;
                        continue;
                    }
                    var modifiedClue = Enumerable.Range(0, 10).Where(x => x % 2 + 1 != clue).PickRandom();
                    yield return CreateClueLight(clueLightPrefab, rowCluesParent, offset, modifiedClue);
                    if (row % 3 != 0)
                        offset.z -= 0.013f;
                    else
                        offset.z -= 0.012f;
                    yield return new WaitForSeconds(0.001f);
                }
                offset.z = 0;
                if (col % 3 != 0)
                    offset.x += 0.013f;
                else
                    offset.x += 0.012f;
            } 
        }
    }
}