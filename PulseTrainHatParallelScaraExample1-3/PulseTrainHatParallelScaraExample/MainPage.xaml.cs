using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Test program for Pulse Train Hat http://www.pthat.com

namespace PulseTrainHatParallelScaraExample
{
    public sealed partial class MainPage : Page
    {
        private SerialDevice serialPort = null;
        private DataWriter dataWriteObject = null;
        private DataReader dataReaderObject = null;

        private ObservableCollection<DeviceInformation> listOfDevices;
        private CancellationTokenSource ReadCancellationTokenSource;

        private int Xset = 0;
        private int Yset = 0;
        private int Runset = 0;

        private double PI_Radians = 0;

        //----Left Motor
        //X and Y Start Position for Left Motor ARM1
        private double LeftMotorStartXARM1 = 0;
        private double LeftMotorStartYARM1 = 0;

        //X and Y End Position for Left Motor
        private double LeftMotorEndX = 0;
        private double LeftMotorEndY = 0;

        //Draw Position Variable for Left Motor ARM1
        private dynamic aaMotor1ARM1 = new System.Numerics.Vector2(0, 0);
        private dynamic bbMotor1ARM1 = new System.Numerics.Vector2(0, 0);
        
        //Draw Position Variable for Left Motor ARM2
        private dynamic aaMotor1ARM2 = new System.Numerics.Vector2(0, 0);
        private dynamic bbMotor1ARM2 = new System.Numerics.Vector2(0, 0);

        //Angle to be set for Left Motor
        private double LeftMotorDegreePlot = 0;

        //----Right Motor
        //X and Y Start Position for Right Motor ARM1
        private double RightMotorStartXARM1 = 0;
        private double RightMotorStartYARM1 = 0;

        //X and Y End Position for Right Motor
        private double RightMotorEndX = 0;
        private double RightMotorEndY = 0;

        //Draw Position Variable for Right Motor ARM1
        private dynamic aaMotor2ARM1 = new System.Numerics.Vector2(0, 0);
        private dynamic bbMotor2ARM1 = new System.Numerics.Vector2(0, 0);

        //Draw Position Variable for Right Motor ARM2
        private dynamic aaMotor2ARM2 = new System.Numerics.Vector2(0, 0);
        private dynamic bbMotor2ARM2 = new System.Numerics.Vector2(0, 0);

        //Angle to be set for Right Motor
        private double RightMotorDegreePlot = 0;


        //Calculation Variables
        private double xtarget = 0;
        private double ytarget = 0;
        private double theta1 = 0;
        private double theta2 = 0;
        private double a = 0;
        private double b = 0;
        private double d = 0;
        private dynamic tempcalc1;
        private dynamic tempcalc2;
        private double xprevious, yprevious;
        private int pulse1 = 0;
        private int pulse2 = 0;

