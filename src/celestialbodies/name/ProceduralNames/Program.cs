using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProceduralNames
{
    class Program
    {
        static void Main(string[] args)
        {
            ProceduralNameGenerator names = new ProceduralNameGenerator("ukrainian_names.txt");

            for (int i = 0; i < 16; i++)
            {
                string word = names.GenerateRandomWord(7);
                Console.WriteLine(word);
            }
        }
    }
}
