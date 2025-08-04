using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KModkit
{
    public static class Logger
    {
        public static void Log<T>(this string message, Sudoku<T> sudoku) where T : ISudokuData
        {
            Debug.LogFormat("[{0} Sudoku #{1}] {2}", sudoku.sudokuColorName, sudoku.moduleId, message);
        }
    }
}