        public MainPage()
        {
            this.InitializeComponent();

            // Calculate.IsEnabled = false;
            sendTextButton.IsEnabled = false;
            ResetCordinates.IsEnabled = false;

            //(distance between center of motor shafts)
            d = 100.0;

            //a = Arm1.Text (length of arm nearest rotor)
            a = Convert.ToDouble(Arm1.Text);

            //b = Arm2.text (length of forearm)
            b = Convert.ToDouble(Arm2.Text);

            //Set up PI calc for plotting grpahic arms
            PI_Radians = 1 / ((Math.Atan(1) * 4) / 180);

            //Set home position of X and Y positions
            xtarget = 100;
            ytarget = -32.706;
            Xtarget.Text = "0.00";
            Ytarget.Text = "0.00";
            PreviousXpos.Text = Convert.ToString(100);
            PreviousYpos.Text = Convert.ToString(-32.706);

            // Now lets plot arms for motor 1 on left with ARM1
            //Start aaX this is end of arm
            LeftMotorStartXARM1 = 50;

            // Start aaY this is end of arm
            LeftMotorStartYARM1 = a;

            // Finish bbX this is now middle of rotor shaft
            LeftMotorEndX = 150;

            // Finish bbY this is now middle of rotor shaft
            LeftMotorEndY = 90;

            //This is amount to turn arm1 in degrees to suit motor 1 on left
            LeftMotorDegreePlot = 0;
            LeftMotorStartXARM1 = Math.Cos(((LeftMotorDegreePlot) + 90) / PI_Radians) * (a) + LeftMotorEndX;
            LeftMotorStartYARM1 = Math.Sin(((LeftMotorDegreePlot) + 90) / PI_Radians) * (a) + LeftMotorEndY;
            LeftMotorDegreeout.Text = Convert.ToString(LeftMotorDegreePlot);

            // Now lets plot arms for motor 2 on Right with ARM1
            //Start aaX this is end of arm
            RightMotorStartXARM1 = LeftMotorStartXARM1 + d;

            // Start aaY this is end of arm
            RightMotorStartYARM1 = a;

            // Finish bbX this is now middle of rotor shaft
            RightMotorEndX = LeftMotorStartXARM1 + d;

            // Finish bbY this is now middle of rotor shaft
            RightMotorEndY = 90;

            //This is amount to turn arm1 in degrees to suit motor 1 on Right
            RightMotorDegreePlot = 0;
            RightMotorStartXARM1 = Math.Cos(((RightMotorDegreePlot) + 90) / PI_Radians) * (a) + RightMotorEndX;
            RightMotorStartYARM1 = Math.Sin(((RightMotorDegreePlot) + 90) / PI_Radians) * (a) + RightMotorEndY;
            RightMotorDegreeout.Text = Convert.ToString(RightMotorDegreePlot);

            // Do calc for mm resolution
            tempcalc1 = a + b;
            tempcalc1 = tempcalc1 * 2;
            tempcalc2 = tempcalc1 * Math.PI / Convert.ToSingle(PulserPerRev.Text);
            mmResolutionResult.Text = Convert.ToString(tempcalc2);

            // Do calc for degree resolution
            tempcalc1 = Convert.ToSingle(PulserPerRev.Text);
            tempcalc2 = 360 / tempcalc1;
            DegreeResolutionResult.Text = Convert.ToString(tempcalc2);

            //Do calc for pulses Left Motor
            tempcalc1 = Convert.ToSingle(LeftMotorAngleResult.Text);
            tempcalc2 = Convert.ToSingle(DegreeResolutionResult.Text);
            LeftMotorPulsesResult.Text = Convert.ToString(tempcalc1 / tempcalc2);

            //Do calc for pulses Right Motor
            tempcalc1 = Convert.ToSingle(RightAngleResult.Text);
            tempcalc2 = Convert.ToSingle(DegreeResolutionResult.Text);
            RightMotorPulsesResult.Text = Convert.ToString(tempcalc1 / tempcalc2);

            listOfDevices = new ObservableCollection<DeviceInformation>();
            ListAvailablePorts();
        }

