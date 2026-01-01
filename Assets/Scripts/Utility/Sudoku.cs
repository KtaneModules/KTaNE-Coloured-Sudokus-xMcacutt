using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

namespace KModkit
{
    public class Sudoku<T> : MonoBehaviour where T : ISudokuData
    {
        [NonSerialized] public KMBombInfo Bomb;
        [NonSerialized] public KMAudio Audio;
        [NonSerialized] public KMBombModule Module;
        [NonSerialized] public KMSelectable moduleSelectable;
        [NonSerialized] public KMColorblindMode colorblindMode;
        public TextAsset sudokuJson;
        public Transform topLeft;
        public KMSelectable submitButton;
        public KMSelectable resetButton;
        public GameObject squarePrefab;
        public GameObject paletteButtonPrefab;
        public Transform paletteParent;
        public Material colorMaterialBase;
        public string sudokuColorName;
        public string sudokuTypeName;
        [NonSerialized] public int moduleId;
        private static int _moduleIdCounter = 1;
        private bool _isSolved;
        protected List<Color> SquareColours;
        protected ColouredSudokuSettings settings;
        private static Random random = new Random();
        public bool startSolvedDebug = false;

        protected T SudokuData;
        protected readonly List<GameObject> Squares = new List<GameObject>();
        private readonly List<GameObject> _palette = new List<GameObject>();
        protected readonly Dictionary<string, List<GameObject>> EdgeObjects = new Dictionary<string, List<GameObject>>();
        protected readonly Dictionary<string, List<GameObject>> EarlyObjects = new Dictionary<string, List<GameObject>>();
        
        protected readonly int[] SquareIndices = new int[81];
        protected int SelectedPaletteColour;


        private void Start()
        {
            Module = GetComponent<KMBombModule>();
            Audio = GetComponent<KMAudio>();
            Bomb = GetComponent<KMBombInfo>();
            moduleSelectable = GetComponent<KMSelectable>();
            colorblindMode = GetComponent<KMColorblindMode>();

            var modConfig = new ModConfig<ColouredSudokuSettings>("Coloured Sudokus");
            settings = modConfig.Read();
            modConfig.Write(settings);
#if UNITY_EDITOR
            settings.startSolved = startSolvedDebug;
#endif            
            moduleId = _moduleIdCounter++;
            InitializeMaterials();
            var indexedPuzzles = JsonConvert.DeserializeObject<List<T>>(sudokuJson.text)
                .Select((puzzle, index) => new { Puzzle = puzzle, Index = index })
                .OrderBy(_ => random.Next())
                .ToList();
            var selected = indexedPuzzles.First();
            SudokuData = selected.Puzzle;
            var originalIndex = selected.Index;
            $"{sudokuColorName} Sudoku Puzzle #{originalIndex}".Log(this);
            
            $"Initial Grid".Log(this);
            var result = Enumerable.Range(0, 9)
                .Select(i => SudokuData.grid.Skip(i * 9).Take(9).Join(" "))
                .ToList();
            foreach (var row in result)
                row.Log(this);
            
            $"Solution".Log(this);
            result = Enumerable.Range(0, 9)
                .Select(i => SudokuData.solution.Skip(i * 9).Take(9).Join(" "))
                .ToList();
            foreach (var row in result)
                row.Log(this);

            LogClues();
            GenerateObjectsEarly();
            
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
                    
                    var ciphers = FindObjectsOfType<SudokuCipher>();
                    foreach (var cipher in ciphers)
                        if (cipher.transform.parent.GetInstanceID() == transform.parent.GetInstanceID())
                            cipher.Enqueue(sudokuTypeName, SudokuData);

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

        protected virtual void GenerateObjectsEarly() { }
        protected virtual void LogClues() { }

        protected virtual bool IsValid()
        {
            if (SquareIndices.Any(s => s == 0))
                return false;
            
            if (SquareIndices.Any(s => s == 0))
            {
                $"Strike! There was an empty square in the input.".Log(this);
                return false;
            }

            var grid = new int[9][];
            for (var index = 0; index < 9; index++)
                grid[index] = new int[9];

            for (var i = 0; i < 81; i++)
                grid[i / 9][i % 9] = SquareIndices[i];
            
            for (var row = 0; row < 9; row++)
            {
                var nums = new HashSet<int>();
                for (var col = 0; col < 9; col++)
                {
                    if (nums.Add(grid[row][col])) 
                        continue;
                    $"Strike! Row {row + 1} has a duplicate colour.".Log(this);
                    return false;
                }
            }
            
            for (var col = 0; col < 9; col++)
            {
                var nums = new HashSet<int>();
                for (var row = 0; row < 9; row++)
                {
                    if (nums.Add(grid[row][col]))
                        continue;
                    $"Strike! Column {col + 1} has a duplicate colour.".Log(this);
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
                            if (nums.Add(val))
                                continue;
                            $"Strike! The square at row {boxRow * 3 + i + 1}, column {boxCol * 3 + j + 1} is a duplicate colour within its 3x3.".Log(this);
                            return false;
                        }
                    }
                }
            }
            
