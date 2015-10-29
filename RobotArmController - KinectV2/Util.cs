using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThirdYearProject.RobotArmController.Sensors
{
    class Util
    {
        const int MaxValue = 230;
        const int MinValue = 0;
        public static byte GetApproximatedTranslation(double angle, int min, int max, int stages, bool inverted)
        {
            angle = Math.Max(Math.Min(max, angle), min);

            int newAngle = (int)((((angle - min) * (MaxValue - MinValue)) / (max - min)) + MinValue);
            if (inverted)
                newAngle = MaxValue - newAngle;


            double partitionSize = (MaxValue + MinValue) / stages;
            byte approximatedAngle = (byte)(Math.Round(newAngle / partitionSize) * partitionSize);

            return approximatedAngle;
        }
    }
}