        /// <summary>
        /// ListAvailablePorts
        /// - Use SerialDevice.GetDeviceSelector to enumerate all serial devices
        /// - Attaches the DeviceInformation to the ListBox source so that DeviceIds are displayed
        /// </summary>
        private async void ListAvailablePorts()
        {
            try
            {
                string aqs = SerialDevice.GetDeviceSelector();
                var dis = await DeviceInformation.FindAllAsync(aqs);

                status.Text = "Select a device and connect";

                for (int i = 0; i < dis.Count; i++)
                {
                    listOfDevices.Add(dis[i]);
                }

                DeviceListSource.Source = listOfDevices;
                comPortInput.IsEnabled = true;
                ConnectDevices.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        //***************************Drawing stuff starts from here***********************************

        private void canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            dynamic format = new Microsoft.Graphics.Canvas.Text.CanvasTextFormat();
            format.FontSize = 14;

            //Top left, length and height
            //print platform
            args.DrawingSession.DrawRectangle(100, 140 + 15, 200, 200, Colors.Black);

            //Print metal motor plate
            args.DrawingSession.DrawRectangle(120, 60, 160, 60, Colors.Black);

            //Center of plate where motor shafts area
            float tempcalc1 = Convert.ToSingle(LeftMotorEndX);

            //Draw Left Motor Circle
            args.DrawingSession.DrawCircle(tempcalc1, 90, 25, Colors.Green);
            tempcalc1 = Convert.ToSingle(LeftMotorEndX);
            args.DrawingSession.DrawCircle(tempcalc1, 90, 5, Colors.Green);
            tempcalc1 = Convert.ToSingle(LeftMotorStartXARM1);
            float tempcalc2 = Convert.ToSingle(LeftMotorStartYARM1);
            args.DrawingSession.DrawCircle(tempcalc1, tempcalc2, 12, Colors.Green);

            //draw x-y
            tempcalc1 = Convert.ToSingle(100 + xtarget);
            tempcalc2 = Convert.ToSingle(340 - ytarget + 15);
            args.DrawingSession.DrawCircle(tempcalc1, tempcalc2, 18, Colors.Red);

            tempcalc1 = Convert.ToSingle(100 + xtarget);
            tempcalc2 = Convert.ToSingle(340 - ytarget + 15);
            args.DrawingSession.DrawCircle(tempcalc1, tempcalc2, 12, Colors.Green);

            tempcalc1 = Convert.ToSingle(100 + xtarget);
            tempcalc2 = Convert.ToSingle(335 - ytarget);
            args.DrawingSession.DrawText("" + xtarget + "," + ytarget, tempcalc1, tempcalc2, Colors.Red, format);

            //Draw Motor 1 ARM1
            tempcalc1 = Convert.ToSingle(LeftMotorStartXARM1);
            tempcalc2 = Convert.ToSingle(LeftMotorStartYARM1);
            aaMotor1ARM1 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Start aaX aaY So Start of line point that is extended out
            tempcalc1 = Convert.ToSingle(LeftMotorEndX);

            bbMotor1ARM1 = new System.Numerics.Vector2(tempcalc1, 90);
            // Finish bbX bbY So Finish of line point  in middle of motor shaft
            args.DrawingSession.DrawLine(aaMotor1ARM1, bbMotor1ARM1, Colors.Blue);

            //Draw Motor 1 ARM2
            tempcalc1 = Convert.ToSingle(100 + xtarget);
            tempcalc2 = Convert.ToSingle(340 - ytarget + 15);
            aaMotor1ARM2 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Start aaX aaY So Start of line point that is extended out
            tempcalc1 = Convert.ToSingle(LeftMotorStartXARM1);
            tempcalc2 = Convert.ToSingle(LeftMotorStartYARM1);
            bbMotor1ARM2 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Finish bbX bbY So Finish of line point  in middle of motor shaft
            args.DrawingSession.DrawLine(aaMotor1ARM2, bbMotor1ARM2, Colors.Blue);

            //Draw Right Motor Circle
            tempcalc1 = Convert.ToSingle(RightMotorEndX);
            args.DrawingSession.DrawCircle(tempcalc1, 90, 25, Colors.Green);
            tempcalc1 = Convert.ToSingle(RightMotorEndX);
            args.DrawingSession.DrawCircle(tempcalc1, 90, 5, Colors.Green);
            tempcalc1 = Convert.ToSingle(RightMotorStartXARM1);
            tempcalc2 = Convert.ToSingle(RightMotorStartYARM1);
            args.DrawingSession.DrawCircle(tempcalc1, tempcalc2, 12, Colors.Green);

            //Draw Motor 2 ARM1
            tempcalc1 = Convert.ToSingle(RightMotorStartXARM1);
            tempcalc2 = Convert.ToSingle(RightMotorStartYARM1);
            aaMotor2ARM1 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Start aaX aaY So Start of line point that is extended out
            tempcalc1 = Convert.ToSingle(RightMotorEndX);
            bbMotor2ARM1 = new System.Numerics.Vector2(tempcalc1, 90);

            // Finish bbX bbY So Finish of line point  in middle of motor shaft
            args.DrawingSession.DrawLine(aaMotor2ARM1, bbMotor2ARM1, Colors.Blue);

            //Draw Motor 2 ARM2
            tempcalc1 = Convert.ToSingle(100 + xtarget);
            tempcalc2 = Convert.ToSingle(340 - ytarget + 15);
            aaMotor2ARM2 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Start aaX aaY So Start of line point that is extended out
            tempcalc1 = Convert.ToSingle(RightMotorStartXARM1);
            tempcalc2 = Convert.ToSingle(RightMotorStartYARM1);
            bbMotor2ARM2 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Finish bbX bbY So Finish of line point  in middle of motor shaft
            args.DrawingSession.DrawLine(aaMotor2ARM2, bbMotor2ARM2, Colors.Blue);
        }

        public void Calculateanglesandpulses()
        {
            Xtarget.Text = String.Format("{0:000.0000}", Convert.ToDouble(Xtarget.Text));
            Ytarget.Text = String.Format("{0:000.0000}", Convert.ToDouble(Ytarget.Text));

            // Do calcs
            PreviousXpos.Text = Convert.ToString(xtarget);
            PreviousYpos.Text = Convert.ToString(ytarget);
            xtarget = Convert.ToDouble(Xtarget.Text);
            ytarget = Convert.ToDouble(Ytarget.Text);
            a = Convert.ToDouble(Arm1.Text);
            b = Convert.ToDouble(Arm2.Text);
            theta1 = 0;
            theta2 = 0;
            AngleFromPosition5(a, b, xtarget, ytarget, ref theta1, ref theta2);

            //Okay we go the angles calculated, now lets draw the arms
            LeftMotorAngleResult.Text = Convert.ToString(theta1);
            RightAngleResult.Text = Convert.ToString(theta2);

            //Set angle for Left motor and Right Motor shaft
            LeftMotorDegreePlot = (theta1);
            RightMotorDegreePlot = (theta2);

            // Now plot arms for motor 1 on left
            LeftMotorDegreeout.Text = Convert.ToString(LeftMotorDegreePlot);
            LeftMotorStartXARM1 = Math.Cos(((LeftMotorDegreePlot) + 90) / PI_Radians) * (a) + LeftMotorEndX;
            LeftMotorStartYARM1 = Math.Sin(((LeftMotorDegreePlot) + 90) / PI_Radians) * (a) + LeftMotorEndY;

            tempcalc1 = Convert.ToSingle(LeftMotorStartXARM1);
            tempcalc2 = Convert.ToSingle(LeftMotorStartYARM1);
            aaMotor1ARM1 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Start aaX aaY So Start of line point that is extended out
            tempcalc1 = Convert.ToSingle(LeftMotorEndX);
            tempcalc2 = Convert.ToSingle(LeftMotorEndY);
            bbMotor1ARM1 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            // Now plot arms for motor 1 on left
            RightMotorDegreeout.Text = Convert.ToString(RightMotorDegreePlot);
            RightMotorStartXARM1 = Math.Cos(((RightMotorDegreePlot) + 90) / PI_Radians) * (a) + RightMotorEndX;
            RightMotorStartYARM1 = Math.Sin(((RightMotorDegreePlot) + 90) / PI_Radians) * (a) + RightMotorEndY;
            tempcalc1 = Convert.ToSingle(RightMotorStartXARM1);
            tempcalc2 = Convert.ToSingle(RightMotorStartYARM1);
            aaMotor2ARM1 = new System.Numerics.Vector2(tempcalc1, tempcalc2);
            tempcalc1 = Convert.ToSingle(RightMotorEndX);
            tempcalc2 = Convert.ToSingle(RightMotorEndY);

            // Start aaX aaY So Start of line point that is extended out
            bbMotor2ARM1 = new System.Numerics.Vector2(tempcalc1, tempcalc2);

            //Pulse calculation'
            a = Convert.ToDouble(Arm1.Text);
            b = Convert.ToDouble(Arm2.Text);

            tempcalc1 = a + b;
            tempcalc1 = tempcalc1 * 2;

            tempcalc2 = tempcalc1 * Math.PI / Convert.ToSingle(PulserPerRev.Text);

            mmResolutionResult.Text = Convert.ToString(tempcalc2);

            tempcalc1 = Convert.ToSingle(PulserPerRev.Text);
            tempcalc2 = 360 / tempcalc1;
            DegreeResolutionResult.Text = Convert.ToString(tempcalc2);

            tempcalc1 = Convert.ToSingle(LeftMotorAngleResult.Text);
            tempcalc2 = Convert.ToSingle(DegreeResolutionResult.Text);
            LeftMotorPulsesResult.Text = Convert.ToString(tempcalc1 / tempcalc2);

            tempcalc1 = Convert.ToSingle(RightAngleResult.Text);
            tempcalc2 = Convert.ToSingle(DegreeResolutionResult.Text);
            RightMotorPulsesResult.Text = Convert.ToString(tempcalc1 / tempcalc2);

            xprevious = Convert.ToDouble(PreviousXpos.Text); // 0.0;
            yprevious = Convert.ToDouble(PreviousYpos.Text); // 0.0;

            xtarget = Convert.ToDouble(Xtarget.Text);
            ytarget = Convert.ToDouble(Ytarget.Text);

            PulsesFromPosition3(a, b, xprevious, yprevious, xtarget, ytarget, ref pulse1, ref pulse2);

            FINALLeftMotorPulsesResult.Text = Convert.ToString(pulse1);

            FINALRightMotorPulsesResult.Text = Convert.ToString(pulse2);

            //This forces call to canvas_Draw to refresh it
            canvas2.Invalidate();
        }

        private void Calculate_Click_1(object sender, RoutedEventArgs e)
        {
            Calculateanglesandpulses();
            formatboxes();
            sendTextButton.IsEnabled = true;
        }

        private void ResetCordinates_Click(object sender, RoutedEventArgs e)
        {
            d = 100.0;            //(rotors separation)

            a = Convert.ToDouble(Arm1.Text);
            b = Convert.ToDouble(Arm2.Text);

            PI_Radians = 1 / ((Math.Atan(1) * 4) / 180);

            xtarget = 100;
            Xtarget.Text = "100";

            ytarget = -32.706;
            Ytarget.Text = "-32.706";

            // Now lets plot arms for motor 1 on left with ARM1
            LeftMotorStartXARM1 = 50;

            //Start aaX this is end of arm
            LeftMotorStartYARM1 = a;

            // Start aaY this is end of arm
            LeftMotorEndX = 150;

            // Finish bbX this is now middle of rotor shaft
            LeftMotorEndY = 90;

            // Finish bbY this is now middle of rotor shaft
            LeftMotorDegreePlot = 0;

            //This is amount to turn arm1 in degrees to suit motor 1 on left
            LeftMotorStartXARM1 = Math.Cos(((LeftMotorDegreePlot) + 90) / PI_Radians) * (a) + LeftMotorEndX;
            LeftMotorStartYARM1 = Math.Sin(((LeftMotorDegreePlot) + 90) / PI_Radians) * (a) + LeftMotorEndY;
            LeftMotorDegreeout.Text = Convert.ToString(LeftMotorDegreePlot);
            LeftMotorAngleResult.Text = "0";

            // Now lets plot arms for motor 2 on Right with ARM1
            RightMotorStartXARM1 = LeftMotorStartXARM1 + d;

            //Start aaX this is end of arm
            RightMotorStartYARM1 = a;

            // Start aaY this is end of arm
            RightMotorEndX = LeftMotorStartXARM1 + d;

            // Finish bbX this is now middle of rotor shaft
            RightMotorEndY = 90;

            // Finish bbY this is now middle of rotor shaft
            RightMotorDegreePlot = 0;

            //This is amount to turn arm1 in degrees to suit motor 1 on Right
            RightMotorStartXARM1 = Math.Cos(((RightMotorDegreePlot) + 90) / PI_Radians) * (a) + RightMotorEndX;
            RightMotorStartYARM1 = Math.Sin(((RightMotorDegreePlot) + 90) / PI_Radians) * (a) + RightMotorEndY;

            RightAngleResult.Text = "0";

            //This forces call to canvas_Draw to refresh it
            canvas2.Invalidate();
            Xtarget.Text = "0.00";
            Ytarget.Text = "0.00";
            PreviousXpos.Text = "100";
            PreviousXpos.Text = "-32.706";

            //enable
            Calculate.IsEnabled = true;
            sendTextButton.IsEnabled = false;
        }

        //Calculates the Angle from previous position to target position
        public void AngleFromPosition5(double a, double b, double xtarget, double ytarget, ref double theta1, ref double theta2)
        {
            Double ax1, ay1, ax2, ay2, dx1, dy1, dx2, dy2, l1, l2;

            a = Convert.ToDouble(Arm1.Text);
            b = Convert.ToDouble(Arm2.Text);
            ax1 = 50;
            ay1 = 270;
            ax2 = 150;
            ay2 = 270;
            theta1 = 0;
            theta2 = 0;
            dx1 = xtarget - ax1;
            dy1 = ay1 - ytarget;
            dx2 = xtarget - ax2;
            dy2 = ay2 - ytarget;
            l1 = Math.Sqrt(Math.Pow(dx1, 2) + Math.Pow(dy1, 2));
            l2 = Math.Sqrt(Math.Pow(dx2, 2) + Math.Pow(dy2, 2));
            theta1 = Math.Atan2(dx1, dy1) - Math.Acos((Math.Pow(l1, 2) - Math.Pow(b, 2) + Math.Pow(a, 2)) / (2 * a * l1));
            theta2 = Math.Atan2(dx2, dy2) + Math.Acos((Math.Pow(l2, 2) - Math.Pow(b, 2) + Math.Pow(a, 2)) / (2 * a * l2));
            theta1 = -180.0 / Math.PI * theta1;
            theta2 = -180.0 / Math.PI * theta2;
        }

        //calcualtes the pulses to move to the target position
        public void PulsesFromPosition3(double a, double b, double xprevious, double yprevious, double xtarget, double ytarget, ref int pulse1, ref int pulse2)
        {
            Double ttheta1, ptheta1, ttheta2, ptheta2;
            ttheta1 = 0.0;
            ptheta1 = 0.0;
            ttheta2 = 0.0;
            ptheta2 = 0.0;

            a = Convert.ToDouble(Arm1.Text);

            b = Convert.ToDouble(Arm2.Text);

            AngleFromPosition5(a, b, xprevious, yprevious, ref ptheta1, ref ptheta2);
            AngleFromPosition5(a, b, xtarget, ytarget, ref ttheta1, ref ttheta2);

            pulse1 = (int)(Convert.ToSingle(PulserPerRev.Text) / 360.0 * (ttheta1 - ptheta1));
            pulse2 = (int)(Convert.ToSingle(PulserPerRev.Text) / 360.0 * (ttheta2 - ptheta2));
        }

        /// <summary>
        /// closeDevice_Click: Action to take when 'Disconnect and Refresh List' is clicked on
        /// - Cancel all read operations
        /// - Close and dispose the SerialDevice object
        /// - Enumerate connected devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void closeDevice_Click(object sender, RoutedEventArgs e)
        {
            Disconnectserial();
        }

        private void Disconnectserial()
        {
            try
            {
                status.Text = "";
                CancelReadTask();
                CloseDevice();
                ListAvailablePorts();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        /// <summary>
        /// comPortInput_Click: Action to take when 'Connect' button is clicked
        /// - Get the selected device index and use Id to create the SerialDevice object
        /// - Configure default settings for the serial port
        /// - Create the ReadCancellationTokenSource token
        /// - Start listening on the serial port input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void comPortInput_Click(object sender, RoutedEventArgs e)
        {
            var selection = ConnectDevices.SelectedItems;

            if (selection.Count <= 0)
            {
                status.Text = "Select a device and connect";
                return;
            }

            DeviceInformation entry = (DeviceInformation)selection[0];

            try
            {
                serialPort = await SerialDevice.FromIdAsync(entry.Id);

                // Disable the 'Connect' button
                comPortInput.IsEnabled = false;
                Calculate.IsEnabled = true;
                sendTextButton.IsEnabled = false;
                ResetCordinates.IsEnabled = true;

                // Configure serial settings
                serialPort.WriteTimeout = TimeSpan.FromMilliseconds(30);
                serialPort.ReadTimeout = TimeSpan.FromMilliseconds(30);
                if (LowSpeedBaud.IsChecked == true)
                {
                    serialPort.BaudRate = 115200;
                }
                else
                {
                    serialPort.BaudRate = 806400;
                }
                serialPort.Parity = SerialParity.None;
                serialPort.StopBits = SerialStopBitCount.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = SerialHandshake.None;

                // Display configured settings
                status.Text = "Serial port configured successfully: ";
                status.Text += serialPort.BaudRate + "-";
                status.Text += serialPort.DataBits + "-";
                status.Text += serialPort.Parity.ToString() + "-";
                status.Text += serialPort.StopBits;

                // Set the RcvdText field to invoke the TextChanged callback
                // The callback launches an async Read task to wait for data
                rcvdText.Text = "Waiting for data...";

                // Create cancellation token object to close I/O operations when closing the device
                ReadCancellationTokenSource = new CancellationTokenSource();

                sendText1.Text = "";

                Listen();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                comPortInput.IsEnabled = true;
                sendTextButton.IsEnabled = false;
            }
        }

        private async void SendDataOut()
        {
            try
            {
                if (serialPort != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    dataWriteObject = new DataWriter(serialPort.OutputStream);

                    await WriteAsync();
                }
                else
                {
                    status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                status.Text = "Send Data Out: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }

        private void sendTextButton_Click(object sender, RoutedEventArgs e)
        {
            SetXaxis();

            Calculate.IsEnabled = false;
            sendTextButton.IsEnabled = false;
            ResetCordinates.IsEnabled = false;
        }

        private void SetXaxis()
        {
            //sets Xset to active and sends data out
            Xset = 1;
            Yset = 0;
            Runset = 0;
            SendDataOut();
        }

        private void SetYaxis()
        {
            Xset = 0;
            Yset = 1;
            Runset = 0;
            SendDataOut();
        }

        private void RunSet()
        {
            Xset = 0;
            Yset = 0;
            Runset = 1;
            SendDataOut();
        }

        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync()
        {
            Task<UInt32> storeAsyncTask;

            //if SetXaxis has been called
            if (Xset == 1)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(sendText1.Text);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    status.Text = sendText1.Text + ", ";
                    status.Text += "bytes written successfully!";
                }
            }

            if (Yset == 1)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(sendText2.Text);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    status.Text = sendText2.Text + ", ";
                    status.Text += "bytes written successfully!";
                }
            }

            if (Runset == 1)
            {
                // Load the text from the sendText input text box to the dataWriter object
                dataWriteObject.WriteString(sendText3.Text);

                // Launch an async task to complete the write operation
                storeAsyncTask = dataWriteObject.StoreAsync().AsTask();

                UInt32 bytesWritten = await storeAsyncTask;
                if (bytesWritten > 0)
                {
                    status.Text = sendText3.Text + ", ";
                    status.Text += "bytes written successfully!";
                }
            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            try
            {
                if (serialPort != null)
                {
                    dataReaderObject = new DataReader(serialPort.InputStream);

                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    status.Text = "Reading task was cancelled, closing device and cleaning up";
                    CloseDevice();
                }
                else
                {
                    status.Text = ex.Message;
                }
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            loadAsyncTask = dataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            UInt32 bytesRead = await loadAsyncTask;

            if (bytesRead > 0)
            {
                rcvdText.Text = dataReaderObject.ReadString(bytesRead);

                string input = rcvdText.Text;

                //Check if received message can be divided by 7 as our return messages are 7 bytes long
                if (input.Length % 7 == 0)
                {
                    //*********
                    for (int i = 0; i < input.Length; i += 7)
                    {
                        string sub = input.Substring(i, 7);

                        //Check if Set X Axis completed
                        if (sub == "CI00CX*")
                        {
                            SetYaxis();
                        }

                        //Check if Set Y Axis completed
                        if (sub == "CI00CY*")
                        {
                            RunSet();
                        }

                        //Check if X Axis completed amount of pulses
                        if (sub == "CI00SX*")
                        {
                            Calculate.IsEnabled = true;
                            sendTextButton.IsEnabled = false;
                            ResetCordinates.IsEnabled = true;
                        }

                        //Check if Y Axis completed amount of pulses
                        if (sub == "CI00SY*")
                        {
                            Calculate.IsEnabled = true;
                            sendTextButton.IsEnabled = false;
                            ResetCordinates.IsEnabled = true;
                        }
                    } // end of for loop
                } //endof checking length if

                status.Text = "bytes read successfully!";
            } //End of checking for bytes
        } //end of async read

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            if (serialPort != null)
            {
                serialPort.Dispose();
            }
            serialPort = null;

            comPortInput.IsEnabled = true;
            sendTextButton.IsEnabled = false;
            rcvdText.Text = "";
            listOfDevices.Clear();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Xset = 1;
            sendText1.Text = "N*";
            SendDataOut();
        }

        private void formatboxes()
        {
            //Check if Pulse Result is a minus number and set direction
            double checkminuspulse;
            checkminuspulse = Convert.ToDouble(FINALLeftMotorPulsesResult.Text);

            if (checkminuspulse < 0)
            {
                Xdir.Text = "0";
                checkminuspulse = System.Math.Abs(checkminuspulse);
                FINALLeftMotorPulsesResult.Text = Convert.ToString(checkminuspulse);
            }
            else
            {
                Xdir.Text = "1";
            }

            checkminuspulse = Convert.ToDouble(FINALRightMotorPulsesResult.Text);

            if (checkminuspulse < 0)
            {
                Ydir.Text = "0";

                checkminuspulse = System.Math.Abs(checkminuspulse);

                FINALRightMotorPulsesResult.Text = Convert.ToString(checkminuspulse);
            }
            else
            {
                Ydir.Text = "1";
            }

            BaseFrequency.Text = String.Format("{0:000000.000}", Convert.ToDouble(BaseFrequency.Text));

            //Calculate the Linear Interpolation so both motors stop and start at the same time

            //Check which motor has more pulses to go

            double xtargetCalc = Convert.ToDouble(FINALLeftMotorPulsesResult.Text);
            double ytargetCalc = Convert.ToDouble(FINALRightMotorPulsesResult.Text);

            if (xtargetCalc == ytargetCalc) //Is X and Y Pulses Equal
            {
                XFreq.Text = BaseFrequency.Text;
                YFreq.Text = BaseFrequency.Text;
            }
            else //Not Equal so now see which is more pulses
            {
                if (xtargetCalc > ytargetCalc) //Is X more pulses than Y
                {
                    XFreq.Text = BaseFrequency.Text;
                    tempcalc1 = xtargetCalc / ytargetCalc;
                    tempcalc2 = Convert.ToDouble(BaseFrequency.Text) / tempcalc1;
                    YFreq.Text = Convert.ToString(tempcalc2);
                }
                else //Y must be more pulses
                {
                    YFreq.Text = BaseFrequency.Text;
                    tempcalc1 = ytargetCalc / xtargetCalc;
                    tempcalc2 = Convert.ToDouble(BaseFrequency.Text) / tempcalc1;
                    XFreq.Text = Convert.ToString(tempcalc2);
                }
            }

            XFreq.Text = String.Format("{0:000000.000}", Convert.ToDouble(XFreq.Text));
            FINALLeftMotorPulsesResult.Text = String.Format("{0:0000000000}", Convert.ToDouble(FINALLeftMotorPulsesResult.Text));
            Xdir.Text = String.Format("{0:0}", Convert.ToDouble(Xdir.Text));
            RampDivide.Text = String.Format("{0:000}", Convert.ToDouble(RampDivide.Text));
            RampPause.Text = String.Format("{0:000}", Convert.ToDouble(RampPause.Text));
            EnablePolarity.Text = String.Format("{0:0}", Convert.ToDouble(EnablePolarity.Text));
            sendText1.Text = "I00CX" + XFreq.Text + FINALLeftMotorPulsesResult.Text + Xdir.Text + "1" + "1" + RampDivide.Text + RampPause.Text + "0" + EnablePolarity.Text + "*";

            YFreq.Text = String.Format("{0:000000.000}", Convert.ToDouble(YFreq.Text));
            FINALRightMotorPulsesResult.Text = String.Format("{0:0000000000}", Convert.ToDouble(FINALRightMotorPulsesResult.Text));
            Ydir.Text = String.Format("{0:0}", Convert.ToDouble(Ydir.Text));
            sendText2.Text = "I00CY" + YFreq.Text + FINALRightMotorPulsesResult.Text + Ydir.Text + "1" + "1" + RampDivide.Text + RampPause.Text + "0" + EnablePolarity.Text + "*";
        }
    }
}