            for (var i = 0; i < 81; i++)
            {
                if (SquareIndices[i] == SudokuData.solution[i]) continue;
                var mismatchedIndices = Enumerable.Range(0, 81)
                    .Where(j => SquareIndices[j] != SudokuData.solution[j])
                    .ToList();
                $"Strike! The following square indices do not match the solution: {mismatchedIndices.Join(", ")}".Log(this);
                return false;
            }
            return true;
        }

        private bool _isResetting = false;
        private IEnumerator ResetSudoku(bool fullReset = false)
        {
            _isResetting = true;

            foreach (var square in Squares)
            {
                Destroy(square);
                yield return new WaitForSeconds(0.001f);
            }

            Squares.Clear();

            if (fullReset)
            {
                foreach (var value in EdgeObjects.Values)
                {
                    foreach (var obj in value)
                    {
                        Destroy(obj);
                        yield return new WaitForSeconds(0.001f);
                    }
                    value.Clear();
                }

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
                GenerateObjects();
            }

            _isResetting = false;
        }

        private void InitializeMaterials()
        {
            SquareColours = new List<Color>
            {
                Colors.Red, Colors.Green, Colors.Blue, Colors.Purple,
                Colors.Yellow, Colors.Orange, Colors.Cyan, Colors.Pink, Colors.White
            };
            SquareColours = SquareColours.OrderBy(_ => UnityEngine.Random.value).ToList();
            SquareColours.Insert(0, Colors.Black);
        }

        protected virtual void GenerateObjects() { }

        private IEnumerator GeneratePalette()
        {
            for (int col = 0; col < 10; col++)
            {
                var paletteSquare = Instantiate(paletteButtonPrefab, paletteParent.position, Quaternion.identity);
                paletteSquare.transform.SetParent(paletteParent, false);
                paletteSquare.transform.localPosition = new Vector3(0, 0, -0.011f * col);
                paletteSquare.GetComponent<MeshRenderer>().material = new Material(colorMaterialBase)
                    { color = SquareColours[col % 10] };
                if ((colorblindMode.ColorblindModeActive || settings.babyMode) && col != 0)
                    paletteSquare.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(SquareColours[col % 10], settings.babyMode ? col.ToString() : null);
                AddPaletteButton(paletteSquare, col);
                yield return new WaitForSeconds(0.001f);
            }
        }

        protected virtual IEnumerator GenerateSquares()
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
                    var value = settings.startSolved ? SudokuData.solution[index] : SudokuData.grid[index];
                    SquareIndices[index] = value;
                    square.GetComponent<MeshRenderer>().material.color = SquareColours[value];
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

        private void AddPaletteButton(GameObject button, int index)
        {
            _palette.Add(button);
            var paletteSelectable = button.GetComponent<KMSelectable>();
            paletteSelectable.Parent = moduleSelectable;
            moduleSelectable.Children = moduleSelectable.Children.Concat(new[] { paletteSelectable }).ToArray();
            moduleSelectable.UpdateChildrenProperly();

            paletteSelectable.OnInteract += () =>
            {
                SelectedPaletteColour = index;
                return false;
            };
        }

