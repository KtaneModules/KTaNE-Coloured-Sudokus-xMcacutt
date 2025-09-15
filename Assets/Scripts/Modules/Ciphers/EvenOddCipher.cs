using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Utility;
using Words;
using Random = System.Random;

namespace KModkit.Ciphers
{
    public class EvenOddCipher : Cipher
    {
        private EvenOddSudokuData sudokuData;
        private Random random = new Random();
        private CellRef currentCell;
        private MazeNode[] mazeArray;

        public EvenOddCipher(EvenOddSudokuData sudokuData)
        {
            this.sudokuData = sudokuData;
            Name = "Even Odd";
            IsMaze = false;
        }

        public override IEnumerator GeneratePuzzle(Action<CipherResult> onComplete)
        {
            var wordIndex = random.Next(sudokuData.words.Count);
            var hiddenWord = new Data().PickBestWord(6, w => w == sudokuData.words[wordIndex] ? 0 : 1);
            var encryptedWord = string.Join("", sudokuData.words[wordIndex]
                .Select((c, i) => ((char)('A' + (c - 'A' + hiddenWord[i] - 'A') % 26 + 1)).ToString()).ToArray());
            var screenTexts = new List<string>();
            screenTexts.Add(encryptedWord);
            var word = sudokuData.words[wordIndex];
            var startIndex = sudokuData.start_indices[wordIndex];
            var startRow = (startIndex / 9) + 1;
            var startCol = (char)('A' + startIndex % 9);
            screenTexts.Add(startCol.ToString() + startRow.ToString());
            screenTexts.Add(sudokuData.lengths[wordIndex].ToString() + word[0]);
            var debugLogs = new List<string>();
            debugLogs.Add($"[GeneratePuzzle] Morse word: {encryptedWord}");
            
            var result = new CipherResult()
            {
                EncryptedWord = encryptedWord,
                UnencryptedWord = hiddenWord,
                ScreenTexts = screenTexts,
                DebugLogs = debugLogs
            };
            onComplete(result);
            yield break;
        }

        public override void SetColor(ref MeshRenderer renderer)
        {
            renderer.material.color = Colors.Pink;
        }
    }
}