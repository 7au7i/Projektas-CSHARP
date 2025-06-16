using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AgentA
{
    class Program
    {
        static void Main(string[] args)
        {
            string directoryPath;

            if (args.Length == 0)
            {
                Console.WriteLine("Nenurodytas katalogo kelias. Iveskite pilna kelia:");
                directoryPath = Console.ReadLine();
            }
            else
            {
                directoryPath = args[0];
            }

            directoryPath = directoryPath.Trim('"');

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("Nurodytas katalogas neegzistuoja.");
                return;
            }

            string[] files = Directory.GetFiles(directoryPath, "*.txt");

            if (files.Length == 0)
            {
                Console.WriteLine("Kataloge nerasta .txt failu.");
                return;
            }

            foreach (string file in files)
            {
                var wordCount = CountWordsInFile(file);
                Console.WriteLine($"Rezultatai {Path.GetFileName(file)}:");

                foreach (var kvp in wordCount)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
            }

            Console.WriteLine("Failu skanavimas baigtas.");
        }

        static Dictionary<string, int> CountWordsInFile(string filePath)
        {
            var wordCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string content = File.ReadAllText(filePath);
            var words = Regex.Matches(content, @"\b\w+\b");

            foreach (Match match in words)
            {
                string word = match.Value;

                if (wordCount.ContainsKey(word))
                    wordCount[word]++;
                else
                    wordCount[word] = 1;
            }

            return wordCount;
        }
    }
}
