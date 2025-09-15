using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    private List<string> backupScreenTexts = new List<string>();
    private string userInput = "";
    private int currentPage = 1;
    private bool moduleSelected;
    private bool moduleSolved;
    private bool inSubmissionMode;
    private bool isGenerating;
    private bool isMazeMode;
    private int stage = 1;
    
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
        "pinkSudoku"
    };

    protected override void ModuleStart()
    {
        sudokuCipherMeshRenderer.material = new Material(sudokuCipherMaterial);
        nextButton.OnInteract += () =>
        {
            nextButton.AddInteractionPunch();
            audio.PlaySoundAtTransform("ArrowPress.ogg", nextButton.transform);
            var maxPage = Mathf.CeilToInt(screenTexts.Count / 3f);
            currentPage++;
            if (currentPage > maxPage)
                currentPage = 1;
            return false;
        };

        previousButton.OnInteract += () =>
        {
            previousButton.AddInteractionPunch();
            audio.PlaySoundAtTransform("ArrowPress.ogg", previousButton.transform);
            var maxPage = Mathf.CeilToInt(screenTexts.Count / 3f);
            currentPage--;
            if (currentPage < 1)
                currentPage = maxPage;
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

        currentCipher = CipherFactory.CreateCipher("Regular", regularSudokuData);
        PopulateScreens();
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
            isGenerating = false;
            foreach (var line in result.DebugLogs)
                Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, line);
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Stage {stage} Word: {currentUnencryptedWord}");
        }));
    }

    private void Update()
    {
        currentCipher?.SetColor(ref sudokuCipherMeshRenderer);

        var pageTexts = screenTexts
            .Skip(3 * (currentPage - 1))
            .Take(3)
            .ToArray();

        screen1.text = pageTexts.Length > 0 ? pageTexts[0] ?? "" : "";
        screen2.text = pageTexts.Length > 1 ? pageTexts[1] ?? "" : "";
        screen3.text = pageTexts.Length > 2 ? pageTexts[2] ?? "" : "";

        if (inSubmissionMode && currentPage == 1)
        {
            screen1.text = "";
            screen2.text = "";
            screen3.text = userInput;
        }

        var modules = bomb.GetModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
        var solvedModules = bomb.GetSolvedModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
        foreach (var m in solvedModules)
            modules.Remove(m);
        if (modules.Count == 0 && queuedSudokuTypes.Count == 0 && currentCipher == null)
        {
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, "Module Solved - No Remaining Sudokus");
            module.HandlePass();
        }

        if (!moduleSelected) return;
        for (var ltr = 0; ltr < 26; ltr++)
            if (Input.GetKeyDown(((char)('a' + ltr)).ToString()))
                keyboard[GetPositionFromChar((char)('A' + ltr))].OnInteract();
        if (Input.GetKeyDown(KeyCode.Return))
            submitButton.OnInteract();
    }

    protected virtual void SubmitWord(KMSelectable button)
    {
        if (moduleSolved || currentCipher == null || isGenerating)
            return;

        button.AddInteractionPunch();

        if ((inSubmissionMode && userInput.Equals(currentUnencryptedWord, StringComparison.OrdinalIgnoreCase)) 
            || (screenTexts.Count > 0 && screenTexts[0] == "Error"))
        {
            if (screenTexts.Count > 0 && screenTexts[0] == "Error")
                Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"An error occurred for stage {{stage}} - {currentCipher.Name}, Stage Passed.");
            else
                Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Submitted {userInput}");
            audio.PlaySoundAtTransform("SolveSFX", transform);
            userInput = "";
            screenTexts.Clear();
            currentCipher = null;
            inSubmissionMode = false;
            var modules = bomb.GetModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
            var solvedModules = bomb.GetSolvedModuleIDs().Where(x => supportedModuleIds.Contains(x)).ToList();
            foreach (var m in solvedModules)
                modules.Remove(m);

            if (queuedSudokuTypes.Count > 0 && !isGenerating)
            {
                var sudoku = queuedSudokuTypes.Dequeue();
                currentCipher = CipherFactory.CreateCipher(sudoku.type, sudoku.sudokuData);
                PopulateScreens();
                inSubmissionMode = false;
            }
            else if (modules.Count == 0 && !isGenerating)
            {
                Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Module Solved - No Remaining Sudokus");
                module.HandlePass();
                moduleSolved = true;
                inSubmissionMode = false;
            }
            return;
        }
        
        if (!inSubmissionMode)
        {
            inSubmissionMode = true;
            backupScreenTexts = new List<string>(screenTexts);
            userInput = "";
            currentPage = 1;
        }
        else
        {
            Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Submitted {userInput}... Strike!");
            audio.PlaySoundAtTransform("StrikeSFX", transform);
            module.HandleStrike();
            ResetToInitialState();
        }
    }

    private int GetPositionFromChar(char c)
    {
        return "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);
    }

    private void LetterPress(KMSelectable pressed)
    {
        if (moduleSolved || currentCipher == null || isGenerating)
            return;
        pressed.AddInteractionPunch(.2f);
        audio.PlaySoundAtTransform("KeyboardPress", transform);

        if (!inSubmissionMode)
        {
            if (currentCipher.IsMaze)
            {
                var letter = pressed.GetComponentInChildren<TextMesh>().text;
                if (letter != "S" && !isMazeMode)
                    return;
                if (!isMazeMode)
                {
                    Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, "Entering Movement Mode");
                    backupScreenTexts = new List<string>(screenTexts);
                    screenTexts.Clear();
                    MazeModeLetterPress("S");
                    isMazeMode = true;
                }
                else
                    MazeModeLetterPress(letter);
                return;
            }

            inSubmissionMode = true;
            backupScreenTexts = new List<string>(screenTexts);
            userInput = "";
            currentPage = 1;
        }

        if (userInput.Length < 6)
        {
            userInput += pressed.GetComponentInChildren<TextMesh>().text;
        }
        else
        {
            userInput = pressed.GetComponentInChildren<TextMesh>().text;
        }
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
                ResetToInitialState();
                return;
            case "!":
            {
                currentMazeLives -= 1;
                if (currentMazeLives == 0)
                {
                    Debug.LogFormat("[Sudoku Cipher #{0}] {1}", moduleId, $"Maze life lost.");
                    audio.PlaySoundAtTransform("StrikeSFX", transform);
                    module.HandleStrike();
                    ResetToInitialState();
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
    
    private void ResetToInitialState()
    {
        screenTexts = new List<string>(backupScreenTexts);
        userInput = "";
        inSubmissionMode = false;
        isMazeMode = false;
        currentPage = 1;
    }
}