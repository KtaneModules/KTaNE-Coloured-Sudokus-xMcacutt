using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Utility
{
    public static class Utility
    {
        public static string CreateKey(this string word)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var seen = new HashSet<char>();
            var builder = new StringBuilder();
            foreach (var c in word.ToUpper().Where(c => alphabet.Contains(c) && seen.Add(c)))
                builder.Append(c);
            foreach (var c in alphabet.Where(c => seen.Add(c)))
                builder.Append(c);
            return builder.ToString();
        }
        
        public static int Mod9(this int x) => ((x % 9) + 9) % 9;
    }
}