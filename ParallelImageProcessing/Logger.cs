using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ParallelImageProcessing
{
    public static class Logger
    {
        public static void Success(string process, long elapsedTime, string description, bool parallel = false, int tasksNumber = 1, int kernelSize = 0, ConsoleColor color = ConsoleColor.DarkGreen)
        {
            Write("[Ok]", process, elapsedTime, description, parallel, tasksNumber, kernelSize, color);
        }

        public static void Info(string process, long elapsedTime, string description, bool parallel = false, int tasksNumber = 1, int kernelSize = 0, ConsoleColor color = ConsoleColor.DarkGreen)
        {
            Write("[Ok]", process, elapsedTime, description, parallel, tasksNumber, kernelSize, color);
        }

        public static void Warning(string message, ConsoleColor color = ConsoleColor.Yellow)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[Warn]  {message}");
            Console.ResetColor();
        }

        public static void Error(string message, ConsoleColor color = ConsoleColor.Red)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"[Error] {message}");
            Console.ResetColor();
        }

        public static void LogToFile(string process, bool parallel, int tasksNumber, int kernelSize, long elapsedTime, string description)
        {
            const string HeaderPattern = "PROCESS;PARALLEL;TASKS;KERNEL;TIME[ms];DESCRIPTION";
            string kernel = kernelSize > 0 ? $"{kernelSize.ToString()}x{kernelSize.ToString()}" : "-";
            string isParallel = parallel ? "yes" : "no";
            string textLine = $"{process};{isParallel};{tasksNumber};{kernel};{elapsedTime};{description}";

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "statistics.csv");

            try
            {
                if (File.Exists(path))
                {
                    IEnumerable<string> lines = File.ReadLines(path);
                    string header = lines.Count() > 0 ? lines.First() : "";

                    if (!header.Equals(HeaderPattern))
                        textLine = $"{HeaderPattern}\n{textLine}";
                }
                else
                    textLine = $"{HeaderPattern}\n{textLine}";

                using (var file = new StreamWriter(path, true))
                {
                    file.WriteLine(textLine);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See details: {ex.Message}");
                throw;
            }
        }

        public static void Beep(int frequency = 700, int duration = 500, int beepNumbers = 1)
        {
            for (int i = 0; i < beepNumbers; i++)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    Console.Beep(frequency, duration);
                else
                    Console.Beep();
            }
        }


        #region Privates

        private static void Write(string status, string process, long miliseconds, string description, bool parallel = false, int tasksNumber = 1, int kernelSize = 0, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(string.Format("{0, -7} {1, -11} {2, -10} {3, -6} {4, -7} {5, -10} {6, -10}",
                              status,
                              process,
                              parallel ? "Yes" : "No",
                              tasksNumber,
                              kernelSize > 0 ? $"{kernelSize.ToString()}x{kernelSize.ToString()}" : " -",
                              miliseconds,
                              description));
            Console.ResetColor();
        }

        #endregion

    }
}
