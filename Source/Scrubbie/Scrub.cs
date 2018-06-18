﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Scrubbie
{
    public class Scrub
    {
        const int DefaultRegxCacheSize = 16;

        public Dictionary<string, string> StringTransDict { private set; get; }
        public List<(string, string)> RegxTuples { private set; get; }
        public Dictionary<char, char> CharTransDict { private set; get; }
        private string _translatedStr;

        public int CacheSize
        {
            set
            {
                Regex.CacheSize = value < 0 ? DefaultRegxCacheSize : value;
            }

            get => Regex.CacheSize;
        }

        /// <summary>
        /// Constructor for the Scrubbies class. It set up default state
        /// for any needed variable and the initial string for which we want to scrub.
        /// </summary>
        /// <param name="origString">A string with each character to map</param>
        public Scrub(string origString)
        {
            _translatedStr = origString;

            StringTransDict = new Dictionary<string, string>();
            RegxTuples = new List<(string, string)>();
            CharTransDict = new Dictionary<char, char>();

            // set the default regx compiled cache size
        }

        /// <summary>
        /// Set the string translation up. Basically accepts a dictionary and a case flag for
        /// comparison. If the incoming dictionary is null, and empty one will be created.
        /// </summary>
        /// <param name="translateMap">Dictionay of words the map to each other</param>
        /// <param name="ignoreCase">True ignore case on dictionary match, False (default)
        /// case sensitive match</param>
        public void SetStringTranslator(Dictionary<string, string> translateMap = null, bool ignoreCase = false)
        {
            // set up the comparer for the dictionary

            StringComparer comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            StringTransDict = translateMap == null ? new Dictionary<string, string>(comparer) : new Dictionary<string, string>(translateMap, comparer);
        }

        /// <summary>
        /// Build a character translation dict. This works for any chars that are
        /// representable as a char in a string. The one to one mapping of the chars
        /// will effect a translation of chars from the inputMap to the matching offset
        /// char in the outputMap. The internal dictionary of this map is created as a
        /// result of these 2 strings. This can only replace 1 char with 1 char.
        /// </summary>
        /// <param name="inputMap">A string with each character to map</param>
        /// <param name="outputMap">The Output Character as a result </param>
        /// <exception cref="ArgumentException">If both strings are not the same length</exception>
        public void SetCharTranslator(string inputMap, string outputMap)
        {
            if (inputMap.Length != outputMap.Length)
                throw new ArgumentException("Invalid Length of Parameter Strings, they must be equal length");

            for (int i = 0; i < inputMap.Length; i++)
                CharTransDict[inputMap[i]] = outputMap[i];
        }

        /// <summary>
        /// Sets a character translation dict. This works for any chars that are
        /// representable as a char in a string. The one to one mapping of the chars
        /// will effect a translation of chars from the inputMap to the matching offset
        /// char in the outputMap. Use the string based Set for easy mapping of large
        /// amounts of characters.
        /// </summary>
        /// <param name="translateMap">A Dictionary with char to char mapping</param>
        public void SetCharTranslator(Dictionary<char, char> translateMap = null)
        {
            CharTransDict = translateMap == null ? new Dictionary<char, char >() : new Dictionary<char, char>(translateMap);
        }

        /// <summary>
        /// Sets up the List of regx match and replace list. The Item1 must be the Regx
        /// that will be match (C# style) and the Item2 element will be what's replaced.
        /// If the passed in list is null will create an empty list
        /// </summary>
        /// <param name="regxTuplesList">List of regx and replacement strings</param>
        public void SetRegxTranslator(List<(string, string)> regxTuplesList = null)
        {
            if (regxTuplesList == null)
            {
                // create empty list

                RegxTuples = new List<(string, string)>();
            }
            else
            {
                // copy the existing list

                RegxTuples = new List<(string, string)>(regxTuplesList);
            }
        }

        /// <summary>
        /// Translates given string based on the the characters in the dictionary. If character is
        /// not in the dictionay, it is pass thru untouched. Size of string is not changed.
        /// </summary>
        /// <returns>Scrubbies</returns>
        public Scrub MapChars()
        {
            // create a new stringbuild of the same size
            char[] chars = _translatedStr.ToCharArray();

            for (int i = 0; i < _translatedStr.Length; i++)
            {
                if (CharTransDict.ContainsKey(chars[i]))
                    chars[i] = CharTransDict[chars[i]];
            }

            _translatedStr = new string(chars);

            return this;
        }

        /// <summary>
        /// Does a regx strip on the working sting. The passed in
        /// expression is a C# Regx style pattern match. This is designed
        /// to be more of an on-the-fly regx. Will Regex will compile and cache for
        /// static calls like this.
        /// </summary>
        /// <param name="matchRegx"></param>
        /// <returns>Scrubbies</returns>
        public Scrub Strip(string matchRegx)
        {
            // Call static replace method, strip and save

            _translatedStr = Regex.Replace(_translatedStr, matchRegx, String.Empty);

            return this;
        }

        /// <summary>
        /// Translates given string based on the dictionary. If character is
        /// not in the dictionay, it is pass thru untouched. Internally
        /// used helper.
        /// </summary>
        /// <param name="origStr"></param>
        /// <returns>string</returns>
        private string Map(string origStr)
        {
            if (string.IsNullOrEmpty(origStr))
            {
                return string.Empty;
            }

            // simple in this case, just get the value from the map

            return StringTransDict.ContainsKey(origStr) ? StringTransDict[origStr] : origStr;
        }

        /// <summary>
        /// Translates a phrase that has each word separated by the 'pattern' string.
        /// Will swap the words in the dictionary if it matches, otherwise just pass
        /// it as it was if no match. This adhears to the matching rules set when
        /// the dictionary was created. Generally the active string should be
        /// clean and have sane word seperators like single space, comma, etc.
        /// This map will process one time against each word, and once a word is translated
        /// it will be be a candidate for further translation.
        /// </summary>
        /// <param name="splitString">Will split the string on this string</param>
        /// <returns>Scrubbies</returns>
        public Scrub MapWords(string splitString = " ")
        {
            if (String.IsNullOrEmpty(_translatedStr) || String.IsNullOrEmpty(splitString))
            {
                _translatedStr = string.Empty;
                return this;
            }

            // Convert to an array of strings which split can use

            string[] patternArray = { splitString };
            string[] elements = _translatedStr.Split(patternArray, StringSplitOptions.None);

            // rebuild string, adding back in each mapped word and split seperator

            StringBuilder sb = new StringBuilder();

            foreach (string element in elements)
            {
                sb.Append(Map(element));
                sb.Append(splitString);
            }

            string cleanStr = sb.ToString();

            // check for empty, bounce since nothing to return

            if (cleanStr.Length == 0)
            {
                _translatedStr = string.Empty;
                return this;
            }

            // remove trailing splitString from end of string.

            _translatedStr = cleanStr.Substring(0, cleanStr.Length - splitString.Length);

            return this;
        }

        /// <summary>
        /// This will allow a list of regx's and match replacements (string, string) tuple
        /// to form a regx pattern and replacement string. Item1 is the pattern, Item2 is the
        /// replacement string on any matches. This is similar to the MapWords() but
        /// based on regx's AND at each new regx match pattern it will be reapplied to any
        /// previously applied matches that may have been replaced.
        /// </summary>
        /// <returns>Scrubbies</returns>
        public Scrub RegxTranslate()
        {
            // for each regx replace in the tuple list do a regx replace
            // with the regx as Item1 and the replace as Item2

            foreach ((string, string) regxTuple in RegxTuples)
            {
                // static will compile and cache the regx for each one

                _translatedStr = Regex.Replace(_translatedStr, regxTuple.Item1, regxTuple.Item2);
            }

            return this;
        }

        /// <summary>
        /// Set the current working string
        /// </summary>
        /// <param name="workingStr"></param>
        /// <returns>Scrubbies</returns>
        public Scrub Set(string workingStr)
        {
            _translatedStr = workingStr;

            return this;
        }

        /// <summary>
        /// Return as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _translatedStr;
        }
    }
}
