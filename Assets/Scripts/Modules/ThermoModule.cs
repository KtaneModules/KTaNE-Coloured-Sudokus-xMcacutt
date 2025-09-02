using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KModkit
{
    public class ThermoModule : Sudoku<ThermoSudokuData>
    {
        public GameObject bulbPrefab;
        public GameObject linePrefab;
        public Transform thermosParent;
        private static List<Color> _thermoColors = new List<Color> { Colors.ThermoRed, Colors.ThermoBlue };
        
        protected override void GenerateObjectsEarly() { GenerateThermos(); }

        private void GenerateThermos()
        {
            if (!EarlyObjects.ContainsKey("Thermos"))
                EarlyObjects.Add("Thermos", new List<GameObject>());
            
            foreach (var thermo in SudokuData.thermos)
            {
                var color = _thermoColors[UnityEngine.Random.Range(0, _thermoColors.Count)];
                var thermoIndices = color == Colors.ThermoRed ? thermo.AsEnumerable().Reverse().ToList() : thermo;
                var bulbIdx = thermoIndices[0];
                var bulbRow = bulbIdx / 9;
                var bulbCol = bulbIdx % 9;
                float colOffset = 0, rowOffset = 0;
                if (bulbCol > 2)
                    colOffset += 0.002f;
                if (bulbCol > 5)
                    colOffset += 0.002f;
                if (bulbRow > 2)
                    rowOffset += 0.002f;
                if (bulbRow > 5)
                    rowOffset += 0.002f;
                var bulbPos = new Vector3(bulbCol * 0.012f + colOffset, 0, -bulbRow * 0.012f - rowOffset);
                var bulb = Instantiate(bulbPrefab, Vector3.zero, Quaternion.identity);
                bulb.transform.SetParent(thermosParent, false);
                bulb.transform.localPosition = bulbPos;
                if (colorblindMode.ColorblindModeActive)
                    bulb.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(color);
                bulb.GetComponentInChildren<MeshRenderer>().material =
                    new Material(colorMaterialBase) { color = color };
                EarlyObjects["Thermos"].Add(bulb);

                for (var i = 0; i < thermoIndices.Count - 1; i++)
                {
                    var idx1 = thermo[i];
                    var idx2 = thermo[i + 1]; 
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
                    quadObj.transform.SetParent(thermosParent, false);
                    var midPoint = (startPos + endPos) / 2f;
                    quadObj.transform.localPosition = midPoint;

                    quadObj.transform.localRotation = Quaternion.LookRotation(endPos - startPos, thermosParent.up);
                    float distance = Vector3.Distance(startPos, endPos);
                    quadObj.transform.localScale = new Vector3(0.0014f, 0.0014f, distance + 0.001f);

                    var renderer = quadObj.GetComponent<MeshRenderer>();
                    renderer.material = new Material(colorMaterialBase) { color = color };
                    renderer.material.color = color;

                    if (quadObj != null)
                        EarlyObjects["Thermos"].Add(quadObj);
                }
            }
        }
    }
}