using System.Collections;
using UnityEngine;

namespace KModkit
{
    public class LittleKillerModule : Sudoku<LittleKillerSudokuData>
    {
        public GameObject clueLightPrefab;
        public Transform leftCluesParent;
        public Transform rightCluesParent;
        public Transform topCluesParent;
        public Transform bottomCluesParent;
        
        protected override void GenerateObjects()
        {
            StartCoroutine(GenerateClues());
        }
        
        private IEnumerator GenerateClues()
        {
            var offset = Vector3.zero;

            var i = 0;
            
            foreach (var clue in SudokuData.little_killer_clues[0])
            {
                
                if (clue == 0)
                {
                    if (i % 3 == 2)
                        offset.x += 0.014f;
                    else
                        offset.x += 0.012f;
                    i++;
                    continue;
                }
                yield return CreateClueLight(clueLightPrefab, topCluesParent, offset, clue, true);
                if (i % 3 == 2)
                    offset.x += 0.014f;
                else
                    offset.x += 0.012f;
                i++; 
            }

            i = 0;
            offset = Vector3.zero;
            foreach (var clue in SudokuData.little_killer_clues[1])
            {
                if (clue == 0)
                {
                    if (i % 3 == 2)
                        offset.z -= 0.014f;
                    else
                        offset.z -= 0.012f;
                    i++;
                    continue;
                }
                yield return CreateClueLight(clueLightPrefab, rightCluesParent, offset, clue, true);
                if (i % 3 == 2)
                    offset.z -= 0.014f;
                else
                    offset.z -= 0.012f;
                i++;
            }

            i = 0;
            offset = Vector3.zero;
            foreach (var clue in SudokuData.little_killer_clues[2])
            {
                if (clue == 0)
                {
                    if (i % 3 == 2)
                        offset.x -= 0.014f;
                    else
                        offset.x -= 0.012f;
                    i++;
                    continue;
                }
                yield return CreateClueLight(clueLightPrefab, bottomCluesParent, offset, clue, true);
                if (i % 3 == 2)
                    offset.x -= 0.014f;
                else
                    offset.x -= 0.012f;
                i++;
            }

            i = 0;
            offset = Vector3.zero;
            foreach (var clue in SudokuData.little_killer_clues[3])
            {
                if (clue == 0)
                {
                    if (i % 3 == 2)
                        offset.z += 0.014f;
                    else
                        offset.z += 0.012f;
                    i++;
                    continue;
                }
                yield return CreateClueLight(clueLightPrefab, leftCluesParent, offset, clue, true);
                if (i % 3 == 2)
                    offset.z += 0.014f;
                else
                    offset.z += 0.012f;
                i++;
            }
        }
    }
}