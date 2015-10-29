using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    class AngleConverter
    {

        byte valueMax = 230;
        byte valueMin = 0;

        public byte GetMiddleFlexerByteValue(int angle)
        {
            byte angleMin = 20;
            byte angleMax = 180;


            byte value;
            if (angle < 20)
                angle = 20;
            else if (angle > 180)
                angle = 180;

            value = (byte)((((angle - angleMin) * (valueMax - valueMin)) / (angleMax - angleMin)) + valueMin);

            return value;
        }
    }
}
