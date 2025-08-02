using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

[Serializable]
public class PalindromeSudokuData
{
    public List<int> grid;
    public List<List<int>> lines;
}

public class PalindromeSudokuScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMSelectable moduleSelectable;
    public TextAsset sudokuJson;
    public Transform topLeft;
    public GameObject squarePrefab;
    public KMSelectable submitButton;
    public KMSelectable resetButton;
    public Material colorMaterialBase;
    public Transform paletteParent;
    public GameObject paletteButtonPrefab;
    public GameObject horizontalTubePrefab; 
    public GameObject diagonalTubePrefab; 
    public KMColorblindMode colorblindMode;

    private List<GameObject> _squares = new List<GameObject>();
    private List<GameObject> _palette = new List<GameObject>();
    private List<GameObject> _lineObjects = new List<GameObject>();
    private readonly int[] _squareIndices = new int[81];
    private PalindromeSudokuData _palindromeSudoku;
    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _isSolved;
    private int _selectedPaletteColor;
    public Transform linesParent;

    private static List<Color> _squareColors;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;

        InitializeMaterials();
        _palindromeSudoku = JsonConvert.DeserializeObject<List<PalindromeSudokuData>>(sudokuJson.text).OrderBy(_ => UnityEngine.Random.value).First();
        StartCoroutine(ResetSudoku(true));
        submitButton.OnInteract += () =>
        {
            submitButton.AddInteractionPunch(1f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, submitButton.transform);
            if (_isResetting || _isSolved)
                return false;
            if (IsValid())
            {
                Module.HandlePass();
                _isSolved = true;
            }
            else
                Module.HandleStrike();
            return false;
        };
        resetButton.OnInteract += () =>
        {
            resetButton.AddInteractionPunch(1f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, resetButton.transform);
            if (_isResetting || _isSolved)
                return false;
            StartCoroutine(ResetSudoku());
            return false;
        };
    }

    private bool IsValid()
    {
        // Check for empty cells
        if (_squareIndices.Any(s => s == 0))
            return false;

        var grid = new int[9][];
        for (var index = 0; index < 9; index++)
            grid[index] = new int[9];

        for (var i = 0; i < 81; i++)
            grid[i / 9][i % 9] = _squareIndices[i];

        // Check rows
        for (var row = 0; row < 9; row++)
        {
            var nums = new HashSet<int>();
            for (var col = 0; col < 9; col++)
            {
                if (!nums.Add(grid[row][col]))
                    return false;
            }
        }

        // Check columns
        for (var col = 0; col < 9; col++)
        {
            var nums = new HashSet<int>();
            for (var row = 0; row < 9; row++)
            {
                if (!nums.Add(grid[row][col]))
                    return false;
            }
        }

        // Check 3x3 boxes
        for (var boxRow = 0; boxRow < 3; boxRow++)
        {
            for (var boxCol = 0; boxCol < 3; boxCol++)
            {
                var nums = new HashSet<int>();
                for (var i = 0; i < 3; i++)
                {
                    for (var j = 0; j < 3; j++)
                    {
                        var val = grid[boxRow * 3 + i][boxCol * 3 + j];
                        if (!nums.Add(val))
                            return false;
                    }
                }
            }
        }


        foreach (var line in _palindromeSudoku.lines)
        {
            for (var i = 0; i < line.Count / 2; i++)
            {
                var leftIndex = line[i];
                var rightIndex = line[line.Count - 1 - i];
                if (_squareIndices[leftIndex] != _squareIndices[rightIndex])
                    return false;
            }
        }
        return true;
    }

    private bool _isResetting = false;
    private IEnumerator ResetSudoku(bool fullReset = false)
    {
        _isResetting = true;

        foreach (var square in _squares)
        {
            Destroy(square);
            yield return new WaitForSeconds(0.001f);
        }
        _squares.Clear();


        if (fullReset)
        {
            foreach (var line in _lineObjects)
            {
                Destroy(line);
                yield return new WaitForSeconds(0.001f);
            }
            _lineObjects.Clear();
            
            foreach (var palette in _palette)
            {
                Destroy(palette);
                yield return new WaitForSeconds(0.001f);
            }
            _palette.Clear();
        }

        yield return StartCoroutine(GenerateSquares());
        if (fullReset)
        {
            yield return StartCoroutine(GenerateLines());
            yield return StartCoroutine(GeneratePalette());
        }

        _isResetting = false;
    }

    private void InitializeMaterials()
    {
        _squareColors = new List<Color> { Colors.Red, Colors.Green, Colors.Blue, Colors.Purple,
            Colors.Yellow, Colors.Orange, Colors.Cyan, Colors.Pink, Colors.White };
        _squareColors = _squareColors.OrderBy(_ => UnityEngine.Random.value).ToList();
        _squareColors.Insert(0, Colors.Black);
    }

    private IEnumerator GeneratePalette()
    {
        for (var col = 0; col < 10; col++)
        {
            var paletteSquare = Instantiate(paletteButtonPrefab, paletteParent.position, Quaternion.identity);
            paletteSquare.transform.SetParent(paletteParent, false);
            paletteSquare.transform.localPosition = new Vector3(0, 0, -0.011f * col);
            paletteSquare.GetComponent<MeshRenderer>().material = new Material(colorMaterialBase) { color = _squareColors[col % 10] };
            if (colorblindMode.ColorblindModeActive)
                paletteSquare.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(_squareColors[col % 10]);
            AddPaletteButton(paletteSquare, col);
            _palette.Add(paletteSquare);
            yield return new WaitForSeconds(0.001f);
        }
    }

    private IEnumerator GenerateLines()
    {
        foreach (var line in _palindromeSudoku.lines)
        {
            for (var i = 0; i < line.Count - 1; i++)
            {
                var idx1 = line[i];
                var idx2 = line[i + 1];
                var row1 = idx1 / 9;
                var col1 = idx1 % 9;
                var row2 = idx2 / 9;
                var col2 = idx2 % 9;
                var pos1 = new Vector3(col1 * 0.012f, 0, -row1 * 0.012f);
                var pos2 = new Vector3(col2 * 0.012f, 0, -row2 * 0.012f);
                var midpoint = (pos1 + pos2) / 2;
                var dr = row2 - row1;
                var dc = col2 - col1;
                GameObject tubePrefab;
                Quaternion rotation;

                if (Math.Abs(dr) == Math.Abs(dc))
                {
                    tubePrefab = diagonalTubePrefab;
                    if (dr == 1 && dc == 1) // Top-left to bottom-right
                        rotation = Quaternion.Euler(0, 0, 0);
                    else if (dr == -1 && dc == -1) // Bottom-right to top-left
                        rotation = Quaternion.Euler(0, 180, 0);
                    else if (dr == -1 && dc == 1) // Bottom-left to top-right
                        rotation = Quaternion.Euler(0, 90, 0);
                    else // Top-right to bottom-left
                        rotation = Quaternion.Euler(0, -90, 0);
                }
                else
                {
                    tubePrefab = horizontalTubePrefab;
                    if (dc == 1) // Right
                        rotation = Quaternion.Euler(0, 0, 0);
                    else if (dc == -1) // Left
                        rotation = Quaternion.Euler(0, 180, 0);
                    else if (dr == 1) // Down
                        rotation = Quaternion.Euler(0, 90, 0);
                    else // Up
                        rotation = Quaternion.Euler(0, -90, 0);
                }

                var tube = Instantiate(tubePrefab, Vector3.zero, rotation);
                tube.transform.SetParent(linesParent, false);
                tube.transform.localPosition = midpoint;
                tube.GetComponentInChildren<MeshRenderer>().material = new Material(colorMaterialBase) { color = Colors.LineGray };
                _lineObjects.Add(tube);
            }

            yield return new WaitForSeconds(0.001f);
        }
    }

    private IEnumerator GenerateSquares()
    {
        var offset = Vector3.zero;
        for (var row = 0; row < 9; row++)
        {
            for (var col = 0; col < 9; col++)
            {
                var index = row * 9 + col;
                var square = Instantiate(squarePrefab, Vector3.zero, Quaternion.identity);
                square.transform.SetParent(topLeft, false);
                square.transform.localPosition = new Vector3(offset.x, 0, offset.z);
                var value = _palindromeSudoku.grid[index];
                _squareIndices[index] = value;
                square.GetComponent<MeshRenderer>().material.color = _squareColors[value];
                if (colorblindMode.ColorblindModeActive)
                    square.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(_squareColors[value]);
                AddSquare(square, index, value == 0);
                offset.x += 0.012f;
                yield return new WaitForSeconds(0.001f);
            }
            offset.x = 0;
            offset.z -= 0.012f;
        }
    }

    private void AddPaletteButton(GameObject button, int index)
    {
        _palette.Add(button);
        var paletteSelectable = button.GetComponent<KMSelectable>();
        paletteSelectable.Parent = moduleSelectable;
        moduleSelectable.Children = moduleSelectable.Children.Concat(new[] { paletteSelectable }).ToArray();
        moduleSelectable.UpdateChildrenProperly();

        paletteSelectable.OnInteract += () =>
        {
            _selectedPaletteColor = index;
            return false;
        };
    }

    private void AddSquare(GameObject square, int gridIndex, bool canInteract)
    {
        _squares.Add(square);

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
            square.GetComponent<MeshRenderer>().material.color = _squareColors[_selectedPaletteColor];
            if (colorblindMode.ColorblindModeActive)
                square.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(_squareColors[_selectedPaletteColor]);
            _squareIndices[gridIndex] = _selectedPaletteColor;
            return false;
        };
    }
}