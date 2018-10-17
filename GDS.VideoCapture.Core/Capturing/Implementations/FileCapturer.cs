using Accord.Audio;
using Accord.Audio.Formats;
using Accord.Video.FFMPEG;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace GDS.VideoCapture.Core.Capturing.Implementations
{
    public class FileCapturer : IVideoCapturingOutput, IAudioCapturingOutput
    {
        private VideoFileWriter videoFileWriter;
        private FileStream audioFileWriter;
        private WaveEncoder waveEncoder;
        long? startTick;

        private string videoOutputFile = "D:\\FileCapturer.avi";
        private string audioOutputFile = "D:\\FileCapturer.wav";
        private string outputFile = "D:\\FinalVideoCapturer.avi";
        private string mergerFile = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "\\ffmpeg\\ffmpeg.exe";

        public FileCapturer()
        {
            frameCount = 0;
        }

        long frameCount;

        public void StartRecording(int height, int width)
        {
            if (IsRecording())
            {
                Console.WriteLine("Already recording...");
                return;
            }

            if (height < 16 || width < 16)
            {
                Console.WriteLine($"You are dumb, both height ({height}) and width ({width}) have to be > 16");
                return;
            }

            startTick = null;

            InitVideoFileWriter(height, width);
            InitAudioFileWriter();
        }

        private bool IsRecording()
        {
            return (videoFileWriter != null && videoFileWriter.IsOpen) || audioFileWriter != null;
        }

        private void InitVideoFileWriter(int height, int width)
        {
            videoFileWriter = new VideoFileWriter();
            videoFileWriter.Open(videoOutputFile, width, height, 60, VideoCodec.Default, 5000000);
        }

        private void InitAudioFileWriter()
        {
            audioFileWriter = new FileStream(audioOutputFile, FileMode.Create);
            waveEncoder = new WaveEncoder(audioFileWriter);
        }

        public void StopRecording()
        {
            if(IsRecording())
            {
                MergeAudioAndVideo();
            }

            if (videoFileWriter != null)
            {
                videoFileWriter.Close();
                videoFileWriter.Dispose();
                videoFileWriter = null;
            }

            if (audioFileWriter != null || waveEncoder != null)
            {
                waveEncoder.Close();
                waveEncoder = null;
                audioFileWriter = null;
            }
        }

        private void MergeAudioAndVideo()
        {
            File.Delete(outputFile);

            Process mergeProcess = new Process();
            ProcessStartInfo mergeProcessStartInfo = new ProcessStartInfo();
            mergeProcessStartInfo.FileName = mergerFile;
            mergeProcessStartInfo.UseShellExecute = false;
            mergeProcessStartInfo.CreateNoWindow = true;
            mergeProcessStartInfo.Arguments = "-i \"" + videoOutputFile + "\" -i \"" + audioOutputFile + "\" -c:v copy -c:a copy -map 0:v:0 -map 1:a:0 \"" + outputFile + "\"";

            mergeProcess.StartInfo = mergeProcessStartInfo;
            mergeProcess.Start();
            mergeProcess.WaitForExit();

            if (File.Exists(outputFile))
            {
                Console.WriteLine("Recording merge completed!");
            }
        }

        public void Dispose()
        {
            StopRecording();
        }


        public void VideoOutputMethod(Bitmap input)
        {
            frameCount++;
            Console.WriteLine($"Frame count: {frameCount}");

            if (videoFileWriter != null && videoFileWriter.IsOpen)
            {
                long currentTick = DateTime.Now.Ticks;
                startTick = startTick ?? currentTick;
                TimeSpan frameOffset = new TimeSpan(currentTick - startTick.Value);
                videoFileWriter.WriteVideoFrame(input, frameOffset);
            }
        }

        public void AudioOutputMethod(Signal input)
        {
            if (audioFileWriter != null && waveEncoder != null)
            {
                waveEncoder.Encode(input);
            }
        }
    }
}