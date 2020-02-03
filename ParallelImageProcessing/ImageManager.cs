using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelImageProcessing
{
    public class ImageManager : IImageManager
    {
        private string _imagesFolderPath;


        /// <summary>
        /// Creates new representation of the <see cref="Bitmap"/> instance from the specified file.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>Instance of <see cref="Bitmap"/> class.</returns>
        /// <exception cref="ArgumentException"><paramref name="imagePath"/> is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Cannot find file with specified path.</exception>
        public Bitmap Open(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                throw new ArgumentException($"Argument {nameof(imagePath)} is null or empty.", nameof(imagePath));

            try
            {
                if (imagePath.Contains('\\'))
                    _imagesFolderPath = imagePath.Substring(0, imagePath.LastIndexOf('\\'));
                else
                    _imagesFolderPath = imagePath.Substring(0, imagePath.LastIndexOf('/'));

                return new Bitmap(imagePath);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error($"File cannot be find. See more details: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves instance of <see cref="Bitmap"/> class with specified name to file.
        /// </summary>
        /// <param name="image">Image to save.</param>
        /// <param name="imageName">Image name.</param>
        /// <returns>Absoulut path to saved image.</returns>
        public string Save(Bitmap image, string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                throw new ArgumentException($"Argument {nameof(imageName)} is null or empty.", nameof(imageName));

            if (image is null)
                throw new ArgumentNullException(nameof(image));

            try
            {
                string imagePath = $@"{_imagesFolderPath}\{imageName}";
                image.Save(imagePath);
                return imagePath;
            }
            catch (Exception ex)
            {
                Logger.Error($"Cannot save file properly. See more details: {ex.Message}");
                throw;
            }
        }


        #region Synchronous methods       

        /// <summary>
        /// Applies a grayscale filter to the image using LockBits or Get/SetPixel method.
        /// </summary>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "useLockBits"> Specifies which method should be used to manipulate pixels. If true, the LockBits method will be used, otherwise the Get/SetPixel method. </param>
        /// <returns> Processed bitmap image. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorect pixel format. </exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception>        
        public Bitmap Grayscale(Bitmap image, bool useLockBits = true)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            var bmp = new Bitmap(image);

            try
            {
                if (useLockBits)
                {
                    var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);

                    // Lock image for next multithreading processing.
                    var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                    byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];

                    // Copy locked image (pixels) into buffer for parallel access.
                    Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                    ProcessGrayscale(buffer, 0, data.Width, 0, data.Height, data.Width, colorDepth);

                    // Copy processed pixels back from buffer to image.
                    Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                    bmp.UnlockBits(data);
                }
                else
                    ProcessGrayscale(bmp, 0, bmp.Width, 0, bmp.Height);

                return bmp;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Applies a blur to the image using LockBits or Get/SetPixel method.
        /// </summary>
        /// <param name = "kernelSize"> The size of the kernel that will be used to blur the image. It should be an odd positive number less than or equal to the width and height of the image. </param>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "useLockBits"> Specifies which method should be used to manipulate pixels. If true, the LockBits method will be used, otherwise the Get/SetPixel method. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorrect pixel format or incorrect <paramref name="kernelSize"/>.</exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception> 
        public Bitmap Blur(Bitmap image, int kernelSize, bool useLockBits = true)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            // Kernel size must be: <= width & <= height & > 0 & be odd number.
            if (kernelSize < 1 || kernelSize > image.Width || kernelSize > image.Height || kernelSize % 2 != 1)
                throw new ArgumentException($"Argument {nameof(kernelSize)} must be greater than 1 and lesser or equal to image's width and height and must be odd number.", nameof(kernelSize));

            var bmp = new Bitmap(image);

            try
            {
                if (useLockBits)
                {
                    var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);

                    // Lock image for next multithreading processing.
                    var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                    byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];

                    // Copy locked image (pixels) into buffer for parallel access.
                    Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                    ProcessBlur(buffer, kernelSize, kernelSize / 2, data.Width - kernelSize / 2, kernelSize / 2, data.Height - kernelSize / 2, data.Width, colorDepth);

                    // Copy processed pixels back from buffer to image.
                    Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                    bmp.UnlockBits(data);
                }
                else
                    ProcessBlur(bmp, kernelSize);

                return bmp;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Applies a median filter to specified image using LockBits or Get/SetPixel method.
        /// </summary>
        /// <param name = "kernelSize"> The size of the kernel that will be used to blur the image. It should be an odd positive number less than or equal to the width and height of the image. </param>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "useLockBits"> Specifies which method should be used to manipulate pixels. If true, the LockBits method will be used, otherwise the Get/SetPixel method. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorrect pixel format or incorrect <paramref name="kernelSize"/>.</exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception> 
        public Bitmap MedianFilter(Bitmap image, int kernelSize, bool useLockBits = true)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            // Kernel size must be: <= width & <= height & > 0 & be odd number.
            if (kernelSize < 1 || kernelSize > image.Width || kernelSize > image.Height || kernelSize % 2 != 1)
                throw new ArgumentException($"Argument {nameof(kernelSize)} must be greater than 1 and lesser or equal to image's width and height and must be odd number.", nameof(kernelSize));

            var bmp = new Bitmap(image);

            try
            {
                if (useLockBits)
                {
                    var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);

                    // Lock image for next multithreading processing.
                    var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                    byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];
                    int startX = kernelSize / 2;
                    int endX = data.Width - kernelSize / 2;

                    // Copy locked image (pixels) into buffer for parallel access.
                    Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                    ProcessMedianFilter(buffer, kernelSize, startX, endX, kernelSize / 2, data.Height - kernelSize / 2, data.Width, colorDepth);

                    // Copy processed pixels back from buffer to image.
                    Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                    bmp.UnlockBits(data);
                }
                else
                    ProcessMedianFilter(bmp, kernelSize);

                return bmp;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Applies an invert to the image using LockBits or Get/SetPixel method.
        /// </summary>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "useLockBits"> Specifies which method should be used to manipulate pixels. If true, the LockBits method will be used, otherwise the Get/SetPixel method. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorrect pixel format.</exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception> 
        public Bitmap Invert(Bitmap image, bool useLockBits = true)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            var bmp = new Bitmap(image);

            try
            {
                if (useLockBits)
                {
                    var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);
                    // Lock image for next multithreading processing.
                    var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                    int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                    byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];

                    // Copy locked image (pixels) into buffer for parallel access.
                    Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                    ProcessInvert(buffer, 0, data.Width, 0, data.Height, data.Width, colorDepth);

                    // Copy processed pixels back from buffer to image.
                    Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                    bmp.UnlockBits(data);
                }
                else
                    ProcessInvert(bmp);

                return bmp;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Changes an image contrast.
        /// </summary>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name="contrast"> Value of image contrast. Should be inclusive between 0 and 1. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> <paramref name="contrast"/> is not floating point number inclusive between 0 and 1. </exception>
        public Bitmap Contrast(Bitmap image, float contrast)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            if (contrast < 0.0f || contrast > 1.0f)
                throw new ArgumentOutOfRangeException($"Image contrast must be floating point number inclusive between 0 and 1.", nameof(contrast));

            var bmp = new Bitmap(image);
            var graphics = Graphics.FromImage(bmp);
            var imageAtributes = new ImageAttributes();
            var colorMatrix = new ColorMatrix(
                new float[][]
                {
                    new float[] { contrast, 0.0f, 0.0f, 0.0f, 0.0f },
                    new float[] { 0.0f, contrast, 0.0f, 0.0f, 0.0f },
                    new float[] { 0.0f, 0.0f, contrast, 0.0f, 0.0f },
                    new float[] { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f },
                    new float[] { 0.001f, 0.001f, 0.001f, 0.0f, 1.0f },
                });

            imageAtributes.SetColorMatrix(colorMatrix);
            graphics.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, imageAtributes);

            return bmp;
        }

        #endregion


        #region Parallel methods

        /// <summary>
        /// Parallel applies a grayscale to the image.
        /// </summary>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "tasksNumber"> Number of tasks on which image processing will be performed in parallel. </param>
        /// <param name="cancellationToken"> Parallel operation cancellation token. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorrect pixel format.</exception>
        /// <exception cref="OperationCanceledException"> The operation has been cancelled. </exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception>
        public Bitmap GrayscaleParallel(Bitmap image, int tasksNumber, CancellationToken cancellationToken = default)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            if (tasksNumber > Environment.ProcessorCount)
                Logger.Warning($"Processor count is {Environment.ProcessorCount} but passed {tasksNumber}.");

            var bmp = new Bitmap(image);

            try
            {
                var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);

                // Lock image for next multithreading processing.
                var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];

                var options = new ParallelOptions { CancellationToken = cancellationToken };

                // Copy locked image (pixels) into buffer for parallel access.
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                Parallel.For(0, tasksNumber, options, (taskNumber) =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();

                    int yChunk = data.Height / tasksNumber;
                    int startY = taskNumber * yChunk;
                    int endY = (taskNumber * yChunk) + yChunk;

                    ProcessGrayscale(buffer, 0, data.Width, startY, endY, data.Width, colorDepth);
                });

                // Copy processed pixels back from buffer to image.
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);

                bmp.UnlockBits(data);

                return bmp;
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warning(ex.Message);
                throw;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Parallel applies a blur to the image.
        /// </summary>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "kernelSize"> The size of the kernel that will be used to blur the image. It should be an odd positive number less than or equal to the width and height of the image. </param>
        /// <param name = "tasksNumber"> Number of tasks on which image processing will be performed in parallel. </param>
        /// <param name="cancellationToken"> Parallel operation cancellation token. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorrect pixel format or incorrect <paramref name="kernelSize"/>.</exception>
        /// <exception cref="OperationCanceledException"> The operation has been cancelled. </exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception>
        public Bitmap BlurParallel(Bitmap image, int kernelSize, int tasksNumber, CancellationToken cancellationToken = default)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            if (kernelSize < 1 || kernelSize > image.Width || kernelSize > image.Height || kernelSize % 2 != 1)
                throw new ArgumentException($"Argument {nameof(kernelSize)} must be greater than 1 and lesser or equal to image's width and height and must be odd number.", nameof(kernelSize));

            if (tasksNumber > Environment.ProcessorCount)
                Logger.Warning($"Processor count is {Environment.ProcessorCount} but passed {tasksNumber}.");

            var bmp = new Bitmap(image);

            try
            {
                var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);

                // Lock image for next multithreading processing.
                var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];
                int startX = kernelSize / 2;
                int endX = data.Width - (kernelSize / 2);

                var options = new ParallelOptions { CancellationToken = cancellationToken };

                // Copy locked image (pixels) into buffer for parallel access.
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                Parallel.For(0, tasksNumber, options, (taskNumber) =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int yChunk = data.Height / tasksNumber;
                    int startY = (taskNumber * yChunk) + (kernelSize / 2);

                    // NOTE: For the last thread end of y-axis must be shorten by kernel size because initially 'startY' offset (kernelSize / 2).
                    int endY = taskNumber == tasksNumber - 1 ? startY + yChunk - kernelSize : startY + yChunk;

                    ProcessBlur(buffer, kernelSize, startX, endX, startY, endY, data.Width, colorDepth);
                });

                // Copy processed pixels back from buffer to image.
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);

                bmp.UnlockBits(data);
                return bmp;
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warning(ex.Message);
                throw;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Parallel applies a median filter to the image.
        /// </summary>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "kernelSize"> The size of the kernel that will be used to blur the image. It should be an odd positive number less than or equal to the width and height of the image. </param>
        /// <param name = "tasksNumber"> Number of tasks on which image processing will be performed in parallel. </param>
        /// <param name="cancellationToken"> Parallel operation cancellation token. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorrect pixel format or incorrect <paramref name="kernelSize"/>.</exception>
        /// <exception cref="OperationCanceledException"> The operation has been cancelled. </exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception>
        public Bitmap MedianFilterParallel(Bitmap image, int kernelSize, int tasksNumber, CancellationToken cancellationToken = default)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            if (kernelSize < 1 || kernelSize > image.Width || kernelSize > image.Height || kernelSize % 2 != 1)
                throw new ArgumentException($"Argument {nameof(kernelSize)} must be greater than 1 and lesser or equal to image's width and height and must be odd number.", nameof(kernelSize));

            if (tasksNumber > Environment.ProcessorCount)
                Logger.Warning($"Processor count is {Environment.ProcessorCount} but passed {tasksNumber}.");

            var bmp = new Bitmap(image);

            try
            {
                var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);

                // Lock image for next multithreading processing.
                var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];
                int startX = kernelSize / 2;
                int endX = data.Width - (kernelSize / 2);

                var options = new ParallelOptions { CancellationToken = cancellationToken };

                // Copy locked image (pixels) into buffer for parallel access.
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                Parallel.For(0, tasksNumber, options, (taskNumber) =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int yChunk = data.Height / tasksNumber;
                    int startY = (taskNumber * yChunk) + (kernelSize / 2);

                    // NOTE: For the last thread end of y-axis must be shorten by kernel size because initially 'startY' offset (kernelSize / 2).
                    int endY = taskNumber == tasksNumber - 1 ? startY + yChunk - kernelSize : startY + yChunk;

                    ProcessMedianFilter(buffer, kernelSize, startX, endX, startY, endY, data.Width, colorDepth);
                });

                // Copy processed pixels back from buffer to image.
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);

                bmp.UnlockBits(data);
                return bmp;
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warning(ex.Message);
                throw;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Parallel applies an invert to the image.
        /// </summary>
        /// <param name = "image"> Bitmap to process. </param>
        /// <param name = "tasksNumber"> Number of tasks on which image processing will be performed in parallel. </param>
        /// <param name="cancellationToken"> Parallel operation cancellation token. </param>
        /// <returns> Processed bitmap image. </returns>       
        /// <exception cref="ArgumentNullException"> <paramref name="image"/> is null. </exception>
        /// <exception cref="ArgumentException"> <paramref name="image"/> has incorrect pixel format.</exception>
        /// <exception cref="OperationCanceledException"> The operation has been cancelled. </exception>
        /// <exception cref="Exception">Unexpected error while processing image.</exception>
        public Bitmap InvertParallel(Bitmap image, int tasksNumber, CancellationToken cancellationToken = default)
        {
            if (image is null)
                throw new ArgumentNullException(nameof(image));

            if (tasksNumber > Environment.ProcessorCount)
                Logger.Warning($"Processor count is {Environment.ProcessorCount} but passed {tasksNumber}.");

            var bmp = new Bitmap(image);

            try
            {
                var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);

                // Lock image for next multithreading processing.
                var data = bmp.LockBits(rectangle, ImageLockMode.ReadWrite, bmp.PixelFormat);
                int colorDepth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
                byte[] buffer = new byte[bmp.Width * bmp.Height * colorDepth];

                var options = new ParallelOptions { CancellationToken = cancellationToken };

                // Copy locked image (pixels) into buffer for parallel access.
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                Parallel.For(0, tasksNumber, options, (taskNumber) =>
                {
                    options.CancellationToken.ThrowIfCancellationRequested();
                    int yChunk = data.Height / tasksNumber;
                    int startY = taskNumber * yChunk;
                    int endY = (taskNumber * yChunk) + yChunk;

                    ProcessInvert(buffer, 0, data.Width, startY, endY, data.Width, colorDepth);
                });

                // Copy processed pixels back from buffer to image.
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);

                bmp.UnlockBits(data);
                return bmp;
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warning(ex.Message);
                throw;
            }
            catch (ArgumentException ex)
            {
                Logger.Warning($"Incorrect image's pixel format. See details: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error($"Operation failed because of unexpected error. See detials: {ex.Message}");
                throw;
            }
        }

        #endregion


        #region Privates   

        private void ProcessGrayscale(Bitmap bitmap, int xStart, int xEnd, int yStart, int yEnd)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    var oldColor = bitmap.GetPixel(x, y);
                    int grayscaleColor = (int)(0.2126f * oldColor.R + 0.7152f * oldColor.G + 0.0722f * oldColor.B);
                    bitmap.SetPixel(x, y, Color.FromArgb(grayscaleColor, grayscaleColor, grayscaleColor));
                }
            }
        }

        private void ProcessGrayscale(byte[] buffer, int xStart, int xEnd, int yStart, int yEnd, int width, int colorDepth)
        {
            // Grayscale equation according to https://en.wikipedia.org/wiki/Grayscale
            // buffer[offest]      -> Red
            // buffer[offest + 1]  -> Green
            // buffer[offest + 2]  -> Blue

            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    int offset = ((y * width) + x) * colorDepth;
                    byte grayscale = (byte)(0.2126f * buffer[offset] + 0.7152f * buffer[offset + 1] + 0.0722f * buffer[offset + 2]);
                    buffer[offset] = buffer[offset + 1] = buffer[offset + 2] = grayscale;
                }
            }
        }

        private void ProcessBlur(byte[] buffer, int kernelSize, int xStart, int xEnd, int yStart, int yEnd, int width, int colorDepth)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    CalculateAverageRgb(buffer, x, y, kernelSize, width, colorDepth);
                }
            }
        }

        private void ProcessBlur(Bitmap bmp, int kernelSize)
        {
            for (int x = kernelSize / 2; x < bmp.Width - kernelSize; x++)
            {
                for (int y = kernelSize / 2; y < bmp.Height - kernelSize; y++)
                {
                    var rgb = CalculateAverageRgb(bmp, x, y, kernelSize);
                    bmp.SetPixel(x, y, Color.FromArgb(rgb['R'], rgb['G'], rgb['B']));
                }
            }
        }

        private void ProcessMedianFilter(byte[] buffer, int kernelSize, int xStart, int xEnd, int yStart, int yEnd, int width, int colorDepth)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    int offset = ((y * width) + x) * colorDepth;
                    buffer[offset] = buffer[offset + 1] = buffer[offset + 2] = CalculateRgbMedian(buffer, x, y, kernelSize, width, colorDepth);
                }
            }
        }

        private void ProcessMedianFilter(Bitmap bmp, int kernelSize)
        {
            for (int x = kernelSize / 2; x < bmp.Width - kernelSize / 2; x++)
            {
                for (int y = kernelSize / 2; y < bmp.Height - kernelSize / 2; y++)
                {
                    int median = CalculateWindowMedian(bmp, x, y, kernelSize);
                    bmp.SetPixel(x, y, Color.FromArgb(median, median, median));
                }
            }
        }

        private void ProcessInvert(byte[] buffer, int xStart, int xEnd, int yStart, int yEnd, int width, int colorDepth)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    int offset = ((y * width) + x) * colorDepth;
                    buffer[offset] = (byte)(255 - buffer[offset]);
                    buffer[offset + 1] = (byte)(255 - buffer[offset + 1]);
                    buffer[offset + 2] = (byte)(255 - buffer[offset + 2]);
                }
            }
        }

        private void ProcessInvert(Bitmap bmp)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    var currentPixelColor = bmp.GetPixel(x, y);
                    int red = currentPixelColor.R;
                    int green = currentPixelColor.G;
                    int blue = currentPixelColor.B;

                    bmp.SetPixel(x, y, Color.FromArgb(255 - red, 255 - green, 255 - blue));
                }
            }
        }

        private int CalculateWindowMedian(Bitmap image, int x, int y, int kernelSize)
        {
            List<int> pixelsValues = new List<int>();

            for (int i = x - (kernelSize / 2); i < x + (kernelSize / 2) + 1; i++)
            {
                for (int j = y - (kernelSize / 2); j < y + (kernelSize / 2) + 1; j++)
                {
                    pixelsValues.Add(image.GetPixel(i, j).R);
                }
            }

            return pixelsValues.OrderBy(x => x).ElementAt(pixelsValues.Count / 2);
        }

        private IDictionary<char, int> CalculateAverageRgb(Bitmap image, int x, int y, int kernelSize)
        {
            IDictionary<char, int> rgb = new Dictionary<char, int> { { 'R', 0 }, { 'G', 0 }, { 'B', 0 } };

            for (int i = x - (kernelSize / 2); i < x + (kernelSize / 2) + 1; i++)
            {
                for (int j = y - (kernelSize / 2); j < y + (kernelSize / 2) + 1; j++)
                {
                    rgb['R'] += image.GetPixel(i, j).R;
                    rgb['G'] += image.GetPixel(i, j).G;
                    rgb['B'] += image.GetPixel(i, j).B;
                }
            }

            rgb['R'] /= kernelSize * kernelSize;
            rgb['G'] /= kernelSize * kernelSize;
            rgb['B'] /= kernelSize * kernelSize;

            return rgb;
        }

        private void CalculateAverageRgb(byte[] buffer, int x, int y, int kernelSize, int width, int colorDepth)
        {
            Dictionary<char, int> rgb = new Dictionary<char, int> { { 'R', 0 }, { 'G', 0 }, { 'B', 0 } };

            try
            {
                for (int i = x - (kernelSize / 2); i < x + (kernelSize / 2) + 1; i++)
                {
                    for (int j = y - (kernelSize / 2); j < y + (kernelSize / 2) + 1; j++)
                    {
                        int offset = ((j * width) + i) * colorDepth;
                        rgb['R'] += buffer[offset];
                        rgb['G'] += buffer[offset + 1];
                        rgb['B'] += buffer[offset + 2];
                    }
                }

                rgb['R'] /= kernelSize * kernelSize;
                rgb['G'] /= kernelSize * kernelSize;
                rgb['B'] /= kernelSize * kernelSize;

                int currentPixel = ((y * width) + x) * colorDepth;

                buffer[currentPixel] = (byte)rgb['R'];
                buffer[currentPixel + 1] = (byte)rgb['G'];
                buffer[currentPixel + 2] = (byte)rgb['B'];
            }
            catch (Exception)
            {

                throw;
            }
        }

        private byte CalculateRgbMedian(byte[] buffer, int x, int y, int kernelSize, int width, int colorDepth)
        {
            List<byte> rCanal = new List<byte>();

            for (int i = x - (kernelSize / 2); i < x + (kernelSize / 2) + 1; i++)
            {
                for (int j = y - (kernelSize / 2); j < y + (kernelSize / 2) + 1; j++)
                {
                    int offset = ((j * width) + i) * colorDepth;
                    rCanal.Add(buffer[offset]);
                }
            }

            return rCanal.OrderBy(x => x).ElementAt(kernelSize * kernelSize / 2);
        }

        #endregion

    }
}
