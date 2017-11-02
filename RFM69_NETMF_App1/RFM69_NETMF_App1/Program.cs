using System;
using Microsoft.SPOT;
using GHI.Pins;
using System.Threading;
using System.Text;
using Microsoft.SPOT.Hardware;
using RoSchmi;

namespace RFM69_NETMF_App1
{
    public class Program
    {
        public static ButtonNETMF myButton;

        // ****************** Parameters which have to be set by the user ********************************

        //Match frequency to the hardware version of the RFM69 radio
        private const string ENCRYPTKEY = "sampleEncryptKey"; //exactly the same 16 characters/bytes on all nodes!
        private const bool IS_RFM69HCW = true; // set to 'true' if you are using an RFM69HCW module
        private static RFM69_NETMF.Frequency FREQUENCY = RFM69_NETMF.Frequency.RF69_868MHZ;
        //private static RFM69_NETMF.Frequency FREQUENCY = RFM69_NETMF.Frequency.RF69_433MHZ;

        private static byte NETWORKID = 100;  // The same on all nodes that talk to each other
        private static byte NODEID = 1;       // The ID of this node

        private const short LowBorderRSSI = -65;       // In automatic transmission control sending power is reduced to this border
        private const byte PowerLevel = 20;            // Sending-power starts with this Power Level (0 - 31)
        private const byte MaxPowerLevel = 20;         // Sending-power does not exceed this Power Level

        // ****************** End of parameters which have to be set by the user **************************

        static Timer sendTimer; 

        static int packetnum = 0;
        static string sendMessage = "Hello World from NETMF ";

        public static RFM69_NETMF radio;
        public static void Main()
        {
            Debug.Print(Resources.GetString(Resources.StringResources.String1));

            //For Cobra III and button module on GXP Gadgeteer Bridge Socket 4 (R)
            myButton = new ButtonNETMF(G120.P2_12, G120.P2_6);
            myButton.Mode = ButtonNETMF.LedMode.OnWhilePressed;
            myButton.ButtonPressed += myButton_ButtonPressed;
            myButton.ButtonReleased += myButton_ButtonReleased;

            //For Cobra III on GXP Gadgeteer Bridge Socket 1 (SU)
            radio = new RFM69_NETMF(Microsoft.SPOT.Hardware.SPI.SPI_module.SPI2, G120.P0_5, G120.P0_4, G120.P4_28, true, "Moteino", "livingroom", "°C", "StorageTable_01");

            radio.ACKReturned += radio_ACKReturned;
            radio.MessageReceived += radio_MessageReceived;
            radio.ReceiveLoopIteration += radio_ReceiveLoopIteration;

            radio.hardReset();

            if (!radio.initialize(FREQUENCY, NODEID, NETWORKID))   // NODEID = 1, NETWORKID = 100
            {
                for (int i2 = 0; i2 < 5; i2++)
                {
                    //multicolorLED.BlinkOnce(GT.Color.Red);
                    Thread.Sleep(2000);
                }
                Debug.Print("Error: Initialization failed");
            }
            else
            {
                //multicolorLED.BlinkOnce(GT.Color.Green);
                Debug.Print("Initialization finished");
            }

            if (IS_RFM69HCW)
            {
                radio.setHighPower(true);
            }

            radio.setPowerLevel(PowerLevel, MaxPowerLevel); // power output ranges from 0 (5dBm) to 31 (20dBm), will not exceed MaxPowerLevel

            //radio.encrypt(ENCRYPTKEY);
            radio.encrypt(null);

            //leave the following line away if autoPower is not wanted
            radio.enableAutoPower(LowBorderRSSI);     // enable automatic transmission control, transmission power is stepwise reduced to the RSSI level passed as parameter 

            Debug.Print("\nListening at "
                + (FREQUENCY == RFM69_NETMF.Frequency.RF69_433MHZ ? "433" : FREQUENCY == RFM69_NETMF.Frequency.RF69_868MHZ ? "868" : "915")
                + " MHz");

            //activate the followig line in if periodic sends are wanted
            //sendTimer = new Timer(new TimerCallback(sendTimer_Tick), null, 7000, 7000);

            // finally: blinking a LED, just for fun
            var LED = new OutputPort(FEZCobraIII.Gpio.Led1, true);
            while (true)
            {
                LED.Write(!LED.Read()); // Invert
                Thread.Sleep(300);
            }
        }

