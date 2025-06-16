using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Master
{
    class Program
    {
        static ConcurrentDictionary<string, ConcurrentDictionary<string, int>> globalIndex = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();

        static void Main(string[] args)
        {
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 0);

            if (args.Length < 2)
            {
                Console.WriteLine("Iveskite pipe pavadinimus:");
                Console.Write("AgentA pipe: ");
                string pipeA = Console.ReadLine() ?? "";
                Console.Write("AgentB pipe: ");
                string pipeB = Console.ReadLine() ?? "";

                args = new string[] { pipeA, pipeB };
            }

            Task t1 = Task.Run(() => StartPipeServer(args[0]));
            Task t2 = Task.Run(() => StartPipeServer(args[1]));

            Task.WaitAll(t1, t2);

            Console.WriteLine("Galutinis rezultatas:");
            PrintResults();
        }

        static void StartPipeServer(string pipeName)
        {
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In))
            {
                Console.WriteLine($"Laukiama kliento pipe \"{pipeName}\"...");
                pipeServer.WaitForConnection();
                Console.WriteLine($"Klientas prisijunge prie pipe \"{pipeName}\"");

                using (StreamReader reader = new StreamReader(pipeServer, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ProcessLine(line);
                    }
                }
            }
        }

        static void ProcessLine(string line)
        {
            try
            {
                string[] parts = line.Split(':');
                if (parts.Length != 3)
                    return;

                string fileName = parts[0];
                string word = parts[1];
                int count = int.Parse(parts[2]);

                var fileDict = globalIndex.GetOrAdd(fileName, new ConcurrentDictionary<string, int>());
                fileDict.AddOrUpdate(word, count, (key, oldValue) => oldValue + count);
            }
            catch { }
        }

        static void PrintResults()
        {
            foreach (var fileEntry in globalIndex)
            {
                string fileName = fileEntry.Key;
                foreach (var wordEntry in fileEntry.Value)
                {
                    Console.WriteLine($"{fileName}:{wordEntry.Key}:{wordEntry.Value}");
                }
            }
        }
    }
}
