/* RFM69 library and code by Felix Rusu - felix@lowpowerlab.com
// Get libraries at: https://github.com/LowPowerLab/
// Make sure you adjust the settings in the configuration section below !!!
// **********************************************************************************
// Copyright Felix Rusu, LowPowerLab.com
// Library and code by Felix Rusu - felix@lowpowerlab.com
// **********************************************************************************
// License
// **********************************************************************************
// This program is free software; you can redistribute it 
// and/or modify it under the terms of the GNU General    
// Public License as published by the Free Software       
// Foundation; either version 3 of the License, or        
// (at your option) any later version.                    
//                                                        
// This program is distributed in the hope that it will   
// be useful, but WITHOUT ANY WARRANTY; without even the  
// implied warranty of MERCHANTABILITY or FITNESS FOR A   
// PARTICULAR PURPOSE. See the GNU General Public        
// License for more details.                              
//                                                        
// You should have received a copy of the GNU General    
// Public License along with this program.
// If not, see <http://www.gnu.org/licenses></http:>.
//                                                        
// Licence can be viewed at                               
// http://www.gnu.org/licenses/gpl-3.0.txt
//
// Please maintain this license information along with authorship
// and copyright notices in any redistribution of this code
// **********************************************************************************/

#include <RFM69.h>    //get it here: https://www.github.com/lowpowerlab/rfm69
#include <RFM69_ATC.h>
#include <SPIFlash.h>      //get it here: https://www.github.com/lowpowerlab/spiflash
#include <SPI.h>

//*********************************************************************************************
// *********** IMPORTANT SETTINGS - YOU MUST CHANGE/ONFIGURE TO FIT YOUR HARDWARE *************
//*********************************************************************************************
#define NETWORKID     100  // The same on all nodes that talk to each other
#define NODEID        2    // The unique identifier of this node
#define RECEIVER      1    // The recipient of packets

#define ENABLE_ATC    //comment out this line to disable AUTO TRANSMISSION CONTROL
#define ATC_RSSI      -80


//Match frequency to the hardware version of the radio on your Feather
//#define FREQUENCY     RF69_433MHZ
#define FREQUENCY     RF69_868MHZ
//#define FREQUENCY     RF69_915MHZ
#define ENCRYPTKEY    "sampleEncryptKey" //exactly the same 16 characters/bytes on all nodes!
#define IS_RFM69HCW   true // set to 'true' if you are using an RFM69HCW module

//*********************************************************************************************
#define SERIAL_BAUD   115200

/* for Feather 32u4 Radio
#define RFM69_CS      8
#define RFM69_IRQ     7
#define RFM69_IRQN    4  // Pin 7 is IRQ 4!
#define RFM69_RST     4
*/

// for Moteino
#define RFM69_CS      SS  // SS is the SPI slave select pin, for instance D10 on ATmega328
#define RFM69_IRQ     2
#define RFM69_IRQN    0  // Pin 2 is IRQ 0!

// for Feather M0 Radio
/*
#define RFM69_CS      8
#define RFM69_IRQ     3
#define RFM69_IRQN    3  // Pin 3 is IRQ 3!
#define RFM69_RST     4
*/

/* ESP8266 feather w/wing
#define RFM69_CS      2
#define RFM69_IRQ     15
#define RFM69_IRQN    digitalPinToInterrupt(RFM69_IRQ )
#define RFM69_RST     16
*/

/* Feather 32u4 w/wing
#define RFM69_RST     11   // "A"
#define RFM69_CS      10   // "B"
#define RFM69_IRQ     2    // "SDA" (only SDA/SCL/RX/TX have IRQ!)
#define RFM69_IRQN    digitalPinToInterrupt(RFM69_IRQ )
*/

// Feather m0 w/wing 
//#define RFM69_RST     11   // "A"
//#define RFM69_CS      10   // "B"
//#define RFM69_IRQ     6    // "D"
//#define RFM69_IRQN    digitalPinToInterrupt(RFM69_IRQ )


