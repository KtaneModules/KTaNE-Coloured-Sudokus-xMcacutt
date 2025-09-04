using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Words;
using System.Linq;
using Utility;
using Random = System.Random;

namespace KModkit.Ciphers
{
    public class CipherResult
    {
        public string EncryptedWord { get; set; }
        public string UnencryptedWord { get; set; }
        public List<string> ScreenTexts { get; set; }
    }
    
    public abstract class Cipher
    {
        string Name { get; }
        
        protected static char[][] GenerateLetterGrid(out List<string> keyWords, out string letterShifts)
        {
            var data = new Data();
            var random = new Random();
            keyWords = new List<string>();
            letterShifts = "";
            var key = "";
            for (var i = 0; i < 3; i++)
            {
                keyWords.Add(data.PickWord(4, 7));
                letterShifts += (char)('A' + random.Next(0, 26));
                var initialKey = keyWords[i].CreateKey();
                key += initialKey.Replace(letterShifts[i], '#') + letterShifts[i];
            }
            return key.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
        }

        public abstract IEnumerator GeneratePuzzle(Action<CipherResult> onComplete);

        public abstract void SetColor(ref MeshRenderer renderer);
    }
}