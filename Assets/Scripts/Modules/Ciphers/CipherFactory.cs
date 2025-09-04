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
                default:
                    return null;
            }
        }
    }
}