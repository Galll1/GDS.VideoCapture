using Accord.Audio;
using System;

namespace GDS.VideoCapture.Core.Capturing
{
    public interface IAudioCapturingOutput : ICapturingOutput, IDisposable
    {
        void AudioOutputMethod(Signal input);
    }
}
