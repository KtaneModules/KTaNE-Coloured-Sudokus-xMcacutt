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
    public class CellRef
    {
        public int Row;
        public int Col;
        public CellRef(int row, int col) { Row = row; Col = col; }

        public override string ToString() { return ((char)('A' + Col)).ToString() + (Row + 1).ToString(); }

        public static bool operator ==(CellRef left, CellRef right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;
            return left.Equals(right);
        }
        
        public static bool operator !=(CellRef left, CellRef right) { return !(left == right); }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(CellRef))
                return false;
            var other = (CellRef)obj;
                return Row == other.Row && Col == other.Col;
        }
        
        public override int GetHashCode() { throw new NotSupportedException("Attempted to use CellRef as a hash key."); }
    }
    
    public class CipherResult
    {
        public string EncryptedWord { get; set; }
        public string UnencryptedWord { get; set; }
        public List<string> ScreenTexts { get; set; }
        public int MazeLives { get; set; } = 0;
        public List<string> DebugLogs { get; set; }
    }
    
    public abstract class Cipher
    {
        public string Name { get; set; } = "";
        public bool IsMaze = false;
        public int TwitchPlaysPoints;
        protected static readonly Random Random = new Random();
        private static readonly Data Data = new Data();

        protected static readonly CipherResult ErrorResult = new CipherResult
        {
            EncryptedWord = null,
            UnencryptedWord = null,
            ScreenTexts = new List<string> { "Error", "Press", "Submit" },
            DebugLogs = new List<string> { "Generation resulted in an error, press submit to continue." }
        };
        
        protected static char[][] GenerateLetterGrid(out List<string> keyWords, out string letterShifts)
        {
            keyWords = new List<string>();
            letterShifts = "";
            var key = "";
            for (var i = 0; i < 3; i++)
            {
                keyWords.Add(Data.PickWord(4, 7));
                letterShifts += (char)('A' + Random.Next(0, 26));
                var initialKey = keyWords[i].CreateKey();
                key += initialKey.Replace(letterShifts[i], '#') + letterShifts[i];
            }
            return key.Select((value, index) => new { value, row = index / 9 })
                .GroupBy(x => x.row)
                .Select(g => g.Select(x => x.value).ToArray())
                .ToArray();
        }

        public virtual string Move(int moduleId, string direction) { return "?"; }

        public abstract IEnumerator GeneratePuzzle(Action<CipherResult> onComplete);

        public abstract void SetColor(ref MeshRenderer renderer);
    }
}