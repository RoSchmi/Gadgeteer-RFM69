// Copyright RoSchmi Version 1.1, Date 06.01.2017
// NETMF 4.3 GHI SDK 2016 R1
// Compiled on Visual Studio 2013 (VS 2015 doesn't support Gadgeteer)
// This is an example how NETMF Gadgeteer Mainboards can receive and transmit data using the RFM69 radio
// The Class RFM69_Gadgeteer.cs is a driver to accomplish the communication between Gadgeteer Mainboard 
// and the RFM69 radio (https://learn.adafruit.com/adafruit-rfm69hcw-and-rfm96-rfm95-rfm98-lora-packet-padio-breakouts/overview)
// The driver is an adaption of Felix Rusu's driver for Arduino like Moteino boards to NETMF/Gadgeteer boards.
// In this example the Adafruit RFM69HCW Radio is connected with the Gadgeteer Spider Mainboard Socket 9 as shown below.
//
//     Adafruit RFM69HCW Radio         Gadgeteer Socket 9
//
//                   RST  ---------------- Pin 4  (Reset)
//                   CS   ---------------- Pin 6  (SPI-CS)
//                   MOSI ---------------- Pin 7  (SPI_MOSI)
//                   MISO ---------------- Pin 8  (SPI_MISO)
//                   SCK  ---------------- Pin 9  (SPI_SCK)
//                   G0   ---------------- Pin 3  (Int)
//                   GND  ---------------- Pin 10 GND
//                   VIn  ---------------- Pin 1  3V3
//
//          Between GND and Vin at the RFM69 a 100 nF Capacitor is mounted 
//          (prevents reboots of the Gadgeteer Mainboard caused by high current peaks)
//
// As the remote device a Moteino of  https://lowpowerlab.com or an 
// Adafruit Feather M0 RFM69 HCW https://learn.adafruit.com/adafruit-feather-m0-radio-with-rfm69-packet-radio/overview was used
// Felix Rusu of lowpowerlab.com wrote the driver for the Moteino and they maintain an excellent forum.
// On their page are tutorials showing how to set up the Moteino (https://lowpowerlab.com/guide/moteino-programming/arduinoide/)
// The Moteino is a 3.3 V Arduino Clone board (Atmel ATMega328P) with a mounted RFM69 radio and optional a Serial Flash 4Mbit chip.
// To program the board a FTDI Adapter (USB-Serial bridge) is needed. An FTDI Adapater to work out of the box can be ordered from lowpowerlab.com
// Adafruit have excellent tutorials as well, but with the Adafruit Feather M0 RFM69 HCW I had some minor issues with the Lowpowerlab RFM69 driver
// (https://github.com/LowPowerLab/RFM69)

// Description of the RFM69_Gadgeteer driver:
// As a first step after the declaration the Constructor is called with two obligatory parameters (Gadgeteer Socket No and type of the used RFM69 radio).
// The other parameters are optional and only for convenience to give some informations about the origin and type of the data that come with the
// MessageReceived event.
// As a second step (after the radio.hardReset) the driver instance is initialized with the parameters: Frequency, Node-ID and Network-Id.
// All radios which want to talk to each other must have the same Network-ID, but every device in the network must have it's
// own Node-ID which is different from the Node-IDs of other RFM69 radios.
// Then there are some calls concerning the "Send Power" of the RFM69. They should be self-explaining.
// This is followed by the radio.encrypt(...) command. The parameter can be null (for no encryption) or a string of exactly 16 characters/bytes, which 
// have to be exactly the same for all nodes.
// If the automatic transmission control feature shall be activated, use the command, e.g. radio.enableAutoPower(-65).
// This ensures that the radio does not always send with a high output but stepwise reduces sending power to the low border of being understood
// by the addressed remote node. E.g it begins with high power so that the receiver gets a strong signal RSSI of lets say -30db.
// The remote node gives a feedback of the RSSI strength in its ACK response to the sender, which in turn reduces its sending power
// stepwise until the RSSI strength which was passed as the parameter of the radio.enableAutoPower command (here: -65) is reached.
//
// Messages from other nodes to this node come with the eventArgs of the radio.MessageReceived Event.
//        
// Sending a message to another node is accomplished by calling the
// radio.asyncSendWithRetry(..) command. The first parameter is the node-ID of the recipient, the second parameter
// is a string or byteArry with a length of up to 60 bytes (more is cut), the third parameter is the number of send retries, the fourth the time
// in ms that each try waits for an ACK and the fifth parameter is a timeStamp.
// These send entities (containing the values of the parameters) are written to a queue, from where they are fetched in the drivers send/receive loop (separate thread)
// to be send to the recipient. So the main loop of the program is not blocked for the time of sending a message and waiting for an ACK. Not more than one package per second is sent. 
// When the recipient sended an ACK, the radio.ACKReturned Event is fired which holds some informations in its eventArgs.
// In this example messages are sent on button.press or by timer action (timer must be started in the Program.Started method).
// There is a third event (radio.ReceiveLoopIteration event) which is only there to, for example, toggle a LED to show that the send/receive loop
// is running.
// The multicolorLED is only used in this program to asynchronously signal certain actions in the course of the program. 
//
// There are two Arduino sketches included in this project (sketch_Moteino_868_Transmitter.ino and sketch_Moteino_868_Receiver)
// these sketches can be loaded on a Moteino to serve as a peer for the Gadgeteer device.

