using System.Drawing;

namespace GDS.VideoCapture.Core.Capturing
{
    public interface IVideoCapturingOutput : ICapturingOutput
    {
        void VideoOutputMethod(Bitmap input);
    }
}
