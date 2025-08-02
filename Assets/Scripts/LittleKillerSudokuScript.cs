using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class LittleKillerSudokuData
{
    public List<int> grid;
    public List<List<int>> little_killer_clues;
}

public class LittleKillerSudokuScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMSelectable moduleSelectable;
    public TextAsset sudokuJson;
    public Transform topLeft;
    public GameObject squarePrefab;
    public GameObject clueLightPrefab;
    public KMSelectable submitButton;
    public KMSelectable resetButton;
    public Material colorMaterialBase;
    public Transform leftCluesParent;
    public Transform rightCluesParent;
    public Transform topCluesParent;
    public Transform bottomCluesParent;
    public Transform paletteParent;
    public GameObject paletteButtonPrefab;
    public KMColorblindMode colorblindMode;

    private List<GameObject> _squares = new List<GameObject>();
    private List<GameObject> _palette = new List<GameObject>();
    private List<GameObject> _clueLights = new List<GameObject>();
    private readonly int[] _squareIndices = new int[81];
    private LittleKillerSudokuData _littleKillerSudoku;
    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _isSolved;
    private int _selectedPaletteColor;

    private static List<Color> _squareColors;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        InitializeMaterials();
        _littleKillerSudoku = JsonConvert.DeserializeObject<List<LittleKillerSudokuData>>(sudokuJson.text)
            .OrderBy(_ => UnityEngine.Random.value).First();
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
        if (_squareIndices.Any(s => s == 0))
            return false;

        var grid = new int[9][];
        for (int index = 0; index < 9; index++)
        {
            grid[index] = new int[9];
        }

        for (var i = 0; i < 81; i++)
            grid[i / 9][i % 9] = _squareIndices[i];

        for (var row = 0; row < 9; row++)
        {
            var nums = new HashSet<int>();
            for (var col = 0; col < 9; col++)
            {
                if (!nums.Add(grid[row][col]))
                    return false;
            }
        }

        for (var col = 0; col < 9; col++)
        {
            var nums = new HashSet<int>();
            for (var row = 0; row < 9; row++)
            {
                if (!nums.Add(grid[row][col]))
                    return false;
            }
        }

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
            foreach (var clueLight in _clueLights)
            {
                Destroy(clueLight);
                yield return new WaitForSeconds(0.001f);
            }

            _clueLights.Clear();

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
            yield return StartCoroutine(GeneratePalette());
            yield return StartCoroutine(GenerateClues());
        }

        _isResetting = false;
    }

    private void InitializeMaterials()
    {
        _squareColors = new List<Color>
        {
            Colors.Red, Colors.Green, Colors.Blue, Colors.Purple,
            Colors.Yellow, Colors.Orange, Colors.Cyan, Colors.Pink, Colors.White
        };
        _squareColors = _squareColors.OrderBy(_ => UnityEngine.Random.value).ToList();
        _squareColors.Insert(0, Colors.Black);
    }

    private IEnumerator CreateClueLight(Transform parent, Vector3 offset, int clue)
    {
        if (clue == 0)
            yield break;
        var clueLightObject = Instantiate(clueLightPrefab, parent.position, Quaternion.identity);
        clueLightObject.transform.SetParent(parent, false);
        clueLightObject.transform.localPosition = new Vector3(offset.x, 0, offset.z);
        var clueLight = clueLightObject.GetComponent<ClueLight>();
        clueLight.colorblindActive = colorblindMode.ColorblindModeActive;
        clueLight.SetColor(colorMaterialBase, _squareColors[clue / 10], _squareColors[clue % 10]);
        _clueLights.Add(clueLightObject);
        yield return new WaitForSeconds(0.001f);
    }

    private IEnumerator GenerateClues()
    {
        var offset = Vector3.zero;

        foreach (var clue in _littleKillerSudoku.little_killer_clues[0])
        {
            yield return CreateClueLight(topCluesParent, offset, clue);
            offset.x += 0.012f;
        }

        offset = Vector3.zero;
        foreach (var clue in _littleKillerSudoku.little_killer_clues[1])
        {
            yield return CreateClueLight(rightCluesParent, offset, clue);
            offset.z -= 0.012f;
        }

        offset = Vector3.zero;
        foreach (var clue in _littleKillerSudoku.little_killer_clues[2])
        {
            yield return CreateClueLight(bottomCluesParent, offset, clue);
            offset.x -= 0.012f;
        }

        offset = Vector3.zero;
        foreach (var clue in _littleKillerSudoku.little_killer_clues[3])
        {
            yield return CreateClueLight(leftCluesParent, offset, clue);
            offset.z += 0.012f;
        }
    }

    private IEnumerator GeneratePalette()
    {
        for (int col = 0; col < 10; col++)
        {
            var paletteSquare = Instantiate(paletteButtonPrefab, paletteParent.position, Quaternion.identity);
            paletteSquare.transform.SetParent(paletteParent, false);
            paletteSquare.transform.localPosition = new Vector3(0, 0, -0.011f * col);
            paletteSquare.GetComponent<MeshRenderer>().material = new Material(colorMaterialBase)
                { color = _squareColors[col % 10] };
            if (colorblindMode.ColorblindModeActive)
                paletteSquare.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(_squareColors[col % 10]);
            AddPaletteButton(paletteSquare, col);
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
                var value = _littleKillerSudoku.grid[index];
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