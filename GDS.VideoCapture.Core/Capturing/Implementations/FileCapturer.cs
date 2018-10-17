using Accord.Audio;
using Accord.Audio.Formats;
using Accord.Video.FFMPEG;
using GDS.VideoCapture.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GDS.VideoCapture.Core.Capturing.Implementations
{
    public class FileCapturer : IVideoCapturingOutput, IAudioCapturingOutput
    {
        private object videoLocker = new object();
        private object queueLocker = new object();
        private object recordingLocker = new object();
        private object stopLocker = new object();

        private bool isRecording = false;
        private bool stopRequested = false;
        
        private VideoFileWriter videoFileWriter;
        private FileStream audioFileWriter;
        private WaveEncoder waveEncoder;
        DateTime? firstFrameTime;

        List<EnqueuedFrame> frameQueue = new List<EnqueuedFrame>();
        Task processingTask;

        private string videoOutputFile = "D:\\FileCapturer.avi";
        private string audioOutputFile = "D:\\FileCapturer.wav";
        private string outputFile = "D:\\FinalVideoCapturer.avi";
        private string mergerFile = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "\\ffmpeg\\ffmpeg.exe";

        public void StartRecording(int height, int width)
        {
            bool isRecordingCurrently = false;
            lock (recordingLocker)
            {
                isRecordingCurrently = isRecording;
            }

            if (isRecordingCurrently)
            {
                Console.WriteLine("Already recording...");
                return;
            }

            if (height < 16 || width < 16)
            {
                Console.WriteLine($"You are dumb, both height ({height}) and width ({width}) have to be > 16");
                return;
            }

            firstFrameTime = DateTime.Now;

            
            InitAudioFileWriter();

            isRecording = true;
            stopRequested = false;
            processingTask = new Task(() => ProcessRecording(height, width));
            processingTask.Start();
        }

        private void ProcessRecording(int height, int width)
        {
            InitVideoFileWriter(height, width);

            bool isRecordingCurrently = true;

            while (isRecordingCurrently)
            {
                EnqueuedFrame frameToSave = DequeueToProcess();

                if(frameToSave == null)
                {
                    continue;
                }

                //FilterApplier.ApplyInPlace(frameToSave.Frame);

                lock(videoLocker)
                {
                    videoFileWriter.WriteVideoFrame(frameToSave.Frame, frameToSave.FrameOffset);
                }

                lock(recordingLocker)
                {
                    isRecordingCurrently = isRecording;
                }
            }
        }

        private EnqueuedFrame DequeueToProcess()
        {
            lock (queueLocker)
            {
                if(!frameQueue.Any())
                {
                    return null;
                }

                EnqueuedFrame frame = frameQueue[0];
                frameQueue.Remove(frame);

                return frame;
            }
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
            lock(stopLocker)
            {
                stopRequested = true;
            }

            WaitForAllFramesWritten();

            bool wasRecording = false;

            lock(recordingLocker)
            {
                wasRecording = isRecording;
                isRecording = false;
            }

            if(wasRecording)
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

        private void WaitForAllFramesWritten()
        {
            bool isWriting = true;
            while(isWriting)
            {
                lock(queueLocker)
                {
                    isWriting = frameQueue.Any();
                }
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
            bool stopWasRequested = false;
            lock (stopLocker)
            {
                stopWasRequested = stopRequested;
            }

            if (videoFileWriter != null && videoFileWriter.IsOpen && !stopWasRequested)
            {
                EnqueueToProcess(input);
            }
        }

        private void EnqueueToProcess(Bitmap input)
        {
            TimeSpan frameOffset = TimeSpan.FromMilliseconds(DateTime.Now.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalMilliseconds - firstFrameTime.Value.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds);
            lock (queueLocker)
            {
                frameQueue.Add(new EnqueuedFrame(frameOffset, input));
            }
        }

        public void AudioOutputMethod(Signal input)
        {
            if (audioFileWriter != null && waveEncoder != null)
            {
                waveEncoder.Encode(input);
            }
        }

        private class EnqueuedFrame
        {
            public EnqueuedFrame(TimeSpan frameOffset, Bitmap frame)
            {
                FrameOffset = frameOffset;
                Frame = frame;
            }

            public TimeSpan FrameOffset { get; }
            public Bitmap Frame { get; }
        }
    }
}