        #region sendTimer_Tick -- can be used to send messages in intervals
        private static void sendTimer_Tick(object state)
        {
            packetnum++;
            string numAffix = packetnum.ToString();
            byte[] byteArray = Encoding.UTF8.GetBytes(sendMessage + numAffix);
            Debug.Print("\r\n\r\nGoing to send " + sendMessage + numAffix);
            if (radio.QueueHasFreePlaces())
            {

                radio.asyncSendWithRetry(2, byteArray, 3, 600, DateTime.Now);
            }
            else
            {
                Debug.Print("Send Queue was full");
                packetnum--;
            }
        }
        #endregion
        
        #region radio_MessageReceived (carries messages from other nodes)       
        static void radio_MessageReceived(RFM69_NETMF sender, RFM69_NETMF.MessageReceivedEventArgs e)
        {
           // multicolorLED.BlinkOnce(GT.Color.Orange);

            Debug.Print("Gadgeteer received: " + new string(Encoding.UTF8.GetChars(e.receivedData)));
            #if DebugPrint
                Debug.Print("\r\nMessage from: " + e.SensorLabel + " Node-ID: " + e.senderID + " Network-ID: " + e.networkID + " to: NETMF-Device at: " + e.ReadTime 
                    + " Location: " + e.SensorLocation + " Signal-Strength: [RSSI: " + e.RSSI + "]:\r\n "+ new string(Encoding.UTF8.GetChars(e.receivedData)));
            #endif
        }
        #endregion

        #region radio_ACKReturned (indicates the acknowledgement of the recipient)
        static void radio_ACKReturned(RFM69_NETMF sender, RFM69_NETMF.ACK_EventArgs e)
        {
            if (e.ACK_Received)
            {
                try
                {
                   // multicolorLED.BlinkOnce(GT.Color.Cyan);
            #if DebugPrint
                        Debug.Print("ACK received from Node: " + e.senderOfTheACK + "  ~ms " + (DateTime.Now - e.sendTime).Milliseconds + " after asyncSendWithRetry");
            #endif
                }
                catch { };
            }
            else
            {
                try
                {
            #if DebugPrint
                        Debug.Print("No ACK was returned for message: " + new string(Encoding.UTF8.GetChars(e.sentData)));
            #endif
                }
                catch { };
            }
        }
        #endregion

        #region radio_ReceiveLoopIteration --- event comes for every iteration of the receive loop (used as indicator / debugging)
        static void radio_ReceiveLoopIteration(RFM69_NETMF sender, RFM69_NETMF.ReceiveLoopEventArgs e)
        {
            if (e.EventID == 1)
            {
                //multicolorLED.BlinkOnce(GT.Color.Green);
            }
            if (e.Iteration % 5 == 0)  // toggle every fifth iteration
            {
                myButton.ToggleLED();
            }
            
        }
        #endregion

        #region ButtonPressed --- used to send messages on button press
        static void myButton_ButtonPressed(ButtonNETMF sender, ButtonNETMF.ButtonState state)
        {
            packetnum++;
            string numAffix = packetnum.ToString();
            byte[] byteArray = Encoding.UTF8.GetBytes(sendMessage + numAffix);
            Debug.Print("\r\n\r\nGoing to send " + sendMessage + numAffix);

            if (radio.QueueHasFreePlaces())
            {

                radio.asyncSendWithRetry(2, byteArray, 3, 800, DateTime.Now);
            }
            else
            {
                Debug.Print("Send Queue was full");
                packetnum--;
            }
        }
        #endregion

        #region ButtonReleased
        static void myButton_ButtonReleased(ButtonNETMF sender, ButtonNETMF.ButtonState state)
        { }
        #endregion

    }
}
