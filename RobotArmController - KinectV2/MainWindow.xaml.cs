    
namespace ThirdYearProject.RobotArmController
{

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    using System.Windows.Threading;
    using ThirdYearProject.RobotArmController.ArmInterface;
    using ThirdYearProject.RobotArmController.Sensors;
    using Awesomium.Core;
    using System.Net;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {


        private void GripperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                myArm.send1Pos(6, (byte)GripperSlider.Value);
            }
            catch { }
        }

        private void TopRotatorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                myArm.send1Pos(5, (byte)TopRotatorSlider.Value);
            }
            catch { }
        }

        private void TopFlexerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                myArm.send1Pos(4, (byte)TopFlexerSlider.Value);
            }
            catch { }
        }

        private void MiddleFlexerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                myArm.send1Pos(3, (byte)MiddleFlexerSlider.Value);
            }
            catch { }
        }

        public void LowerFlexerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                myArm.send1Pos(2, (byte)LowerFlexerSlider.Value);
            }
            catch { }
        }

        private void LowerRotatorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                myArm.send1Pos(1, (byte)LowerRotatorSlider.Value);
            }
            catch { }
        }

        private void PowerServos_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                myArm.PowerServos(PowerServos.IsChecked.Value);
            }
            catch { }
        }

        private void ResetOrientations_Clicked(object sender, RoutedEventArgs e)
        {
            GripperSlider.Value = 100;
            TopRotatorSlider.Value = 100;
            TopFlexerSlider.Value = 100;
            MiddleFlexerSlider.Value = 100;
            LowerFlexerSlider.Value = 100;
            LowerRotatorSlider.Value = 100;
        }

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        static Leap.Controller controller;
        static LeapListener listener;
        TextBoxOutputter outputter;
        RoboticArm myArm;
        System.Drawing.Bitmap leapImage;

        DispatcherTimer checkFeedAliveTimer, reloadFeedTimer, sensorTimer;

        public void reloadFeed(object sender, EventArgs e)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://192.168.173.79:8080");
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
                {
                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        return;
                    }
                    else
                    {
                        VideoCameraImage.Source = new BitmapImage(new Uri("Images/VideoCameraActive.png", UriKind.Relative));
                        webControl.Visibility = System.Windows.Visibility.Visible;
                        webControl.Reload(true);
                        reloadFeedTimer.Stop();
                        checkFeedAliveTimer.Start();
                    }
                    // Do nothing; we're only testing to see if we can get the response
                }
            }
            catch(Exception ex) 
            {
                Console.Write(ex.Message);
            }
        }

        public void checkFeedAlive(object sender, EventArgs e)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://192.168.173.79:8080");
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)req.GetResponse())
                {
                    if (response == null || response.StatusCode != HttpStatusCode.OK)
                    {
                        webControl.Visibility = System.Windows.Visibility.Hidden;
                        VideoCameraImage.Source = new BitmapImage(new Uri("Images/VideoCameraInactive.png", UriKind.Relative));
                        reloadFeedTimer.Start();
                        checkFeedAliveTimer.Stop();
                    }
                    else
                    {
                        webControl.Visibility = System.Windows.Visibility.Visible;
                        return;
                    }
                    // Do nothing; we're only testing to see if we can get the response
                }
            }
            catch
            {
                VideoCameraImage.Source = new BitmapImage(new Uri("Images/VideoCameraInactive.png", UriKind.Relative));                      
                webControl.Visibility = System.Windows.Visibility.Hidden;
                reloadFeedTimer.Start();
                checkFeedAliveTimer.Stop();
            }
        }


        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            checkFeedAliveTimer= new DispatcherTimer();
            checkFeedAliveTimer.Interval = new TimeSpan(0, 0, 5);
            checkFeedAliveTimer.Tick += checkFeedAlive;

            reloadFeedTimer = new DispatcherTimer();
            reloadFeedTimer.Interval = new TimeSpan(0, 0, 5);
            reloadFeedTimer.Tick += reloadFeed;

            //checkFeedAliveTimer.Start();

            sensorTimer = new DispatcherTimer();
            sensorTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            sensorTimer.Tick += dispatcherTimer_Tick;

            sensorTimer.Start();

            // Keep this process running until Enter is pressed
            Console.WriteLine("Press Enter to quit...");
            Console.ReadLine();

            // Remove the sample listener when done
            /*controller.RemoveListener(listener);
            controller.Dispose();
            */

            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            //this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
              //                                              : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
            //outputter = new TextBoxOutputter(OutputTextBox);
            //Console.SetOut(outputter);


            // Create a sample listener and controller
            listener = new LeapListener();
            controller = new Leap.Controller();

            controller.SetPolicy(Leap.Controller.PolicyFlag.POLICY_IMAGES);
            // Have the sample listener receive events from the controller
            controller.AddListener(listener);

            myArm = new RoboticArm();

            
            //myArm.PowerServos(true);
        }


        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            IntPtr hBitmap = new IntPtr();

            LeapImage.Source = new BitmapImage(new Uri("/Images/Leap_Axes.png", UriKind.Relative));
            if (!controller.IsConnected)
                return;
            try
            {
                Leap.Frame frame = controller.Frame();
                Leap.Image image = frame.Images[0];
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                //set palette
                System.Drawing.Imaging.ColorPalette grayscale = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                {
                    grayscale.Entries[i] = System.Drawing.Color.FromArgb((int)255, i, i, i);
                }
                bitmap.Palette = grayscale;


                System.Drawing.Rectangle lockArea = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                System.Drawing.Imaging.BitmapData bitmapData = bitmap.LockBits(lockArea, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                byte[] rawImageData = image.Data;
                System.Runtime.InteropServices.Marshal.Copy(rawImageData, 0, bitmapData.Scan0, image.Width * image.Height);
                bitmap.UnlockBits(bitmapData);

                System.Drawing.Bitmap clone = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(clone))
                {
                    gr.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, clone.Width, clone.Height));
                }

                float cameraOffset = 20; //x-axis offset in millimeters
                Leap.FingerList fingers = frame.Fingers;
                foreach (Leap.Finger finger in fingers)
                {

                    Leap.Vector palmNormal = finger.Hand.Direction;
                    float hSlope = -(palmNormal.x + cameraOffset * (2 * image.Id - 1)) / palmNormal.y;
                    float vSlope = palmNormal.z / palmNormal.y;
                    palmNormal = image.Warp(new Leap.Vector(hSlope, vSlope, 0));

                    Leap.Vector palmCenter = finger.Hand.PalmPosition;
                    hSlope = -(palmCenter.x + cameraOffset * (2 * image.Id - 1)) / palmCenter.y;
                    vSlope = palmCenter.z / palmCenter.y;
                    palmCenter = image.Warp(new Leap.Vector(hSlope, vSlope, 0));

                    Leap.Vector tipCenter = finger.TipPosition;
                    hSlope = -(tipCenter.x + cameraOffset * (2 * image.Id - 1)) / tipCenter.y;
                    vSlope = tipCenter.z / tipCenter.y;
                    tipCenter = image.Warp(new Leap.Vector(hSlope, vSlope, 0));


                    Leap.Vector wristCenter = finger.Hand.WristPosition;
                    hSlope = -(wristCenter.x + cameraOffset * (2 * image.Id - 1)) / wristCenter.y;
                    vSlope = wristCenter.z / wristCenter.y;
                    wristCenter = image.Warp(new Leap.Vector(hSlope, vSlope, 0));


                    System.Drawing.Pen redPen = new System.Drawing.Pen(System.Drawing.Color.Red, 3);
                    System.Drawing.Pen greenPen = new System.Drawing.Pen(System.Drawing.Color.Green, 3);
                    System.Drawing.Pen bluePen = new System.Drawing.Pen(System.Drawing.Color.Blue, 3);

                    double angle;
                    byte newAngle;
                    // Create string to draw.
                    String drawString1 = "Hand Pitch: " + (finger.Hand.Direction.Pitch * 57.29).ToString("0°");
                    angle = finger.Hand.Direction.Pitch * 57.29;
                    newAngle= Util.GetApproximatedTranslation(angle,-70,70,(int)TopFlexerStages.Value,false);
                    
                    if(TopFlexerKinect.IsChecked.Value)
                    { 
                        myArm.SetTopFlexerValue(newAngle);
                        TopFlexerSlider.Value = newAngle;
                    }
                    String drawString2 = "Hand Yaw: " + (finger.Hand.Direction.Yaw * 57.29).ToString("0°");
                    String drawString3 = "Hand Roll: " + (finger.Hand.PalmNormal.Roll * 57.29).ToString("0°");
                    angle = finger.Hand.PalmNormal.Roll * 57.29;

                    newAngle = Util.GetApproximatedTranslation(angle, -130, 40, (int)TopRotatorStages.Value, true);

                    if (TopRotatorKinect.IsChecked.Value)
                    {
                        myArm.SetTopRotatorValue(newAngle);
                        TopRotatorSlider.Value = newAngle;
                    }
                    byte gripperValue = (byte) (finger.Hand.PinchStrength * 230);
                    gripperValue = Util.GetApproximatedTranslation(gripperValue, 0, 230, (int)GripperStages.Value, false);


                    if (GripperKinect.IsChecked.Value)
                    {
                        myArm.SetGripperValue(gripperValue);
                        GripperSlider.Value = gripperValue;
                    }

                    // Create font and brush.
                    System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
                    System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
                    System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();


                    using (var graphics = System.Drawing.Graphics.FromImage(clone))
                    {
                        graphics.DrawString(drawString1, drawFont, drawBrush, new System.Drawing.RectangleF(20.0F, 200.0F, 200.0F, 50.0F), drawFormat);
                        graphics.DrawString(drawString2, drawFont, drawBrush, new System.Drawing.RectangleF(20.0F, 160.0F, 200.0F, 50.0F), drawFormat);
                        graphics.DrawString(drawString3, drawFont, drawBrush, new System.Drawing.RectangleF(20.0F, 120.0F, 200.0F, 50.0F), drawFormat);
                        graphics.DrawLine(bluePen, palmCenter.x, palmCenter.y, tipCenter.x, tipCenter.y);
                        graphics.DrawLine(greenPen, tipCenter.x, tipCenter.y, wristCenter.x, wristCenter.y);
                        //graphics.DrawLine(greenPen, palmCenter.x, palmCenter.y, palmNormal.x, palmNormal.y);
                    }

                }


                hBitmap = clone.GetHbitmap(); 
                object source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()); 
                ImageSource imageSource = (ImageSource)source;
                LeapImage.Source = imageSource;
            }
            catch (Exception ex) { 
                Console.WriteLine(ex.Message); 
            }
            finally
            {
                DeleteObject(hBitmap);
                //timer.Stop();
            }
            // get the next image
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);
                            this.UpdateAngles(body, dc);
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        private void UpdateAngles(Body body, DrawingContext drawingContext)
        {
            if(LowerFlexerKinect.IsChecked.Value)
                UpdateAngle(body, drawingContext, JointType.SpineShoulder, JointType.ShoulderRight, JointType.ElbowRight);
            if (MiddleFlexerKinect.IsChecked.Value)
                UpdateAngle(body, drawingContext, JointType.ShoulderRight, JointType.ElbowRight, JointType.HandRight);
            //UpdateAngle(body, drawingContext, JointType.SpineShoulder, JointType.ShoulderRight, JointType.ElbowRight);

            if (LowerRotatorKinect.IsChecked.Value)
            {
                double distance = body.Joints[JointType.HandRight].Position.Z - body.Joints[JointType.HipRight].Position.Z;//, skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft]);


                if (distance < -0.5)
                    distance = -0.5;
                else if (distance > 0.5)
                    distance = 0.5;
                byte distanceB = (byte)((((distance + 0.5) * (230))));

                byte middleValue = 115;

                if (distanceB < middleValue)
                {
                    distanceB = (byte)(middleValue + (middleValue - distanceB));
                }
                else
                    distanceB = (byte)(middleValue - (distanceB - middleValue));



                FormattedText formattedText = new FormattedText(distanceB + "", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 18,
                    Brushes.White);


                drawingContext.DrawText(formattedText, new Point(0, 0));


                if (Math.Abs(LowerRotatorSlider.Value - distanceB) >= changeThreshold)
                {
                    myArm.SetLowerRotatorValue(distanceB);
                    LowerRotatorSlider.Value = distanceB;
                }
            }
        }

        byte changeThreshold = 5;
        private void UpdateAngle(Body body, DrawingContext drawingContext, JointType leftJoint, JointType centerJoint, JointType rightJoint)
        {
            byte angle = (byte)AngleBetweenJoints(body.Joints[leftJoint], body.Joints[centerJoint], body.Joints[rightJoint]);

            FormattedText formattedText = new FormattedText(angle.ToString() + "°", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), 18,
                Brushes.White);

            CameraSpacePoint position = body.Joints[centerJoint].Position;
            if (position.Z < 0)
            {
                position.Z = InferredZPositionClamp;
            }

            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
            drawingContext.DrawText(formattedText, new Point(depthSpacePoint.X, depthSpacePoint.Y));

            if(centerJoint == JointType.ShoulderRight)
                angle = Util.GetApproximatedTranslation(angle, 90, 240, 100, true);


            if (centerJoint == JointType.ElbowRight && Math.Abs(LowerRotatorSlider.Value - 130) > 30)
                angle = Util.GetApproximatedTranslation(angle, 0, 200, 100, true);

            if (centerJoint == JointType.ElbowRight && Math.Abs(MiddleFlexerSlider.Value - angle) >= changeThreshold)
            {
                myArm.SetMiddleFlexerValue(angle);
                MiddleFlexerSlider.Value = angle;
            }
            else if (centerJoint == JointType.ShoulderRight && Math.Abs(LowerFlexerSlider.Value - angle) >= changeThreshold)
            {
                myArm.SetLowerFlexerValue(angle);
                LowerFlexerSlider.Value = angle;
            }
        }
        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>  
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {

            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }
        /// <summary>\
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }


            // Remove the sample listener when done
            //controller.RemoveListener(listener);
            //controller.Dispose();

            myArm.SetInitialPosition();
            myArm.PowerServos(false);
            myArm.Connect(false,true);
        }


        /// <summary>
        /// Return the angle between 3 Joints
        /// </summary>
        /// <param name="j1"></param>
        /// <param name="j2"></param>
        /// <param name="j3"></param>
        /// <returns></returns>
        public static double AngleBetweenJoints(Joint j1, Joint j2, Joint j3)
        {
            try
            {
                double angle = Math.Atan2(j1.Position.Y - j2.Position.Y, j1.Position.X - j2.Position.X) - Math.Atan2(j3.Position.Y - j2.Position.Y, j3.Position.X - j2.Position.X);
                angle = angle * 360 / (2 * Math.PI);

                if (angle < 0)
                {
                    angle = angle + 360;
                }

                return angle;
               /* 
                Vector3 v1 = new Vector3(j1.Position.X - j2.Position.X, j1.Position.Y - j2.Position.Y, j1.Position.Z - j2.Position.Z);
                Vector3 v2 = new Vector3(j3.Position.X - j2.Position.X, j3.Position.Y - j2.Position.Y, j3.Position.Z - j2.Position.Z);

                double v1mag = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y + v1.Z * v1.Z);
                Vector3 v1norm = new Vector3(v1.X / v1mag, v1.Y / v1mag, v1.Z / v1mag);

                double v2mag = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y + v2.Z * v2.Z);
                Vector3 v2norm = new Vector3(v2.X / v2mag, v2.Y / v2mag, v2.Z / v2mag);

                double res = v1norm.X * v2norm.X + v1norm.Y * v2norm.Y + v1norm.Z * v2norm.Z;

                float sign = Math.Sign(res);



                return Math.Acos(res) * (180 / Math.PI);*/
            }
            catch { }

            return 180;
        }
        class Vector3
        {
            public double X, Y, Z;
            public Vector3(double X, double Y, double Z)
            {
                this.X = X; this.Y = Y; this.Z = Z;
            }
        }
        /// <summary>
        /// Euclidean norm of 3-component Vector
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private static double vectorNorm(double x, double y, double z)
        {

            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));

        }
        /*
        public float AngleBetweenTwoVectors(Vector3 vectorA, Vector3 vectorB)
        {
            
            float dotProduct = 0.0f;
            dotProduct = Vector3.Dot(vectorA, vectorB);

            return (float)Math.Acos(dotProduct);
        }
        */
        


        private void Connect_Clicked(object sender, RoutedEventArgs e)
        {
                myArm.Connect(Connect.IsChecked.Value,true);
        }

        private void TopFlexerKinect_Checked(object sender, RoutedEventArgs e)
        {
            TopFlexerSlider.IsEnabled = !TopFlexerSlider.IsEnabled;
            TopFlexerValue.IsEnabled = !TopFlexerValue.IsEnabled;
            if (!TopFlexerSlider.IsEnabled)
                TopFlexerImage.Source = new BitmapImage(new Uri("Images/LeapActive.png", UriKind.Relative));
            else
                TopFlexerImage.Source = new BitmapImage(new Uri("Images/LeapInactive.png", UriKind.Relative));
        }

        private void GripperKinect_Checked(object sender, RoutedEventArgs e)
        {
            GripperSlider.IsEnabled = !GripperSlider.IsEnabled;
            GripperValue.IsEnabled = !GripperValue.IsEnabled;
            if (!GripperSlider.IsEnabled)
                GripperImage.Source = new BitmapImage(new Uri("Images/LeapActive.png", UriKind.Relative));
            else
                GripperImage.Source = new BitmapImage(new Uri("Images/LeapInactive.png", UriKind.Relative));
        
}

        private void LowerRotatorKinect_Checked(object sender, RoutedEventArgs e)
        {
            LowerRotatorSlider.IsEnabled = !LowerRotatorSlider.IsEnabled;
            LowerRotatorValue.IsEnabled = !LowerRotatorValue.IsEnabled;
            if (!LowerRotatorSlider.IsEnabled)
                LowerRotatorImage.Source = new BitmapImage(new Uri("Images/KinectActive.png", UriKind.Relative));
            else
                LowerRotatorImage.Source = new BitmapImage(new Uri("Images/KinectInactive.png", UriKind.Relative));
        
        }

        private void LowerFlexerKinect_Checked(object sender, RoutedEventArgs e)
        {
            LowerFlexerSlider.IsEnabled = !LowerFlexerSlider.IsEnabled;
            LowerFlexerValue.IsEnabled = !LowerFlexerValue.IsEnabled;
            if (!LowerFlexerSlider.IsEnabled)
                LowerFlexerImage.Source = new BitmapImage(new Uri("Images/KinectActive.png", UriKind.Relative));
            else
                LowerFlexerImage.Source = new BitmapImage(new Uri("Images/KinectInactive.png", UriKind.Relative));
        
        }

        private void MiddleFlexerKinect_Checked(object sender, RoutedEventArgs e)
        {
            MiddleFlexerSlider.IsEnabled = !MiddleFlexerSlider.IsEnabled;
            MiddleFlexerValue.IsEnabled = !MiddleFlexerValue.IsEnabled;
            if (!MiddleFlexerSlider.IsEnabled)
                MiddleFlexerImage.Source = new BitmapImage(new Uri("Images/KinectActive.png", UriKind.Relative));
            else
                MiddleFlexerImage.Source = new BitmapImage(new Uri("Images/KinectInactive.png", UriKind.Relative));
        
        }

        private void TopRotatorKinect_Checked(object sender, RoutedEventArgs e)
        {
            TopRotatorSlider.IsEnabled = !TopRotatorSlider.IsEnabled;
            TopRotatorValue.IsEnabled = !TopRotatorValue.IsEnabled;
            if (!TopRotatorSlider.IsEnabled)
                TopRotatorImage.Source = new BitmapImage(new Uri("Images/LeapActive.png", UriKind.Relative));
            else
                TopRotatorImage.Source = new BitmapImage(new Uri("Images/LeapInactive.png", UriKind.Relative));
        
        }

        private void GripperStages_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                GripperSlider.TickFrequency = 230 / GripperStages.Value;
            }
            catch { }
        }

        private void TopRotatorStages_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                TopRotatorSlider.TickFrequency = 230 / TopRotatorStages.Value;
            }
            catch { }
  
        }

        private void TopFlexerStages_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                TopFlexerSlider.TickFrequency = 230 / TopFlexerStages.Value;
            }
            catch { }
  
        }

        private void MiddleFlexerStages_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                MiddleFlexerSlider.TickFrequency = 230 / MiddleFlexerStages.Value;
            }
            catch { }
  
        }

        private void LowerFlexerStages_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                LowerFlexerSlider.TickFrequency = 230 / LowerFlexerStages.Value;
            }
            catch { }
  
        }

        private void LowerRotatorStages_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                LowerRotatorSlider.TickFrequency = 230 / LowerRotatorStages.Value;
            }
            catch { }
  
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            //this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
              //                                              : Properties.Resources.SensorNotAvailableStatusText;
        }

    }
}