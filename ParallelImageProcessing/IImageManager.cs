using System.Drawing;
using System.Threading;

namespace ParallelImageProcessing
{
    public interface IImageManager
    {
        Bitmap Open(string imagePath);
        string Save(Bitmap image, string imageName);
        Bitmap Blur(Bitmap image, int kernelSize, bool useLockBits = true);
        Bitmap BlurParallel(Bitmap image, int kernelSize, int tasksNumber, CancellationToken cancellationToken = default);
        Bitmap Contrast(Bitmap image, float contrast);
        Bitmap Grayscale(Bitmap image, bool useLockBits = true);
        Bitmap GrayscaleParallel(Bitmap image, int tasksNumber, CancellationToken cancellationToken = default);
        Bitmap Invert(Bitmap image, bool useLockBits = true);
        Bitmap InvertParallel(Bitmap image, int tasksNumber, CancellationToken cancellationToken = default);
        Bitmap MedianFilter(Bitmap image, int kernelSize, bool useLockBits = true);
        Bitmap MedianFilterParallel(Bitmap image, int kernelSize, int tasksNumber, CancellationToken cancellationToken = default);
    }
}