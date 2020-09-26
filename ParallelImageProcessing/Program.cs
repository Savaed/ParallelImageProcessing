using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ParallelImageProcessing
{
    internal class Program
    {
        private static string _appName;

        internal static void Main(string[] args)
        {
            var hostBuilder = new HostBuilder().ConfigureHostConfiguration(configHost =>
            {
                configHost.AddEnvironmentVariables();
                configHost.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((hostingContext, cfg) =>
            {
                _appName = hostingContext.HostingEnvironment.ApplicationName;
            })
            .Build();


            try
            {
                var parameters = GetProgramParameters(args);
                PrintHeader(parameters);
                string imagePath = parameters.Item1;
                int kernelSize = parameters.Item2;
                int parallelOperationTimeout = parameters.Item3;

                var manager = new ImageManager();
                var originalImage = manager.Open(imagePath);

                var watch = new Stopwatch();

                // Synchronous GetSet method.
                Run(manager, originalImage, watch, false, kernelSize);

                // Synchronous LockBits method.
                Run(manager, originalImage, watch, true, kernelSize);

                var cts = new CancellationTokenSource();
                cts.CancelAfter(parallelOperationTimeout);

                for (int i = 0; i < Environment.ProcessorCount + 3; i++)
                {
                    RunParallel(manager, originalImage, watch, i + 1, cts.Token, kernelSize);
                }

                Logger.Beep();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }


        #region Privates        

        private static void PrintHeader(Tuple<string, int, int> parameters)
        {
            Console.WriteLine(" _____                                                                    _");
            Console.WriteLine("|_   _|                                                                  (_)");
            Console.WriteLine("  | |  _ __ ___   __ _  __ _  ___     _ __  _ __ ___   ___ ___  ___ ___ _ _ __   __ _");
            Console.WriteLine(@"  | | | '_ ` _ \ / _` |/ _` |/ _ \   | '_ \| '__/ _ \ / __/ _ \/ __/ __| | '_ \ / _` |");
            Console.WriteLine(@" _| |_| | | | | | (_| | (_| |  __/   | |_) | | | (_) | (_|  __/\__ \__ \ | | | | (_| |");
            Console.WriteLine(@"|_____|_| |_| |_|\__,_|\__, |\___|   | .__/|_|  \___/ \___\___||___/___/_|_| |_|\__, |");
            Console.WriteLine("                        __/ |        | |                                         __/ |");
            Console.WriteLine("                       |___/         |_|                                        |___/ ");
            Console.WriteLine("\nVersion: 1.1");
            Console.WriteLine("Release date: 26.09.2020\n");

            PrintAppParameters(parameters);

            Console.WriteLine(string.Format("{0, -7} {1, -11} {2, -10} {3, -6} {4, -7} {5, -10} {6, -10}", "\nSTATUS", "PROCESS", "PARALLEL", "TASKS", "KERNEL", "TIME[ms]", "DESCRIPTION"));
            Console.WriteLine("--------------------------------------------------------------------------------------");
        }

        private static Tuple<string, int, int> GetProgramParameters(string[] args)
        {
            string strKernelSize;
            string strTimeout;
            string imagePath;

            if (args[0].Equals(_appName))
            {
                imagePath = args[1];
                strKernelSize = args[2];
                strTimeout = args[3];
            }
            else
            {
                imagePath = args[0];
                strKernelSize = args[1];
                strTimeout = args[2];
            }

            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("Incorrect image path passed.");

            if (!int.TryParse(strKernelSize, out int kernelSize) || kernelSize < 3 || kernelSize % 2 == 0)
                throw new ArgumentException("Incorrect kernel size passed.");

            if (!int.TryParse(strTimeout, out int parallelOperationTimeout) || parallelOperationTimeout < 1)
                throw new ArgumentException("Incorrect timeout passed.");

            return new Tuple<string, int, int>(imagePath, kernelSize, parallelOperationTimeout);
        }

        private static void PrintAppParameters(Tuple<string, int, int> parameters)
        {
            Console.WriteLine($"Image path: {parameters.Item1}");
            Console.WriteLine($"Kernel size: {parameters.Item2}");
            Console.WriteLine($"Timeout: {parameters.Item3} ms");
        }

        private static void Run(ImageManager manager, Bitmap originalImage, Stopwatch watch, bool useLockBits = true, int kernelSize = 3, float contrast = 0.5f)
        {
            string usedMethod = useLockBits ? "LockBits method" : "Get/Set method";

            watch.Restart();
            var blurImage = manager.Blur(originalImage, kernelSize, useLockBits);
            watch.Stop();
            Logger.Success("Blur", watch.ElapsedMilliseconds, $"{usedMethod}", false, 1, kernelSize);
            Logger.LogToFile("Blur", false, 1, kernelSize, watch.ElapsedMilliseconds, usedMethod);

            watch.Restart();
            var grayscaleImage = manager.Grayscale(originalImage, useLockBits);
            watch.Stop();
            Logger.Success("Grayscale", watch.ElapsedMilliseconds, $"{usedMethod}", false, 1);
            Logger.LogToFile("Grayscale", false, 1, 0, watch.ElapsedMilliseconds, usedMethod);

            watch.Restart();
            var medianImage = manager.MedianFilter(originalImage, kernelSize, useLockBits);
            watch.Stop();
            Logger.Success("Median", watch.ElapsedMilliseconds, $"{usedMethod}", false, 1, kernelSize);
            Logger.LogToFile("Median", false, 1, kernelSize, watch.ElapsedMilliseconds, usedMethod);

            watch.Restart();
            var invertImage = manager.Invert(originalImage, useLockBits);
            watch.Stop();
            Logger.Success("Invert", watch.ElapsedMilliseconds, $"{usedMethod}", false, 1);
            Logger.LogToFile("Invert", false, 1, 0, watch.ElapsedMilliseconds, usedMethod);

            watch.Restart();
            var contrastImage = manager.Contrast(originalImage, contrast);
            watch.Stop();
            Logger.Success("Contrast", watch.ElapsedMilliseconds, $"{usedMethod}. Contrast: {contrast}", false, 1, kernelSize);
            Logger.LogToFile("Contrast", false, 1, 0, watch.ElapsedMilliseconds, usedMethod);

            manager.Save(blurImage, "blur.jpg");
            manager.Save(grayscaleImage, "grayscale.jpg");
            manager.Save(medianImage, "median.jpg");
            manager.Save(invertImage, "invert.jpg");
            manager.Save(contrastImage, "contrast.jpg");
        }

        private static void RunParallel(ImageManager manager, Bitmap originalImage, Stopwatch watch, int tasksNumber, CancellationToken cancellationToken, int kernelSize = 3)
        {
            try
            {
                watch.Restart();
                var blurImageParallel = manager.BlurParallel(originalImage, kernelSize, tasksNumber, cancellationToken);
                watch.Stop();
                Logger.Success("Blur", watch.ElapsedMilliseconds, "LockBits method", true, tasksNumber, kernelSize);
                Logger.LogToFile("Blur", true, tasksNumber, kernelSize, watch.ElapsedMilliseconds, "LockBits method");

                watch.Restart();
                var grayscaleImageParallel = manager.GrayscaleParallel(originalImage, tasksNumber, cancellationToken);
                watch.Stop();
                Logger.Success("Grayscale", watch.ElapsedMilliseconds, "LockBits method", true, tasksNumber);
                Logger.LogToFile("Grayscale", true, tasksNumber, 0, watch.ElapsedMilliseconds, "LockBits method");

                watch.Restart();
                var medianImageParallel = manager.MedianFilterParallel(originalImage, kernelSize, tasksNumber, cancellationToken);
                watch.Stop();
                Logger.Success("Median", watch.ElapsedMilliseconds, "LockBits method", true, tasksNumber, kernelSize);
                Logger.LogToFile("Median", true, tasksNumber, kernelSize, watch.ElapsedMilliseconds, "LockBits method");

                watch.Restart();
                var invertImageParallel = manager.InvertParallel(originalImage, tasksNumber, cancellationToken);
                watch.Stop();
                Logger.Success("Invert", watch.ElapsedMilliseconds, "LockBits method", true, tasksNumber);
                Logger.LogToFile("Invert", true, tasksNumber, 0, watch.ElapsedMilliseconds, "LockBits method");

                manager.Save(grayscaleImageParallel, "grayscale-parallel.jpg");
                manager.Save(blurImageParallel, "blur-parallel.jpg");
                manager.Save(medianImageParallel, "median-parallel.jpg");
                manager.Save(invertImageParallel, "invert-parallel.jpg");
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

    }
}
