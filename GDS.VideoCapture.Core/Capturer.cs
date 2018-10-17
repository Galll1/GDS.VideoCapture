using Accord.Video.DirectShow;
using Accord.Video;
using System;
using Accord.DirectSound;
using System.Collections.Generic;
using Accord.Imaging.Filters;
using GDS.VideoCapture.Core.Capturing;
using GDS.VideoCapture.Core.Broadcasting;

namespace GDS.VideoCapture.Core
{
    public class Capturer : IDisposable
    {
        private FilterInfoCollection videoCaptureDevices;
        private VideoCaptureDevice videoCaptureDevice;
        private IEnumerable<IVideoCapturingOutput> videoCaptureOutputs;

        private AudioDeviceCollection audioCaptureDevices;
        private AudioCaptureDevice audioCaptureDevice;
        private IEnumerable<IAudioCapturingOutput> audioCaptureOutputs;

        private VideoOutputBroadcaster videoOutputBroadcaster;

        public Capturer(params ICapturingOutput[] capturingOutputs)
        {
            FilterCapturers(capturingOutputs);

            videoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            audioCaptureDevices = new AudioDeviceCollection(AudioDeviceCategory.Capture);
        }

        private void FilterCapturers(ICapturingOutput[] capturingOutputs)
        {
            if(capturingOutputs == null)
            {
                throw new ArgumentNullException("Outputs cannot be null!");
            }

            List<IVideoCapturingOutput> videoCaptureOutputsFiltered = new List<IVideoCapturingOutput>();
            List<IAudioCapturingOutput> audioCaptureOutputsFiltered = new List<IAudioCapturingOutput>();

            foreach (ICapturingOutput capturingOutput in capturingOutputs)
            {
                if (capturingOutput is IVideoCapturingOutput)
                {
                    videoCaptureOutputsFiltered.Add(capturingOutput as IVideoCapturingOutput);
                }
                if (capturingOutput is IAudioCapturingOutput)
                {
                    audioCaptureOutputsFiltered.Add(capturingOutput as IAudioCapturingOutput);
                }
            }

            videoCaptureOutputs = videoCaptureOutputsFiltered;
            audioCaptureOutputs = audioCaptureOutputsFiltered;
        }

        void CaptureVideoDeviceNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            videoOutputBroadcaster.Broadcast((System.Drawing.Bitmap)eventArgs.Frame.Clone());
            //System.Drawing.Bitmap frame = (System.Drawing.Bitmap)eventArgs.Frame.Clone();

            //BilateralSmoothing conservativeSmoothingFilter = new BilateralSmoothing();

            //conservativeSmoothingFilter.ApplyInPlace(frame);

            //foreach (IVideoCapturingOutput capturingOutput in videoCaptureOutputs)
            //{
            //    capturingOutput.VideoOutputMethod(frame);
            //}
        }

        private void AudioCaptureDeviceNewFrame(object sender, Accord.Audio.NewFrameEventArgs eventArgs)
        {
            foreach (IAudioCapturingOutput capturingOutput in audioCaptureOutputs)
            {
                capturingOutput.AudioOutputMethod(eventArgs.Signal);
            }
        }

        public void StartCapturing()
        {
            if(videoCaptureDevices.Count < 0)
            {
                return;
            }

            videoOutputBroadcaster = new VideoOutputBroadcaster(videoCaptureOutputs);

            videoCaptureDevice = new VideoCaptureDevice(videoCaptureDevices[0].MonikerString);

            audioCaptureDevice = GetProperAudioDevice();

            ProbeVideoDevice();

            videoCaptureDevice.NewFrame += CaptureVideoDeviceNewFrame;
            videoCaptureDevice.Start();

            audioCaptureDevice.NewFrame += AudioCaptureDeviceNewFrame;
            audioCaptureDevice.Start();
        }

        private AudioCaptureDevice GetProperAudioDevice()
        {
            AudioDeviceInfo audioDeviceInfo = null;

            foreach (AudioDeviceInfo audioCaptureDeviceInfo in audioCaptureDevices)
            {
                if (audioCaptureDeviceInfo.Guid != Guid.Empty)
                {
                    audioDeviceInfo = audioCaptureDeviceInfo;
                }
            }

            return audioDeviceInfo == null ? new AudioCaptureDevice() : new AudioCaptureDevice(audioDeviceInfo);
        }

        private void ProbeVideoDevice()
        {
            videoCaptureDevice.NewFrame += CaptureVideoDeviceNewFrameProbe;
            videoCaptureDevice.Start();
        }

        private void CaptureVideoDeviceNewFrameProbe(object sender, NewFrameEventArgs eventArgs)
        {
            System.Drawing.Bitmap probingFrame = (System.Drawing.Bitmap)eventArgs.Frame.Clone();

            CaptureDeviceHeight = probingFrame.Height;
            CaptureDeviceWidth = probingFrame.Width;

            videoCaptureDevice.NewFrame -= CaptureVideoDeviceNewFrameProbe;
        }

        public void Dispose()
        {
            DisposeVideoDevice();
            DisposeAudioDevice();
            DisposeCapturingOutputs(videoCaptureOutputs);
            DisposeCapturingOutputs(audioCaptureOutputs);
        }

        private void DisposeCapturingOutputs(IEnumerable<ICapturingOutput> captureOutputsToDispose)
        {
            if(captureOutputsToDispose == null)
            {
                return;
            }

            foreach(ICapturingOutput capturingOutput in captureOutputsToDispose)
            {
                capturingOutput.Dispose();
            }
        }

        private void DisposeAudioDevice()
        {
            if (audioCaptureDevice == null)
            {
                return;
            }

            if (audioCaptureDevice.IsRunning)
            {
                audioCaptureDevice.Stop();
            }

            audioCaptureDevice.NewFrame -= AudioCaptureDeviceNewFrame;
        }

        private void DisposeVideoDevice()
        {
            if (videoCaptureDevice == null)
            {
                return;
            }

            if (videoCaptureDevice.IsRunning)
            {
                videoCaptureDevice.Stop();
            }

            videoCaptureDevice.NewFrame -= CaptureVideoDeviceNewFrame;
        }

        public int CaptureDeviceHeight { get; private set; }
        public int CaptureDeviceWidth { get; private set; }
    }
}