        protected virtual void AddSquare(GameObject square, int gridIndex, bool canInteract)
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
                if (colorblindMode.ColorblindModeActive || settings.babyMode)
                    square.GetComponentInChildren<ColorblindHelperScript>().SetFromColor(SquareColours[SelectedPaletteColour], 
                        settings.babyMode && SelectedPaletteColour != 0 ? SelectedPaletteColour.ToString() : null);
                SquareIndices[gridIndex] = SelectedPaletteColour;
                return false;
            };
        }
        
        protected IEnumerator CreateClueLight(GameObject prefab, Transform parent, Vector3 offset, int clue, bool dualColour = false)
        {
            if (!EdgeObjects.ContainsKey("Clue Lights"))
                EdgeObjects.Add("Clue Lights", new List<GameObject>());
            var clueLightObject = Instantiate(prefab, parent.position, Quaternion.identity);
            clueLightObject.transform.SetParent(parent, false);
            clueLightObject.transform.localPosition = new Vector3(offset.x, 0, offset.z);
            var clueLight = clueLightObject.GetComponent<ClueLight>();
            
            clueLight.colorblindActive = colorblindMode.ColorblindModeActive;
            if (dualColour)
                clueLight.SetColor(colorMaterialBase, SquareColours[clue / 10], SquareColours[clue % 10], settings.babyMode, clue / 10, clue % 10);
            else
                clueLight.SetColor(colorMaterialBase,  SquareColours[clue], settings.babyMode, clue);
            EdgeObjects["Clue Lights"].Add(clueLightObject);
            yield return new WaitForSeconds(0.001f);
        }

        protected IEnumerator CreateLine(GameObject prefab, Transform parent, Vector3 offset, bool vertical, bool large)
        {
            if (!EdgeObjects.ContainsKey("Lines"))
                EdgeObjects.Add("Lines", new List<GameObject>());
            var rot = vertical ? Quaternion.Euler(0, 90, 0) : Quaternion.Euler(0, 0, 0);
            var line = Instantiate(prefab, Vector3.zero, rot);
            line.transform.SetParent(parent, false);
            line.transform.localPosition += offset;
            if (large)
                line.transform.localScale += new Vector3(0.1f, 0, 0);
            line.GetComponentInChildren<MeshRenderer>().material = new Material(colorMaterialBase) { color = Colors.BorderLine };
            EdgeObjects["Lines"].Add(line);
            yield return new WaitForSeconds(0.001f);
        }

        #region Twitch Plays
        //The message to send to players showing available commands
        #pragma warning disable 414
        protected readonly string TwitchHelpMessage = @"!{0} W A4 F7 [Select a color and cells] | !{0} submit/reset [Press submit or reset] | Colors are blacK, White, Red, Green, Blue, Yellow, Cyan, Orange, Violet, and Pink";
        #pragma warning restore 414
        //Process commands sent from TP to the module
        protected IEnumerator ProcessTwitchCommand(string command)
        {
            if (command.EqualsIgnoreCase("submit")) //If the command is submit, then submit
            {
                yield return null; //Let TP know the command is valid, and focus on the module
                submitButton.OnInteract();
                yield break;
            }
            if (command.EqualsIgnoreCase("reset")) //If the command is reset, then reset
            {
                yield return null;
                resetButton.OnInteract();
                yield break;
            }
            command = Regex.Replace(command, @"\s+", " ").Trim(); //Remove excess spaces from the command
            string[] parameters = command.Split(' '); //Split the command by spaces
            for (int i = 0; i < parameters.Length; i++) //Verify each part of the command is valid
            {
                if (parameters[i].Length == 1 && !"kwrgbycovp".Contains(parameters[i].ToLowerInvariant())) //Check if the part is 1 character and not a valid color
                    yield break;
                else if (parameters[i].Length == 2) //Check if the part is 2 characters
                {
                    if (!"abcdefghi".Contains(parameters[i].ToLowerInvariant()[0]) || !"123456789".Contains(parameters[i][1])) //Check if the part is not a valid cell
                        yield break;
                    if (!Squares["123456789".IndexOf(parameters[i][1]) * 9 + "abcdefghi".IndexOf(parameters[i].ToLowerInvariant()[0])].GetComponent<KMSelectable>().enabled) //Ensure the cell is selectable
                    {
                        yield return "sendtochaterror The specified cell '" + parameters[i] + "' cannot be selected!";
                        yield break;
                    }
                }
                else if (parameters[i].Length > 2) //Check if the part is more than 2 characters
                    yield break;
            }
            yield return null;
            for (int i = 0; i < parameters.Length; i++) //Select all colors and cells that were specified
            {
                if (parameters[i].Length == 1)
                {
                    switch (parameters[i].ToLowerInvariant()) //Get the color button for the specified color
                    {
                        case "k":
                            _palette[SquareColours.IndexOf(Colors.Black)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "w":
                            _palette[SquareColours.IndexOf(Colors.White)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "r":
                            _palette[SquareColours.IndexOf(Colors.Red)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "g":
                            _palette[SquareColours.IndexOf(Colors.Green)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "b":
                            _palette[SquareColours.IndexOf(Colors.Blue)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "y":
                            _palette[SquareColours.IndexOf(Colors.Yellow)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "c":
                            _palette[SquareColours.IndexOf(Colors.Cyan)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "o":
                            _palette[SquareColours.IndexOf(Colors.Orange)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        case "v":
                            _palette[SquareColours.IndexOf(Colors.Purple)].GetComponent<KMSelectable>().OnInteract();
                            break;
                        default:
                            _palette[SquareColours.IndexOf(Colors.Pink)].GetComponent<KMSelectable>().OnInteract();
                            break;
                    }
                }
                else
                    Squares["123456789".IndexOf(parameters[i][1]) * 9 + "abcdefghi".IndexOf(parameters[i].ToLowerInvariant()[0])].GetComponent<KMSelectable>().OnInteract();
                yield return new WaitForSeconds(0.1f); //Add a sliver of delay between selections, 0.1 seconds is the default for TP
            }
        }

        //Make the module solve itself if it is forcefully solved by TP
        protected IEnumerator TwitchHandleForcedSolve()
        {
            while (_isResetting) yield return true; //Let other modules in solve queue go while the module is resetting
            List<int> order = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            if (SelectedPaletteColour != 0) //If a color is already selected, then make sure it is checked over first
            {
                order.Remove(SelectedPaletteColour);
                order.Insert(0, SelectedPaletteColour);
            }
            for (int i = 0; i < 9; i++) //Loop through every cell for each color
            {
                for (int j = 0; j < 81; j++)
                {
                    if (SudokuData.solution[j] == order[i] && SquareIndices[j] != order[i]) //If the current cell needs to be the current color and it isn't, then set it
                    {
                        if (SelectedPaletteColour != order[i]) //Select the current color if it is not already selected
                        {
                            _palette[order[i]].GetComponent<KMSelectable>().OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                        Squares[j].GetComponent<KMSelectable>().OnInteract();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            submitButton.OnInteract(); //Finally, press submit
        }
        #endregion
    }
}