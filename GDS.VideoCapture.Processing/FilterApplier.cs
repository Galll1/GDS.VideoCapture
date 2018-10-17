using Accord.Imaging.Filters;
using System.Drawing;

namespace GDS.VideoCapture.Processing
{
    public static class FilterApplier
    {
        public static void ApplyInPlace(Bitmap frame)
        {
            BilateralSmoothing conservativeSmoothingFilter = new BilateralSmoothing();

            conservativeSmoothingFilter.ApplyInPlace(frame);
        }
    }
}
