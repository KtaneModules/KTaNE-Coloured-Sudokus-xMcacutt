using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KModkit
{
    public class SandwichModule : Sudoku<SandwichSudokuData>
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
            for (var col = 0; col < 9; col++)
            {
                var colClue = SudokuData.col_sums[col];
                yield return CreateClueLight(clueLightPrefab, columnCluesParent, offset, colClue, true);
                yield return new WaitForSeconds(0.001f);
                if (col % 3 == 2)
                    offset.x += 0.014f;
                else
                    offset.x += 0.012f;
            }
            offset = Vector3.zero;
            for (var row = 0; row < 9; row++)
            {
                var rowClue = SudokuData.row_sums[row];
                yield return CreateClueLight(clueLightPrefab, rowCluesParent, offset, rowClue, true);
                yield return new WaitForSeconds(0.001f);
                if (row % 3 == 2)
                    offset.z -= 0.014f;
                else
                    offset.z -= 0.012f;
            }
        }
        
    }
}