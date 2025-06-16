using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;

namespace AgentA
{
    class Program
    {
        static BlockingCollection<string> eile = new BlockingCollection<string>();

        static void Main(string[] args)
        {
            Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)(1 << 1);

            string katalogoKelias;
            string pipePavadinimas;

            if (args.Length < 2)
            {
                Console.WriteLine("Iveskite katalogo kelia:");
                katalogoKelias = Console.ReadLine() ?? "";

                Console.WriteLine("Iveskite pipe pavadinima:");
                pipePavadinimas = Console.ReadLine() ?? "";
            }
            else
            {
                katalogoKelias = args[0];
                pipePavadinimas = args[1];
            }

            katalogoKelias = katalogoKelias.Trim('"');

            if (!Directory.Exists(katalogoKelias))
            {
                Console.WriteLine("Nurodytas katalogas neegzistuoja.");
                return;
            }

            Thread skaitytojoGija = new Thread(() => SkaitytiFailus(katalogoKelias));
            Thread siuntejoGija = new Thread(() => SiustiDuomenis(pipePavadinimas));

            skaitytojoGija.Start();
            siuntejoGija.Start();

            skaitytojoGija.Join();
            eile.CompleteAdding();
            siuntejoGija.Join();

            Console.WriteLine("AgentA darbas baigtas.");
        }

        static void SkaitytiFailus(string katalogoKelias)
        {
            string[] failai = Directory.GetFiles(katalogoKelias, "*.txt");

            foreach (string failas in failai)
            {
                var zodziuSkaicius = SkaiciuotiZodzius(failas);
                string failoPavadinimas = Path.GetFileName(failas);

                foreach (var elementas in zodziuSkaicius)
                {
                    string eilute = $"{failoPavadinimas}:{elementas.Key}:{elementas.Value}";
                    eile.Add(eilute);
                }
            }
        }

        static void SiustiDuomenis(string pipePavadinimas)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipePavadinimas, PipeDirection.Out))
            {
                Console.WriteLine("Jungiamasi prie master...");
                pipeClient.Connect();
                Console.WriteLine("Prisijungta prie master.");

                using (StreamWriter writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true })
                {
                    foreach (var eilute in eile.GetConsumingEnumerable())
                    {
                        writer.WriteLine(eilute);
                    }
                }
            }
        }

        static Dictionary<string, int> SkaiciuotiZodzius(string failoKelias)
        {
            var zodziuSkaicius = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string turinys = File.ReadAllText(failoKelias);
            var zodziai = Regex.Matches(turinys, @"\b\w+\b");

            foreach (Match zodis in zodziai)
            {
                string tekstas = zodis.Value;
                if (zodziuSkaicius.ContainsKey(tekstas))
                    zodziuSkaicius[tekstas]++;
                else
                    zodziuSkaicius[tekstas] = 1;
            }

            return zodziuSkaicius;
        }
    }
}
