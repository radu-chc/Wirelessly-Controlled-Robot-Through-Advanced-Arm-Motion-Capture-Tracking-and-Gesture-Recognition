using Leap;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ThirdYearProject.RobotArmController.Sensors
{
    class LeapListener : Listener
    {
        public float pinchStrength { get; set; }
        public float wristPitch { get; set; }
        public float wristYaw { get; set; }

        private Object thisLock = new Object();


        private void SafeWriteLine(String line)
        {
            lock (thisLock)
            {
                Console.WriteLine(line);
            }
        }

        public void WriteStatus()
        {
            SafeWriteLine("Yaw: " + wristYaw + "   Pitch: " + wristPitch + "   Strength: " + pinchStrength);
            
        }
        public override void OnInit(Controller controller)
        {
            SafeWriteLine("Leap Initialized");
        }

        public override void OnConnect(Controller controller)
        {
            SafeWriteLine("Leap Connected");
            controller.EnableGesture(Gesture.GestureType.TYPE_CIRCLE);
            controller.EnableGesture(Gesture.GestureType.TYPE_KEY_TAP);
            controller.EnableGesture(Gesture.GestureType.TYPE_SCREEN_TAP);
            controller.EnableGesture(Gesture.GestureType.TYPE_SWIPE);
        }

        public override void OnDisconnect(Controller controller)
        {
            //Note: not dispatched when running in a debugger.
            SafeWriteLine("Leap Disconnected");
        }

        public override void OnExit(Controller controller)
        {
            SafeWriteLine("Leap Exited");
        }

        public override void OnFrame(Controller controller)
        {
            // Get the most recent frame and report some basic information
            Frame frame = controller.Frame();

            /*SafeWriteLine("Frame id: " + frame.Id
                        + ", timestamp: " + frame.Timestamp
                        + ", hands: " + frame.Hands.Count
                        + ", fingers: " + frame.Fingers.Count
                        + ", tools: " + frame.Tools.Count
                        + ", gestures: " + frame.Gestures().Count);
            */

            pinchStrength = frame.Hands.Count > 0 ? frame.Hands[0].GrabStrength:pinchStrength;
            wristPitch = frame.Hands.Count > 0 ? frame.Hands[0].Direction.Pitch:0;
            wristYaw = frame.Hands.Count > 0 ? frame.Hands[0].Direction.Roll : 0;
        }
    }
}

