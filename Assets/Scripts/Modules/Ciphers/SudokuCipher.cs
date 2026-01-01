using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KModkit;
using KModkit.Ciphers;
using Newtonsoft.Json;
using UnityEngine;
using Words;
using Random = System.Random;

public struct SudokuData
{
    public string type;
    public ISudokuData sudokuData;

    public SudokuData(string type, ISudokuData sudokuData)
    {
        this.type = type;
        this.sudokuData = sudokuData;
    }
}

enum ViewMode
{
    Screens,
    Maze,
    Submission
}

public class SudokuCipher : Module
{
    public Material sudokuCipherMaterial;
    public MeshRenderer sudokuCipherMeshRenderer;
    public TextMesh screen1;
    public TextMesh screen2;
    public TextMesh screen3;
    public KMSelectable nextButton;
    public KMSelectable previousButton;
    public KMSelectable submitButton;
    public KMSelectable[] keyboard;
    public TextAsset regularSudokuData;
    private Queue<SudokuData> queuedSudokuTypes = new Queue<SudokuData>();
    private string currentUnencryptedWord;
    private Cipher currentCipher;
    private List<string> screenTexts = new List<string>();
    private List<string> screensViewTexts = new List<string>();
    private string userInput = "";
    private int currentPage = 1;
    private bool moduleSelected;
    private bool isGenerating;
    private int stage = 1;
    protected bool ZenModeActive;
    private ViewMode mode = ViewMode.Screens;
    
    public static string[] supportedModuleIds = new string[]
    {
        "blackSudoku",
        "purpleSudoku",
        "blueSudoku",
        "cyanSudoku",
        "greenSudoku",
        "yellowSudoku",
        "whiteSudoku",
        "redSudoku",
        "pinkSudoku",
        "RegularSudoku"
    };

    protected override void ModuleStart()
    {
        sudokuCipherMeshRenderer.material = new Material(sudokuCipherMaterial);
        nextButton.OnInteract += () =>
        {
            GoNextPage();
            return false;
        };

        previousButton.OnInteract += () =>
        {
            GoPrevPage();
            return false;
        };
        submitButton.OnInteract += delegate()
        {
            SubmitWord(submitButton);
            return false;
        };
        module.GetComponent<KMSelectable>().OnFocus += delegate { moduleSelected = true; };
        module.GetComponent<KMSelectable>().OnDefocus += delegate { moduleSelected = false; };
        foreach (var keyButton in keyboard)
        {
            var pressedButton = keyButton;
            pressedButton.OnInteract += delegate()
            {
                LetterPress(pressedButton);
                return false;
            };
        }

        var modules = bomb.GetModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
        if (modules.Count == 0)
        {
            currentCipher = CipherFactory.CreateCipher("Regular", regularSudokuData);
            PopulateScreens();
        }
    }

    private void GoNextPage()
    {
        if (mode != ViewMode.Screens)
            return;
        nextButton.AddInteractionPunch();
        audio.PlaySoundAtTransform("ArrowPress.ogg", nextButton.transform);
        var maxPage = Mathf.CeilToInt(screenTexts.Count / 3f);
        currentPage++;
        if (currentPage > maxPage)
            currentPage = 1;
    }
    
    private void GoPrevPage()
    {
        if (mode != ViewMode.Screens)
            return;
        previousButton.AddInteractionPunch();
        audio.PlaySoundAtTransform("ArrowPress.ogg", previousButton.transform);
        var maxPage = Mathf.CeilToInt(screenTexts.Count / 3f);
        currentPage--;
        if (currentPage < 1)
            currentPage = maxPage;
    }

    private void PopulateScreens()
    {
        if (currentCipher == null)
            return;

        isGenerating = true;
        screenTexts = new List<string> { "...", "...", "..." };
        currentPage = 1;
        mazeLives = 0;
        currentMazeLives = 0;

        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"New Cipher (Stage {stage}) Generated - {currentCipher.Name}");
        
