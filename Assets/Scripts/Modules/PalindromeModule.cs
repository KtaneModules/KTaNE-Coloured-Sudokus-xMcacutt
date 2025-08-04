using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KModkit
{
    public class PalindromeModule : Sudoku<PalindromeSudokuData>
    {
        public GameObject linePrefab;
        public Transform linesParent;

        protected override void GenerateObjectsEarly() { GenerateLines(); }

        private void GenerateLines()
        {
            if (!EarlyObjects.ContainsKey("Palindrome Lines"))
                EarlyObjects.Add("Palindrome Lines", new List<GameObject>());

            foreach (var line in SudokuData.lines)
            {
                for (int i = 0; i < line.Count - 1; i++)
                {
                    var idx1 = line[i];
                    var idx2 = line[i + 1]; 
                    var row1 = idx1 / 9;
                    var col1 = idx1 % 9;
                    var row2 = idx2 / 9;
                    var col2 = idx2 % 9;
                    var colOffset1 = col1 > 2 ? 0.002f : 0f;
                    if (col1 > 5) colOffset1 += 0.002f;
                    var rowOffset1 = row1 > 2 ? 0.002f : 0f;
                    if (row1 > 5) rowOffset1 += 0.002f;
                    var colOffset2 = col2 > 2 ? 0.002f : 0f;
                    if (col2 > 5) colOffset2 += 0.002f;
                    var rowOffset2 = row2 > 2 ? 0.002f : 0f;
                    if (row2 > 5) rowOffset2 += 0.002f;
                    var startPos = new Vector3(col1 * 0.012f + colOffset1, 0, -row1 * 0.012f - rowOffset1);
                    var endPos = new Vector3(col2 * 0.012f + colOffset2, 0, -row2 * 0.012f - rowOffset2);

                    var quadObj = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
                    quadObj.transform.SetParent(linesParent, false);
                    var midPoint = (startPos + endPos) / 2f;
                    quadObj.transform.localPosition = midPoint;

                    quadObj.transform.localRotation = Quaternion.LookRotation(endPos - startPos, linesParent.up);
                    float distance = Vector3.Distance(startPos, endPos);
                    quadObj.transform.localScale = new Vector3(0.0014f, 0.0014f, distance + 0.001f);

                    var renderer = quadObj.GetComponent<MeshRenderer>();
                    renderer.material = new Material(colorMaterialBase) { color = Colors.PalindromeLine };
                    renderer.material.color = Colors.PalindromeLine;

                    if (quadObj != null)
                        EarlyObjects["Palindrome Lines"].Add(quadObj);
                }
            }
        }
    }
}