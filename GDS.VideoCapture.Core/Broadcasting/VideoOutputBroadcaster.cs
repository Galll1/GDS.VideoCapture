using GDS.VideoCapture.Core.Capturing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace GDS.VideoCapture.Core.Broadcasting
{
    public class VideoOutputBroadcaster
    {
        private readonly IEnumerable<IVideoCapturingOutput> videoListeners;
        public VideoOutputBroadcaster(IEnumerable<IVideoCapturingOutput> videoListeners)
        {
            this.videoListeners = videoListeners;
        }

        public void Broadcast(Bitmap input)
        {
            if(videoListeners == null)
            {
                return;
            }

            foreach(IVideoCapturingOutput outputListener in videoListeners)
            {
                BroadcastAsync(outputListener, (Bitmap)input.Clone());
            }
        }

        private async void BroadcastAsync(IVideoCapturingOutput outputListener, Bitmap input)
        {
            await Task.Factory.StartNew(() => outputListener.VideoOutputMethod(input));
        }
    }
}