        StartCoroutine(currentCipher.GeneratePuzzle(result =>
        {
            currentUnencryptedWord = result.UnencryptedWord;
            mazeLives = result.MazeLives;
            currentMazeLives = mazeLives;
            screenTexts = result.ScreenTexts;
            screensViewTexts = new List<string>(screenTexts);
            isGenerating = false;
            foreach (var line in result.DebugLogs)
                Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, line);
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Stage {stage} Word: {currentUnencryptedWord}");
        }));
    }

    private float hue = 0;
    private void Update()
    {
        var pageTexts = screenTexts
            .Skip(3 * (currentPage - 1))
            .Take(3)
            .ToArray();

        screen1.text = pageTexts.Length > 0 ? pageTexts[0] ?? "" : "";
        screen2.text = pageTexts.Length > 1 ? pageTexts[1] ?? "" : "";
        screen3.text = pageTexts.Length > 2 ? pageTexts[2] ?? "" : "";

        if (mode == ViewMode.Submission && currentPage == 1)
        {
            screen1.text = "";
            screen2.text = "";
            screen3.text = userInput;
        }
        
        if (_isSolved)
            return;
        
        currentCipher?.SetColor(ref sudokuCipherMeshRenderer);
        if (currentCipher == null)
        {
            hue += Time.deltaTime * 0.1f;
            if (hue > 1f) hue = 0f;
            sudokuCipherMeshRenderer.material.color = Color.HSVToRGB(hue, 0.4f, 0.7f);
        }
        
        var modules = bomb.GetModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
        var solvedModules = bomb.GetSolvedModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
        foreach (var m in solvedModules)
            modules.Remove(m);
        if (modules.Count == 0 && queuedSudokuTypes.Count == 0 && currentCipher == null)
        {
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, "Module Solved - No Remaining Sudokus");
            sudokuCipherMeshRenderer.material.color = Color.HSVToRGB(0f, 0f, 0.5f);
            module.HandlePass();
        }

        if (!moduleSelected) return;
        for (var ltr = 0; ltr < 26; ltr++)
            if (Input.GetKeyDown(((char)('a' + ltr)).ToString()))
                keyboard[GetPositionFromChar((char)('A' + ltr))].OnInteract();
        if (Input.GetKeyDown(KeyCode.Return))
            submitButton.OnInteract();
    }
    
    private void EnterScreens()
    {
        mode = ViewMode.Screens;
        userInput = "";
        currentPage = 1;
        screenTexts = new List<string>(screensViewTexts);
    }

    private void EnterSubmission()
    {
        mode = ViewMode.Submission;
        userInput = "";
        currentPage = 1;
    }

    private void EnterMaze()
    {
        mode = ViewMode.Maze;
        currentPage = 1;   
        screenTexts.Clear();
        MazeModeLetterPress("S");
    }

    protected virtual void SubmitWord(KMSelectable button)
    {
        if (_isSolved || currentCipher == null || isGenerating)
            return;

        button.AddInteractionPunch();
        
        switch(mode)
        {
            case ViewMode.Maze:
                Debug.LogFormat("[Sudoku Cipher #{0}] Maze → Submission", moduleId);
                EnterSubmission();
                return;
            case ViewMode.Submission:
                if (string.IsNullOrEmpty(userInput))
                {
                    Debug.LogFormat("[Sudoku Cipher #{0}] Submission → Screens", moduleId);
                    EnterScreens();
                    return;
                }
                if (userInput.Equals(currentUnencryptedWord, StringComparison.OrdinalIgnoreCase)
                    || (screenTexts.Count > 0 && screenTexts[0] == "Error"))
                { 
                    if (screenTexts.Count > 0 && screenTexts[0] == "Error")
                        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"An error occurred for stage {{stage}} - {currentCipher.Name}, Stage Passed.");
                    else
                        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Submitted {userInput}");
                    audio.PlaySoundAtTransform("SolveSFX", transform);
                    userInput = "";
                    screenTexts.Clear();
                    screensViewTexts.Clear();
                    currentCipher = null;
                    mode = ViewMode.Screens;
                    var modules = bomb.GetModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
                    var solvedModules = bomb.GetSolvedModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
                    foreach (var m in solvedModules)
                        modules.Remove(m);

                    if (queuedSudokuTypes.Count > 0 && !isGenerating)
                    {
                        var sudoku = queuedSudokuTypes.Dequeue();
                        currentCipher = CipherFactory.CreateCipher(sudoku.type, sudoku.sudokuData);
                        PopulateScreens();
                    }
                    else if (modules.Count == 0 && !isGenerating)
                    {
                        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Module Solved - No Remaining Sudokus");
                        sudokuCipherMeshRenderer.material.color = Color.HSVToRGB(0f, 0f, 0.5f);
                        module.HandlePass();
                        _isSolved = true;
                    }
                }
                else
                {
                    Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Submitted {userInput}... Strike!");
                    audio.PlaySoundAtTransform("StrikeSFX", transform);
                    module.HandleStrike();
                    EnterScreens();
                }
                break;
            case ViewMode.Screens:
                Debug.LogFormat("[Sudoku Cipher #{0}] Screens → Submission", moduleId);
                EnterSubmission();
                return;
        }
    }

    private int GetPositionFromChar(char c)
    {
        return "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);
    }

    private void LetterPress(KMSelectable pressed)
    {
        if (_isSolved || currentCipher == null || isGenerating)
            return;
        pressed.AddInteractionPunch(.2f);
        audio.PlaySoundAtTransform("KeyboardPress", transform);

        if (mode != ViewMode.Submission)
        {
            if (currentCipher.IsMaze)
            {
                var letter = pressed.GetComponentInChildren<TextMesh>().text;
                if (letter != "S" && mode != ViewMode.Maze)
                    return;
                if (mode != ViewMode.Maze)
                {
                    Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, "Entering Movement Mode");
                    EnterMaze();
                }
                else
                    MazeModeLetterPress(letter);
                return;
            }

            EnterSubmission();
        }

        if (userInput.Length < 6)
            userInput += pressed.GetComponentInChildren<TextMesh>().text;
        else
            userInput = pressed.GetComponentInChildren<TextMesh>().text;
    }

    private int mazeLives;
    private int currentMazeLives;
    private void MazeModeLetterPress(string letter)
    {
        var currentLetter = screenTexts.Count > 1 ? screenTexts[1] : "?";
        screenTexts.Clear();
        var value = currentCipher.Move(moduleId, letter);
        switch (value)
        {
            case "*":
                Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Incorrect movement, Strike.");
                audio.PlaySoundAtTransform("StrikeSFX", transform);
                module.HandleStrike();
                EnterScreens();
                return;
            case "!":
            {
                currentMazeLives -= 1;
                if (currentMazeLives == 0)
                {
                    Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Maze life lost.");
                    audio.PlaySoundAtTransform("StrikeSFX", transform);
                    module.HandleStrike();
                    EnterScreens();
                    PopulateScreens();
                    return;
                }
                break;
            }
        }
        screenTexts.Add(mazeLives > 0 ? $"❤{currentMazeLives}" : "");
        screenTexts.Add(value != "!" ? value : currentLetter);
        screenTexts.Add("");
    }
    
    public void Enqueue(string type, ISudokuData sudokuData)
    {
        if (_isSolved)
            return;
        if (currentCipher == null)
        {
            currentCipher = CipherFactory.CreateCipher(type, sudokuData);
            PopulateScreens();
        }
        else
        {
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Sudoku completed, queueing new cipher - {type}.");
            queuedSudokuTypes.Enqueue(new SudokuData(type, sudokuData));
        }
    }
    
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} sub (press the submit button), !{0} l/r (move page left or right), !{0} input <text> (press the letter buttons on the keyboard)";
#pragma warning restore 414
	
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (Regex.IsMatch(command, @"^\s*(?:sub)\s*$", RegexOptions.IgnoreCase))
        {
            yield return null;
            if (mode == ViewMode.Submission && !ZenModeActive && currentCipher != null && userInput.Equals(currentUnencryptedWord, StringComparison.OrdinalIgnoreCase))
                yield return "awardpointsonsolve " + currentCipher.TwitchPlaysPoints;
            SubmitWord(submitButton);
            yield break;
        }
        var match = Regex.Match(command, @"^\s*([lr])\s*$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            yield return null;
            string direction = match.Groups[1].Value.ToLower();
            if (direction == "l")
                GoPrevPage();
            else if (direction == "r")
                GoNextPage();
            yield break;
        }
        match = Regex.Match(command, @"^\s*(?:input) (\D+)\s*$", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            yield return null;
            var text = match.Groups[1].Value.ToLower();
            foreach (var c in text.ToUpperInvariant())
            {
                var pos = GetPositionFromChar(c);
                if (pos != -1 && pos < keyboard.Length)
                    keyboard[pos].OnInteract();
                else
                    yield break;
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
        yield return null;
    }
    
    protected IEnumerator TwitchHandleForcedSolve()
    {
        if (_isSolved)
            yield break;
        audio.PlaySoundAtTransform("SolveSFX", transform);
        userInput = "";
        screenTexts.Clear();
        screensViewTexts.Clear();
        currentCipher = null;
        EnterScreens();
        Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Module Solved - Forced Solve");
        sudokuCipherMeshRenderer.material.color = Color.HSVToRGB(0f, 0f, 0.5f);
        module.HandlePass();
        _isSolved = true;
    }

    private int getPositionFromChar(char c)
    {
        return "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);
    }
}