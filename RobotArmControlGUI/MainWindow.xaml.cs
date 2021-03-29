using System;
using System.IO.Ports;
using System.Threading;
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

            // Initialise button content/colour
            ConnectButton.Content = "Connect";
            ConnectButton.Background = Brushes.Green;
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
                mySerialPort.RtsEnable    = true;           // ^
                mySerialPort.ReadTimeout  = 500;
                mySerialPort.WriteTimeout = 500;

                // Open the port (maybe handle exceptions?)
                mySerialPort.Open();

                // Set 
                DebugTextblock.Text = "";
                ConnectButton.Content = "Disconnect"; // set button state to indicate we are connected
                ConnectButton.Background = Brushes.Red;

            }
            else
            {
                if (mySerialPort.IsOpen == true)
                {
                    mySerialPort.Close();
                    DebugTextblock.Text = "";
                    ConnectButton.Content = "Connect"; // set button state to indicate we are disconnected
                    ConnectButton.Background = Brushes.Green;
                }
            }
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
    }
}