/* Teensy 3.x w/wing 
#define RFM69_RST     9   // "A"
#define RFM69_CS      10   // "B"
#define RFM69_IRQ     4    // "C"
#define RFM69_IRQN    digitalPinToInterrupt(RFM69_IRQ )
*/

/* WICED Feather w/wing 
#define RFM69_RST     PA4     // "A"
#define RFM69_CS      PB4     // "B"
#define RFM69_IRQ     PA15    // "C"
#define RFM69_IRQN    RFM69_IRQ
*/

#ifdef __AVR_ATmega1284P__
  #define LED           15 // Moteino MEGAs have LEDs on D15
  #define FLASH_SS      23 // and FLASH SS on D23
#else
  #define LED           9 // Moteinos have LEDs on D9
  #define FLASH_SS      8 // and FLASH SS on D8
#endif


int16_t packetnum = 0;  // packet counter, we increment per xmission

#ifdef ENABLE_ATC
  RFM69_ATC radio = RFM69_ATC(RFM69_CS, RFM69_IRQ, IS_RFM69HCW, RFM69_IRQN);
  #else
  RFM69 radio = RFM69(RFM69_CS, RFM69_IRQ, IS_RFM69HCW, RFM69_IRQN);
#endif

void setup() {
  delay(5000);
  //while (!Serial); // wait until serial console is open, remove if not tethered to computer
  Serial.begin(SERIAL_BAUD);

  Serial.println("RFM69HCW Transmitter");
  
  // Hard Reset the RFM module
  /*
  pinMode(RFM69_RST, OUTPUT);
  digitalWrite(RFM69_RST, HIGH);
  delay(100);
  digitalWrite(RFM69_RST, LOW);
  delay(100);
  */
  
  // Initialize radio
  radio.initialize(FREQUENCY,NODEID,NETWORKID);
  
  if (IS_RFM69HCW) {
    radio.setHighPower();    // Only for RFM69HCW & HW!
  }
  
  radio.setPowerLevel(31); // power output ranges from 0 (5dBm) to 31 (20dBm)
  
  //radio.encrypt(ENCRYPTKEY);
  radio.encrypt(0);
  radio.enableAutoPower(-65);
  
  pinMode(LED, OUTPUT);
  Serial.print("\nTransmitting at ");
  Serial.print(FREQUENCY==RF69_433MHZ ? 433 : FREQUENCY==RF69_868MHZ ? 868 : 915);
  Serial.println(" MHz");
}

void loop() 
{
  int8_t lastMode = radio._mode;      
  radio.setMode(RF69_MODE_STANDBY);   // Prevent the delay from beeing interrupted by interrupts from the radio, 
                                //otherweise the delay never returns 
   
  Serial.println(""); Serial.println("Going to delay 5000 ms");
  delay(5000);                  // Wait x seconds between transmits, could also 'sleep' here!
  radio.setMode(lastMode);            // Restore previous mode
  
  char radiopacket[20] = "Hello World #";
  itoa(packetnum++, radiopacket+13, 10);
  if (packetnum == 10)
  {packetnum = 0;}
  Serial.print("Sending "); Serial.println(radiopacket);
    
if (radio.sendWithRetry(RECEIVER, radiopacket, strlen(radiopacket), 3, 255)) 
{ //target node Id, message as string or byte array, message length, No of retries, waitTime for ACK
    Serial.println("OK");
   Blink(LED, 50, 3); //blink LED 3 times, 50ms between blinks
 }
 else
 {
  Serial.println("No Ack returned");
  }

  radio.receiveDone(); //put radio in RX mode
  Serial.flush(); //make sure all serial data is clocked out before sleeping the MCU
}

void Blink(byte PIN, byte DELAY_MS, byte loops)
{
  for (byte i=0; i<loops; i++)
  {
    digitalWrite(PIN,HIGH);
    delay(DELAY_MS);
    digitalWrite(PIN,LOW);
    delay(DELAY_MS);
  }
}





