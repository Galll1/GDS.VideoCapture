using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using System;
using System.IO;

namespace GDS.VideoCapture.Core.Capturing.Implementations
{
    public class AudioCapturingOutputToDevice : IAudioCapturingOutput
    {
        private IntPtr audioOutputDeviceOwnerHandle;
        private AudioOutputDevice audioOutputDevice;
        private MemoryStream memoryStream;
        private WaveEncoder encoder;
        private WaveDecoder decoder;

        public void Init(IntPtr handle)
        {
            audioOutputDeviceOwnerHandle = handle;
            memoryStream = new MemoryStream();
            encoder = new WaveEncoder(memoryStream);
        }

        public void AudioOutputMethod(Signal input)
        {
            if(audioOutputDeviceOwnerHandle == null)
            {
                return;
            }

            if(audioOutputDevice == null)
            {
                InitAudioOutputDevice(input);
            }

            encoder.Encode(input);
            float[] samples = new float[input.Samples];

            input.CopyTo(samples);

            audioOutputDevice.Play();
        }

        private void InitAudioOutputDevice(Signal probingInput)
        {
            audioOutputDevice = new AudioOutputDevice(audioOutputDeviceOwnerHandle, probingInput.SampleRate, probingInput.Channels);
            audioOutputDevice.FramePlayingStarted += AudioOutputDeviceFramePlayingStarted;
            audioOutputDevice.NewFrameRequested += AudioOutputDeviceNewFrameRequested;
            audioOutputDevice.Stopped += AudioOutputDevicePlayingFinished;
        }

        private void AudioOutputDeviceFramePlayingStarted(object sender, PlayFrameEventArgs e)
        {
            //MemoryStream playStream = new MemoryStream();
            //memoryStream.CopyTo(playStream);
            //MemoryStream stream = GetRepairedStream(memoryStream);
            //stream.Seek(0, SeekOrigin.Begin);

            //decoder = new WaveDecoder(stream);
            //decoder.Decode(e.Count);

            //if (e.FrameIndex + e.Count < decoder.Frames)
            //{
            //    int previous = decoder.Position;
            //    decoder.Seek(e.FrameIndex);

            //    Signal s = decoder.Decode(e.Count);
            //    decoder.Seek(previous);
            //}
        }

        private MemoryStream GetRepairedStream(MemoryStream playStream)
        {
            MemoryStream repairedMemoryStream = new MemoryStream();
            if (playStream == null || playStream.Length < 1)
            {
                return new MemoryStream();
            }

            long count = playStream.Length / 16406L;
            byte[] buffer = playStream.GetBuffer();

            for (int iter = 0; iter < count; iter++)
            {
                byte[] subBuffer = GetSubBuffer(buffer, 16406L, iter * 16406L);
                repairedMemoryStream.Write(buffer, (int)(iter * 16406L), 16406);
            }
            return repairedMemoryStream;
        }

        private byte[] GetSubBuffer(byte[] buffer, long bytesToRead, long offset)
        {
            byte[] subBuffer = new byte[bytesToRead];

            for (int iter = 0; iter < bytesToRead; iter++)
            {
                subBuffer[iter] = buffer[iter + offset];
            }

            return subBuffer;
        }

        private void AudioOutputDeviceNewFrameRequested(object sender, NewFrameRequestedEventArgs e)
        {
            if(decoder == null)
            {
                return;
            }

            e.FrameIndex = decoder.Position;

            Signal signal = decoder.Decode(e.Frames);

            if (signal == null)
            {
                e.Stop = true;
                return;
            }

            e.Frames = signal.Length;

            signal.CopyTo(e.Buffer);
        }

        private void AudioOutputDevicePlayingFinished(object sender, EventArgs e)
        {
            
        }

        public void Dispose()
        {
            audioOutputDevice?.Dispose();
        }
    }
}
