## Parallel Image Processing

This project covers the theme of the image processing such as blur, grayscale, colour invert, image contrast and median filter, 
but its main purpose is to present parallel processing instead of algorithms of filter themselves. It was built mainly to 
learning some stuff about multithreading and parallel programming.

The application applies specific filters to the image, display a short processing overview, save images and processing statistics. 
To facilitate the manipulation of statistics, they are saved as a CSV file. Currently available filters are blur, grayscale, median, 
inversion of colour and images contrast. As command line parameters, it requires image path, kernel size and timeout for cancelling 
parallel operations. 

  - If only image name is passed then app searches for this image in the Documents folder. 
  - Statistics file is saved in the Documents folder. The timeout is in milliseconds and must be greater than 0.
  - Kernel size must be an odd number and must be between 3 and an image height or width
  
Example of run application which takes ***image.png*** file from the Documents folder and set the kernel size to ***3x3*** and parallel 
operations timeout to ***20 seconds***: `dotnet run ParallelImageProcessing image.png 3 20000`

## Image filters

Below are brief descriptions of the algorithms used in the project.

**Blur**

A [blur](https://en.wikipedia.org/wiki/Box_blur) is implemented as a sliding-window algorithm. The blurred colour of the current pixel 
is the average of the current pixel's colour and its neighbouring pixels. The number of neighbouring pixels 
relies on the kernel size, eg. for kernel size = 3x3, the number of neighbouring pixels is 8.

**Grayscale**

The [grayscale](https://en.wikipedia.org/wiki/Grayscale) algorithm works on every pixel of the image and, unlike the blur algorithm, 
does not use the kernel to calculate the resulting colour. The grayscale colour of the processing pixel is determined by the equation: 
0.2126 * R + 0.7152 * G + 0.0722 * B, where R, G, B is the red, green and blue colour.

**Median filter**

The way the [median filter](https://en.wikipedia.org/wiki/Median_filter) algorithm works is the same as blur, but the colour is calculated 
as the median of the current colour and neighbouring pixels. 
In this implementation, only R colour is under consideration.

**Invert**

Similar to grayscale, the inverse filter does not use the kernel to calculate the current pixel colour. The colour is determined by 
subtracting from the maximum value (255) of the current value of the given colour. For example, for a colour 
in RGB 100 100 100, the resulting colour is RGB 155 155 155.

**Contrast**

Changing the image contrast is the only process that actually has got no parallel implementation because it is very fast in 
its basic form.

## App output

After starting the application, it prints on a screen a short information about the image processing.

![console output](https://github.com/Savaed/ParallelImageProcessing/blob/master/Docs/console-output.png)

More examples of app output as well as image processing statistics and charts are in the 
[Docs](https://github.com/Savaed/ParallelImageProcessing/blob/master/Docs) folder.


## Local run

Feel free to download this repository and develop it locally as you like. For those interested in how the application works
or detailed implementation of algorithms, a step-by-step instruction is available on how to clone the repository, build the 
project and run it locally.

1. [Download](https://git-scm.com/downloads) and install Git for your operating system
2. [Download](https://dotnet.microsoft.com/download/dotnet-core) and install Microsoft .NET Core SDK (min. version 3.1)
3. Run Git CLI and then go to a folder in which you want to have got the repository. Next clone the repo using the command: 
`git clone https://github.com/Savaed/ParallelImageProcessing.git`
4. Go to the main project folder (this one with **.csproj* file) and build the project with command: `dotnet build`
5. Run the app: ***dotnet run ParallelImageProcessing \<image path\> \<kernel size\> \<parallel operation timeout in ms\>***
Example run on Windows: `dotnet run ParallelImageProcessing c:/users/user1/desktop/image.png 3 20000`
