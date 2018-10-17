using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using GDS.VideoCapture.Core.Capturing;
using GDS.VideoCapture.Core.Capturing.Implementations;
using GDS.VideoCapture.Core;

namespace GDS.VideoCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IVideoCapturingOutput
    {
        Capturer capturer;

        FileCapturer fileCapturer = new FileCapturer();
        AudioCapturingOutputToDevice audioOutput = new AudioCapturingOutputToDevice();

        public MainWindow()
        {
            InitializeComponent();
            capturer = new Capturer(this, fileCapturer, audioOutput);

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            audioOutput.Init(Handle);
            capturer.StartCapturing();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            capturer.Dispose();
            capturer = null;
        }

        public void VideoOutputMethod(Bitmap input)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                input.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage currentFrame = new BitmapImage();
                currentFrame.BeginInit();
                currentFrame.StreamSource = ms;
                currentFrame.EndInit();

                currentFrame.Freeze();

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    videoHolder.Source = currentFrame;
                }));
            }
            catch (Exception ex)
            {
            }
        }

        private void startRecording_Click(object sender, RoutedEventArgs e)
        {
            fileCapturer.StartRecording(capturer.CaptureDeviceHeight, capturer.CaptureDeviceWidth);
        }

        private void stopRecording_Click(object sender, RoutedEventArgs e)
        {
            fileCapturer.StopRecording();
        }

        public void Dispose()
        {

        }

        private IntPtr Handle => new WindowInteropHelper(this).Handle;
    }
}
