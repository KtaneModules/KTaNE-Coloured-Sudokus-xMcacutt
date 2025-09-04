using System.Collections.Generic;
using Newtonsoft.Json;

namespace KModkit
{
    public class ISudokuData
    {
        public List<int> grid { get; set; } = new List<int>();
        public List<int> solution { get; set; } = new List<int>();
    }

    public class RegularSudokuData : ISudokuData
    {
        
    }

    public class SandwichSudokuData : ISudokuData
    {
        public List<int> row_sums { get; set; } = new List<int>();
        public List<int> col_sums { get; set; } = new List<int>();
    }
    
    public class LittleKillerSudokuData : ISudokuData
    {
        public List<List<int>> little_killer_clues { get; set; } = new List<List<int>>();
    }
    
    public class KillerSudokuData : ISudokuData
    {
        public List<int> cages { get; set; } = new List<int>();
        public List<int> cage_sums { get; set; } = new List<int>();
    }
    
    public class PalindromeSudokuData : ISudokuData
    {
        public List<List<int>> lines { get; set; } = new List<List<int>>();
    }
    
    public class AntiKnightSudokuData : ISudokuData
    {
    }
    
    public class EvenOddSudokuData : ISudokuData
    {
    }

    public class SkyscraperSudokuData : ISudokuData
    {
        public List<int> clues_l { get; set; } = new List<int>();
        public List<int> clues_r { get; set; } = new List<int>();
        public List<int> clues_t { get; set; } = new List<int>();
        public List<int> clues_b { get; set; } = new List<int>();
    }

    public class KropkiSudokuData : ISudokuData
    {
        public List<List<int>> horizontal_clues { get; set; } = new List<List<int>>();
        public List<List<int>> vertical_clues { get; set; } = new List<List<int>>();
    }

    public class ThermoSudokuData : ISudokuData
    {
        public List<List<int>> thermos { get; set; } = new List<List<int>>();
    }

    public class JigsawSudokuData : ISudokuData
    {
        public List<int> boxes { get; set; } = new List<int>();
    }
}