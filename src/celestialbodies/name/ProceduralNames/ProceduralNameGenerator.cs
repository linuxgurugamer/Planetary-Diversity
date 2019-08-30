/*
The MIT License (MIT)

Copyright (c) 2015 Dave Carlile

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

namespace PlanetaryDiversity.CelestialBodies.Name
{
    class Character
    {
        public char Letter;
        public int Instances;
        public float Probability;
    }


    public class ProceduralNameGenerator
    {
        int order;
        System.Random random;
        Dictionary<string, List<Character>> chains = new Dictionary<string, List<Character>>();
        HashSet<string> wordPatterns = new HashSet<string>();
        public bool initialized = false;

        public ProceduralNameGenerator(string[] words, int order = 2, System.Random random = null)
        {
            Initialize(words, order, random);
        }


        public ProceduralNameGenerator(string filename, int order = 2, System.Random random = null)
        {
            string inputfile = filename;
            if (!File.Exists(inputfile))
            {
                Debug.Log("File missing: " + filename);
                return;
            }

            string[] words = File.ReadAllLines(inputfile);
            Initialize(words, order, random);
            initialized = true;
        }


        void Initialize(string[] words, int order, System.Random random)
        {
            this.random = random == null ? new System.Random() : random;
            AnalyzeWords(words, order);
        }


        bool IsVowel(char ch)
        {
            return "aeiou".Contains(ch) || "àáâãäèéêëìíîïòóôõöùúûü".Contains(ch);
        }


        string GetWordPattern(string word)
        {
            string pattern = "";

            foreach (char ch in word)
            {
                pattern += IsVowel(ch) ? "v" : "c";
            }

            return pattern;
        }


        void IdentifyWordPattern(string word)
        {
            wordPatterns.Add(GetWordPattern(word));
        }


        void AddCharacter(string key, char ch)
        {
            List<Character> chain;
            if (!chains.TryGetValue(key, out chain))
            {
                chain = new List<Character>();
                chains.Add(key, chain);
            }


            Character letter = chain.Find(l => l.Letter == ch);
            if (letter == null)
            {
                letter = new Character { Letter = ch };
                chain.Add(letter);
            }

            letter.Instances++;
        }


        void ProcessWord(string word)
        {
            word = new String('_', order) + word + "_";

            for (int i = 0; i < word.Length - order; i++)
            {
                string key = word.Substring(i, order);
                AddCharacter(key, word[i + order]);
            }
        }


        void AnalyzeWord(string word)
        {
            IdentifyWordPattern(word);
            ProcessWord(word);
        }


        void CalculateProbability()
        {
            Dictionary<string, List<Character>> newChains = new Dictionary<string, List<Character>>();

            foreach (KeyValuePair<string, List<Character>> pair in chains)
            {
                float totalInstances = pair.Value.Sum(l => l.Instances);
                pair.Value.ForEach(l => l.Probability = l.Instances / totalInstances);
                newChains[pair.Key] = pair.Value.OrderBy(l => l.Probability).ToList();
            }

            chains.Clear();
            chains = newChains;
        }


        void AnalyzeWords(string[] words, int order)
        {
            this.order = order;

            chains.Clear();
            wordPatterns.Clear();

            foreach (string word in words)
            {
                AnalyzeWord(word.ToLower());
            }

            CalculateProbability();
        }


        Character GetCharacterByProbability(string key, double probability)
        {
            List<Character> chain;
            if (!chains.TryGetValue(key, out chain)) return null;

            float cumulative = 0;
            Character result = null;

            for (int i = 0; i < chain.Count; i++)
            {
                cumulative += chain[i].Probability;
                if (probability < cumulative)
                {
                    result = chain[i];
                    break;
                }
            }

            return result;
        }


        string GenerateRandomWord(int minLength, int maxLength = 16)
        {
            string result;

            result = "";
            string key = new String('_', order);

            while (result.Length < maxLength)
            {
                Character character = GetCharacterByProbability(key, random.NextDouble());
                char ch = character == null ? '_' : character.Letter;
                if (ch == '_') break;

                result += ch;
                key += ch;

                key = key.Substring(key.Length - order);
            }

            return result.Substring(0, 1).ToUpper() + result.Substring(1);
        }


        public string GenerateRandomWord(int minLength = 5, int maxLength = 16, bool matchWordPattern = true)
        {
            for (int i = 0; i < 64; i++)
            {
                string word = GenerateRandomWord(minLength, maxLength);
                if (word.Length < minLength) continue;
                if (!matchWordPattern) return word;
                if (wordPatterns.Contains(GetWordPattern(word))) return word;
            }

            return "";
        }
    }
}