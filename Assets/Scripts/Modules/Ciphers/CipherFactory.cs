using System;
using System.Collections.Generic;
using Modules.Ciphers;
using UnityEngine;

namespace KModkit.Ciphers
{
    public static class CipherFactory
    {
        public static Cipher CreateCipher(string cipherType, TextAsset sudokuData = null)
        {
            switch (cipherType)
            {
                case "Regular":
                    return new RegularCipher(sudokuData);
                default:
                    return null;
            }
        }
        
        public static Cipher CreateCipher(string cipherType, ISudokuData sudokuData)
        {
            switch (cipherType)
            {
                case "Skyscraper":
                    return new SkyscraperCipher(sudokuData as SkyscraperSudokuData);
                case "AntiKnight":
                    return new AntiKnightCipher(sudokuData as AntiKnightSudokuData);
                case "Thermo":
                    return new ThermoCipher(sudokuData as ThermoSudokuData);
                case "Jigsaw":
                    return new JigsawCipher(sudokuData as JigsawSudokuData);
                case "Little Killer":
                    return new LittleKillerCipher(sudokuData as LittleKillerSudokuData);
                case "Killer":
                    return new KillerCipher(sudokuData as KillerSudokuData);
                case "Even Odd":
                    return new EvenOddCipher(sudokuData as EvenOddSudokuData);
                case "Palindrome":
                    return new PalindromeCipher(sudokuData as PalindromeSudokuData);
                case "Sandwich":
                    return new SandwichCipher(sudokuData as SandwichSudokuData);
                case "Kropki":
                    return new KropkiCipher(sudokuData as KropkiSudokuData);
                case "Regular":
                    return new RegularCipher(sudokuData as RegularSudokuData);
                default:
                    return null;
            }
        }
    }
}