using System;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RobotArmControlGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static SerialPort mySerialPort = new SerialPort();

        private delegate void NoArgDelegate();
        private delegate void OneArgDelegate(string arg);

        private enum ServoIDs
        {
            BASE_DIR     = 0,
            LOWER_HEIGHT = 1,
            MID_HEIGHT   = 2,
            TOP_HEIGHT   = 3,
            CLAW_TILT    = 4,
            CLAW_GRAB    = 5
        }

        public MainWindow()
        {
            InitializeComponent();

            ConnectButton_SetDisconnected(); // set button to indicate ready to connect

            ToggleTestsButton.Content = "Show tests";
            BeginTestButton.Content = "Start test";

            PacketsLabel.Visibility = Visibility.Collapsed;
            AntAckFailCountLabel.Visibility = Visibility.Collapsed;
            ResetAntAckButton.Visibility = Visibility.Collapsed;
            BeginTestButton.Visibility = Visibility.Collapsed;
            Application.Current.MainWindow.Height = 820;

        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ConnectButton.Content == "Connect")
            {
                string[] ports = SerialPort.GetPortNames();

                if (!ports.Contains(ComTextbox.Text)) // Check user provided a valid COM port
                {
                    DebugTextblock.Text = "Invalid COM port.  The system contains:\n";
                    foreach (string port in ports)
                    {
                        DebugTextblock.Text += port + '\n';
                    }
                    return;
                }

                // Set up the Serial Port
                mySerialPort.PortName     = ComTextbox.Text;
                mySerialPort.BaudRate     = 115200;
                mySerialPort.DataBits     = 8;
                mySerialPort.StopBits     = StopBits.One;
                mySerialPort.Parity       = Parity.None;
                mySerialPort.Handshake    = Handshake.None; // flow control
                mySerialPort.DtrEnable    = true;           // needs to be enabled as no handshake
                mySerialPort.RtsEnable    = true;           // "
                mySerialPort.ReadTimeout  = 500;
                mySerialPort.WriteTimeout = 500;

                try
                {
                    mySerialPort.Open();
                }
                catch (UnauthorizedAccessException ex)
                {
                    DebugTextblock.Text = ex.Message;
                    mySerialPort.Close();
                    return;
                }

                // Set UI
                ConnectButton_SetConnected(); // set button state to indicate we are connected

                // Register event handler for SerialDataReceived events
                mySerialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(SerialRecieveHandler);

            }
            else
            {
                if (mySerialPort.IsOpen == true)
                {
                    mySerialPort.Close(); // only close port if it is open as we get an exception
                }

                // Always set UI even if port was already closed
                ConnectButton_SetDisconnected(); // set button to indicate ready to connect
            }
        }


        private void ConnectButton_SetDisconnected()
        {
            DisableAllSliders();
            DebugTextblock.Text = "";
            ConnectButton.Content = "Connect"; // set button state to indicate we are disconnected
            ConnectButton.Background = Brushes.Green;

        }

        private void ConnectButton_SetConnected()
        {
            EnableAllSliders();
            DebugTextblock.Text = "";
            ConnectButton.Content = "Disconnect"; // set button state to indicate we are connected
            ConnectButton.Background = Brushes.Red;

        }

        private void SendFormattedCommand(ServoIDs ServoId, double RawValue)
        {
            if (mySerialPort.IsOpen == true)
            {
                /*
                    Format:
                    [0] = ServoID (0 - 5, see enum ServoIDs for mapping) 
                    [1] = "-"
                    [2..5] = Duty cycle (0500 - 2500, ALWAYS 4 characters, will be padded if not)
                    [6] = " "
                    [7] = "\r"
                    [8] = "\n"
                */
                mySerialPort.Write(((int)ServoId).ToString() + "-" + RawValue.ToString().PadLeft(4, '0').ToString() + " \r\n");
            }
            else
            {
                if (DebugTextblock != null) // because this function gets triggered before the textbox is initialised
                {
                    DebugTextblock.Text = "Not Connected, commands not being send";
                }
            }
        }

        private void BaseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SendFormattedCommand(ServoIDs.BASE_DIR, e.NewValue);
        }

        private void LowerHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SendFormattedCommand(ServoIDs.LOWER_HEIGHT, e.NewValue);
        }

        private void MidHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SendFormattedCommand(ServoIDs.MID_HEIGHT, e.NewValue);
        }

        private void TopHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SendFormattedCommand(ServoIDs.TOP_HEIGHT, e.NewValue);
        }

        private void ClawTiltSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SendFormattedCommand(ServoIDs.CLAW_TILT, e.NewValue);
        }

        private void ClawGrabSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SendFormattedCommand(ServoIDs.CLAW_GRAB, e.NewValue);
        }

        private void EnableAllSliders()
        {
            BaseSlider.IsEnabled        = true;
            LowerHeightSlider.IsEnabled = true;
            MidHeightSlider.IsEnabled   = true;
            TopHeightSlider.IsEnabled   = true;
            ClawGrabSlider.IsEnabled    = true;
            ClawTiltSlider.IsEnabled    = true;
        }
        private void DisableAllSliders()
        {
            BaseSlider.IsEnabled        = false;
            LowerHeightSlider.IsEnabled = false;
            MidHeightSlider.IsEnabled   = false;
            TopHeightSlider.IsEnabled   = false;
            ClawGrabSlider.IsEnabled    = false;
            ClawTiltSlider.IsEnabled    = false;
        }

        private void SerialRecieveHandler(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            string recieved_data = mySerialPort.ReadExisting();

            if (recieved_data == "TX_FAIL\r\n")
            {
                int count;

                // Because we are updating UI elements from outside of the main thread
                // https://stackoverflow.com/questions/9732709/the-calling-thread-cannot-access-this-object-because-a-different-thread-owns-it
                this.Dispatcher.Invoke(() => // lambda
                {
                    //DebugTextblock.Text = recieved_data; // for debug
                    count = int.Parse(AntAckFailCountLabel.Content.ToString());
                    AntAckFailCountLabel.Content = (count+1).ToString();

                });
            }
        }

        private void ToggleTestsButton_Click(object sender, RoutedEventArgs e)
        {
            // Set UI dependent on state
            if ((string)ToggleTestsButton.Content == "Show tests")
            {
                ToggleTestsButton.Content = "Hide tests";
                PacketsLabel.Visibility = Visibility.Visible;
                AntAckFailCountLabel.Visibility = Visibility.Visible;
                ResetAntAckButton.Visibility = Visibility.Visible;
                BeginTestButton.Visibility = Visibility.Visible;
                Application.Current.MainWindow.Height = 916;

            }
            else
            {
                ToggleTestsButton.Content = "Show tests";
                PacketsLabel.Visibility = Visibility.Collapsed;
                AntAckFailCountLabel.Visibility = Visibility.Collapsed;
                ResetAntAckButton.Visibility = Visibility.Collapsed;
                BeginTestButton.Visibility = Visibility.Collapsed;
                Application.Current.MainWindow.Height = 820;
            }
        }

        private void ResetAntAckButton_Click(object sender, RoutedEventArgs e)
        {
            AntAckFailCountLabel.Content = "0";
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ConnectButton.Content == "Disconnect")
            {
                DebugTextblock.Text = "Testing.....";

                // Start the test asynchronously
                NoArgDelegate fetcher = new NoArgDelegate(this.PerformTest);
                fetcher.BeginInvoke(null, null);
            }
            else
            {
                DebugTextblock.Text = "Disconnected, cannot start test";
            }
        }

        private void PerformTest()
        {
            string msg;
            int seconds = 0;
            int packetspersecond = 0;

            this.Dispatcher.Invoke(() =>
            {
                Int32.TryParse(TimeTextbox.Text, out seconds);
                Int32.TryParse(PacketsPerSecondTextbox.Text, out packetspersecond);
            });

            if (seconds == 0 || packetspersecond == 0)
            {
                msg = "ERROR - test requires inputs";
            }
            else
            {
                int lower = 500;
                int packetssent = 0;

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                while (stopwatch.Elapsed < TimeSpan.FromSeconds(seconds))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        SendFormattedCommand(ServoIDs.BASE_DIR, lower++);
                    });
                    packetssent++;

                    if (lower >= 2500) // reset lower if it reaches upper bounds so that pulsewidth will not exceed bounds
                        lower = 1500;

                    Thread.Sleep(1000 / packetspersecond);
                }

                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;

                // Format elapsed time ready to display.
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

                msg = "TEST FINISHED!  \nTime elapsed: " + elapsedTime + " \nPackets sent: " + packetssent;

            }
         
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, 
                                        new OneArgDelegate(UpdateUI),
                                        msg);
        }

        private void UpdateUI(String msg)
        {
            DebugTextblock.Text = msg;
        }

    }
}
