using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.IO;
using CsvHelper;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace KNreader
{
    class Program
    {
        public static readonly int maxThreads = 10;

        public static string suffix = ".csv";
        public static string logSuffix = ".log";
        public static string resultFolder = "results";
        public static string resultFileNameSuffix = "_GPS";

        private static readonly HttpClient httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            string path = "";
            var files = new List<string>();

            if (args.Length != 1)
            {
                Console.WriteLine("Wrong number of arguments. Supply path to direcotry with CSV files or path to one specific CSV file.");
                Console.Write("Press any key to exit");
                Console.ReadKey();
                return;
            }

            path = args[0];
            var filename = Path.GetFileName(path);
            if (filename?.Length > 0)
            {
                path = path.TrimEnd(filename);
                files.Add(filename.TrimEnd(suffix));
            }
            else
            {
                files = Directory.GetFiles(path, $"*{suffix}", SearchOption.TopDirectoryOnly).Select(f => Path.GetFileName(f).TrimEnd(suffix)).ToList();
            }

            foreach (var file in files)
            {
                // process each file in series (every entry in each file will run in parallel)
                Console.WriteLine($"{files.IndexOf(file)+1}/{files.Count()}, {file}{suffix}");
                var rows = await ProcessFile(path, file);
            }

            Console.Write("\nPress any key to exit...");
            Console.ReadKey();
        }

        public static async Task<int> ProcessFile(string path, string file)
        {
            var recordsWithGps = new List<Pozemek>();
            var linesWithErrors = "";
            int count = 1;
            int counter = 0;

            using (var logWriter = new StreamWriter(Path.Combine(path, $"{file}{logSuffix}")))
            using (var reader = new StreamReader(Path.Combine(path, $"{file}{suffix}")))
            using (var csvReader = new CsvReader(reader))
            {
                logWriter.AutoFlush = true;
                csvReader.Configuration.Delimiter = ",";
                var records = csvReader.GetRecords<Pozemek>().ToList();
                count = records.Count;
                var q = new ConcurrentQueue<Pozemek>(records);

                Console.Write($" # Getting info on {count} items\t");

                using (var progressBar = new ProgressBar())
                {
                    var tasks = new List<Task>();
                    for (int n = 0; n < maxThreads; n++)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            while (q.TryDequeue(out var record))
                            {
                                try
                                {
                                    var pozemek = await KNReader.GetPoints(httpClient, record);
                                    recordsWithGps.Add(pozemek);
                                }
                                catch (Exception e)
                                {
                                    var log = $"{record.Ku}, {record.Parcela}";
                                    logWriter.WriteLine(log);
                                    linesWithErrors += log + "\n";
                                }
                                finally
                                {
                                    Interlocked.Increment(ref counter);
                                    progressBar.Report((double)counter / count);
                                }
                            }
                        }));
                    }
                    await Task.WhenAll(tasks);
                }

                Console.WriteLine($"\n # Successfully processed {recordsWithGps.Count()} entries from {count}");
                if (linesWithErrors.Length > 0)
                {
                    Console.WriteLine(" # Following lines from CSV were not completed:");
                    Console.WriteLine(linesWithErrors);
                }
            }

            EnsureDirectoryExists(Path.Combine(path, resultFolder, $"{file}{resultFileNameSuffix}{suffix}"));

            using (var writer = new StreamWriter(Path.Combine(path, resultFolder, $"{file}{resultFileNameSuffix}{suffix}")))
            using (var csvWriter = new CsvWriter(writer))
            {
                csvWriter.Configuration.Delimiter = ",";
                csvWriter.WriteRecords(recordsWithGps);
            }

            return recordsWithGps.Count();
        }

        

        private static void EnsureDirectoryExists(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (!fi.Directory.Exists)
            {
                System.IO.Directory.CreateDirectory(fi.DirectoryName);
            }
        }
    }
}