//#define DebugPrint

using System;
using System.Collections;
using System.Text;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;
using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using Gadgeteer.Modules.RoSchmi;


namespace RFM69_Gadgeteer_App1
{
    public partial class Program
    {
        // ****************** Parameters which have to be set by the user ********************************

        //Match frequency to the hardware version of the RFM69 radio
        private const string ENCRYPTKEY = "sampleEncryptKey"; //exactly the same 16 characters/bytes on all nodes!
        private const bool IS_RFM69HCW = true; // set to 'true' if you are using an RFM69HCW module

        private RFM69_Gadgeteer.Frequency FREQUENCY = RFM69_Gadgeteer.Frequency.RF69_868MHZ;
        private byte NETWORKID = 100;  // The same on all nodes that talk to each other
        private byte NODEID = 1;       // The ID of this node

        private const short LowBorderRSSI = -65;       // In automatic transmission control sending power is reduced to this border
        private const byte PowerLevel = 10;            // Sending-power starts with this Power Level (0 - 31)
        private const byte MaxPowerLevel = 10;         // Sending-power does not exceed this Power Level

        // ****************** End of parameters which have to be set by the user **************************
        
        GT.Timer sendTimer = new GT.Timer(7000);
        int packetnum = 0;
        string sendMessage = "Hello World from NETMF ";

        RFM69_Gadgeteer radio;

        #region ProgramStarted
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Program Started");
            sendTimer.Stop();
            
            // Only the parameters socket and IS_RFM69HCW are needed, the other parameters are optional, they are returned in the eventhandler, telling something about the message)
            // The following constructor call shows an example
            radio = new RFM69_Gadgeteer(9, IS_RFM69HCW, "Moteino-RFM69", "Livingroom", "°C", "StorageTable_01");

            radio.MessageReceived += radio_MessageReceived;             // Messages from other nodes come over this event
            button.ButtonPressed += button_ButtonPressed;
            sendTimer.Tick += sendTimer_Tick;             // Can be used to send messages in a certain time interval

            radio.ACKReturned += radio_ACKReturned;                     // Acknowledgements from other nodes come over this event
            radio.ReceiveLoopIteration += radio_ReceiveLoopIteration;   // Event that can be used for debugging or to toggle a LED showing that receive loop is running
            
            radio.hardReset();
           
            if (!radio.initialize(FREQUENCY, NODEID, NETWORKID))   // NODEID = 1, NETWORKID = 100
            {
                for (int i2 = 0; i2 < 5; i2++)
                {
                    multicolorLED.BlinkOnce(GT.Color.Red);
                    Thread.Sleep(2000);
                }
                Debug.Print("Error: Initialization failed");
            }
            else
            {
                multicolorLED.BlinkOnce(GT.Color.Green);
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
                + (FREQUENCY == RFM69_Gadgeteer.Frequency.RF69_433MHZ ? "433" : FREQUENCY == RFM69_Gadgeteer.Frequency.RF69_868MHZ ? "868" : "915") 
                + " MHz");
           
           //sendTimer.Start();  // must be activated if periodical messages are wanted
        }
        #endregion

        #region ButtonPressed --- used to send messages on button press
        void button_ButtonPressed(Button sender, Button.ButtonState state)
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

        #region sendTimer_Tick -- can be used to send messages in intervals
        void sendTimer_Tick(GT.Timer timer)
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
        void radio_MessageReceived(RFM69_Gadgeteer sender, RFM69_Gadgeteer.MessageReceivedEventArgs e)
        {
            multicolorLED.BlinkOnce(GT.Color.Orange);
            
            Debug.Print("Gadgeteer received: " + new string(Encoding.UTF8.GetChars(e.receivedData)));
            #if DebugPrint
                Debug.Print("\r\nMessage from: " + e.SensorLabel + " Node-ID: " + e.senderID + " Network-ID: " + e.networkID + " to: NETMF-Device at: " + e.ReadTime 
                    + " Location: " + e.SensorLocation + " Signal-Strength: [RSSI: " + e.RSSI + "]:\r\n "+ new string(Encoding.UTF8.GetChars(e.receivedData)));
            #endif
        }
        #endregion

        #region radio_ACKReturned (indicates the acknowledgement of the recipient)
        void radio_ACKReturned(RFM69_Gadgeteer sender, RFM69_Gadgeteer.ACK_EventArgs e)
        {
            if (e.ACK_Received)
            {
                try
                {
                    multicolorLED.BlinkOnce(GT.Color.Cyan);
                    Debug.Print("ACK received from Node: " + e.senderOfTheACK);
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
        void radio_ReceiveLoopIteration(RFM69_Gadgeteer sender, RFM69_Gadgeteer.ReceiveLoopEventArgs e)
        {
            if (e.EventID == 1)
            {
                multicolorLED.BlinkOnce(GT.Color.Green);
            }
            if (e.Iteration % 5 == 0)  // toggle every fifth iteration
            {
                
                button.ToggleLED();
            }
        }
        #endregion
    }
}










