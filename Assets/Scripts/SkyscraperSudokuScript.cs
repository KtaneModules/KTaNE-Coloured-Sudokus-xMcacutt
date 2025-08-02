using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class SkyscraperSudokuData
{
    public List<int> grid;
    public List<int> clues_l;
    public List<int> clues_r;
    public List<int> clues_t;
    public List<int> clues_b;
}

public class SkyscraperSudokuScript : MonoBehaviour 
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
    private SkyscraperSudokuData _skyscraperSudoku;
    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _isSolved;
    private int _selectedPaletteColor;

    private static List<Color> _squareColors;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        InitializeMaterials();
        _skyscraperSudoku = JsonConvert.DeserializeObject<List<SkyscraperSudokuData>>(sudokuJson.text).OrderBy(_ => UnityEngine.Random.value).First();
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

        for (var col = 0; col < 9; col++)
        {
            if (_skyscraperSudoku.clues_t[col] == 0) continue;
            var visible = 1;
            var maxHeight = grid[0][col];
            for (var row = 1; row < 9; row++)
            {
                if (grid[row][col] <= maxHeight) continue;
                visible++;
                maxHeight = grid[row][col];
            }
            if (_skyscraperSudoku.clues_t[col] != visible)
                return false;
        }

        for (var col = 0; col < 9; col++)
        {
            if (_skyscraperSudoku.clues_b[col] == 0) continue;
            var visible = 1;
            var maxHeight = grid[8][col];
            for (var row = 7; row >= 0; row--)
            {
                if (grid[row][col] <= maxHeight) continue;
                visible++;
                maxHeight = grid[row][col];
            }
            if (_skyscraperSudoku.clues_b[col] != visible)
                return false;
        }
        
        for (var row = 0; row < 9; row++)
        {
            if (_skyscraperSudoku.clues_l[row] == 0) continue;
            var visible = 1;
            var maxHeight = grid[row][0];
            for (var col = 1; col < 9; col++)
            {
                if (grid[row][col] <= maxHeight) continue;
                visible++;
                maxHeight = grid[row][col];
            }
            if (_skyscraperSudoku.clues_l[row] != visible)
                return false;
        }

        for (var row = 0; row < 9; row++)
        {
            if (_skyscraperSudoku.clues_r[row] == 0) continue;
            var visible = 1;
            var maxHeight = grid[row][8];
            for (var col = 7; col >= 0; col--)
            {
                if (grid[row][col] <= maxHeight) continue;
                visible++;
                maxHeight = grid[row][col];
            }
            if (_skyscraperSudoku.clues_r[row] != visible)
                return false;
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
        _squareColors = new List<Color> { Colors.Red, Colors.Green, Colors.Blue, Colors.Purple, 
            Colors.Yellow, Colors.Orange, Colors.Cyan, Colors.Pink, Colors.White };
        _squareColors = _squareColors.OrderBy(_ => UnityEngine.Random.value).ToList();
        _squareColors.Insert(0, Colors.Black);
    }

    private IEnumerator CreateClueLight(Transform parent, Vector3 offset, int clue)
    {
        var clueLightObject = Instantiate(clueLightPrefab, parent.position, Quaternion.identity);
        clueLightObject.transform.SetParent(parent, false);
        clueLightObject.transform.localPosition = new Vector3(offset.x, 0, offset.z);

        var clueLight = clueLightObject.GetComponent<ClueLight>();
        var color = _squareColors.Where(i => _squareColors.IndexOf(i) % 2 != clue - 1).PickRandom();
        clueLight.colorblindActive = colorblindMode.ColorblindModeActive;
        clueLight.SetColor(colorMaterialBase, color);

        _clueLights.Add(clueLightObject);

        yield return new WaitForSeconds(0.001f);
    }

    private IEnumerator GenerateClues()
    {
        var offset = Vector3.zero;

        foreach (var clue in _skyscraperSudoku.clues_t)
        {
            yield return CreateClueLight(topCluesParent, offset, clue);
            offset.x += 0.012f;
        }
        offset = Vector3.zero;
        foreach (var clue in _skyscraperSudoku.clues_r)
        {
            yield return CreateClueLight(rightCluesParent, offset, clue);
            offset.z -= 0.012f;
        }
        offset = Vector3.zero;
        foreach (var clue in _skyscraperSudoku.clues_b)
        {
            yield return CreateClueLight(bottomCluesParent, offset, clue);
            offset.x += 0.012f;
        }
        offset = Vector3.zero;
        foreach (var clue in _skyscraperSudoku.clues_l)
        {
            yield return CreateClueLight(leftCluesParent, offset, clue);
            offset.z -= 0.012f;
        }
    }

    private IEnumerator GeneratePalette()
    {
        for (int col = 0; col < 10; col++)
        {
            var paletteSquare = Instantiate(paletteButtonPrefab, paletteParent.position, Quaternion.identity);
            paletteSquare.transform.SetParent(paletteParent, false);
            paletteSquare.transform.localPosition = new Vector3(0, 0, -0.011f * col);
            paletteSquare.GetComponent<MeshRenderer>().material = new Material(colorMaterialBase) { color = _squareColors[col % 10] };
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
                var value = _skyscraperSudoku.grid[index];
                var height = value == 0 ? 0.001f : value * 0.004f;
                square.transform.localScale = new Vector3(1f, height * 1000, 1f);
                square.transform.localPosition = new Vector3(offset.x, height / 4f, offset.z);
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
            var height = _selectedPaletteColor == 0 ? 0.001f : _selectedPaletteColor * 0.004f;
            var scale = square.transform.localScale;
            scale.y = height * 1000f;
            square.transform.localScale = scale;
            var position = square.transform.localPosition;
            position.y = height / 4f;
            square.transform.localPosition = position;
            
            var highlight = square.GetComponentInChildren<KMHighlightable>().gameObject;
            highlight.transform.localScale = _selectedPaletteColor == 0 ? new Vector3(1.1f, 0.1f, 1.1f) : new Vector3(1.2f, height * 20f, 1.2f);
            highlight.transform.localPosition = _selectedPaletteColor == 0 ? new Vector3(0, 0.001f, 0f) : new Vector3(0f, height * 0.005f, 0f);
            
            return false;
        };
    }
}
