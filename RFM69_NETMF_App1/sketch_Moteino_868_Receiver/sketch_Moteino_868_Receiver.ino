
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
#define NETWORKID     100  //the same on all nodes that talk to each other
#define NODEID        2  

#define ENABLE_ATC    //comment out this line to disable AUTO TRANSMISSION CONTROL
#define ATC_RSSI      -80

//Match frequency to the hardware version of the radio on your Feather
//#define FREQUENCY     RF69_433MHZ
#define FREQUENCY     RF69_868MHZ
//#define FREQUENCY      RF69_915MHZ
#define ENCRYPTKEY     "sampleEncryptKey" //exactly the same 16 characters/bytes on all nodes!
#define IS_RFM69HCW    true // set to 'true' if you are using an RFM69HCW module

//*********************************************************************************************
#define SERIAL_BAUD   115200

// for Wattuino
/*
#define RFM69_CS      10
#define RFM69_IRQ     2
#define RFM69_IRQN    0  // Pin 2 is IRQ 0!
#define RFM69_RST     9
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

//RFM69 radio = RFM69(RFM69_CS, RFM69_IRQ, IS_RFM69HCW, RFM69_IRQN);
//RFM69_ATC radio = RFM69_ATC(RFM69_CS, RFM69_IRQ, IS_RFM69HCW, RFM69_IRQN);

void setup() {
//while (!Serial); // wait until serial console is open, remove if not tethered to computer
  Serial.begin(SERIAL_BAUD);

  Serial.println("Moteino RFM69HCW Receiver");

  /*
  // Hard Reset the RFM module
  pinMode(RFM69_RST, OUTPUT);
  digitalWrite(RFM69_RST, HIGH);
  delay(100);
  digitalWrite(RFM69_RST, LOW);
  delay(100);
  */
  
  // Initialize radio
  radio.initialize(FREQUENCY,NODEID,NETWORKID);
  if (IS_RFM69HCW) 
  {
     radio.setHighPower();    // Only for RFM69HCW & HW!
  }
  radio.setPowerLevel(31); // power output ranges from 0 (5dBm) to 31 (20dBm)
  
  //radio.encrypt(ENCRYPTKEY);
  radio.encrypt(0);

  radio.enableAutoPower(-65);
  
  pinMode(LED, OUTPUT);

  

  Serial.println("Printing REG-initialization:");
Serial.print("REG_OPMODE ("); Serial.print(" 0x01 )"); Serial.println(radio.readReg(0x01));
Serial.print("REG_DATAMODUL ("); Serial.print(" 0x02 )"); Serial.println(radio.readReg(0x02));
Serial.print("REG_BITRATEMSB ("); Serial.print(" 0x03 )"); Serial.println(radio.readReg(0x03));
Serial.print("REG_BITRATELSB ("); Serial.print(" 0x04 )"); Serial.println(radio.readReg(0x04));
Serial.print("REG_FDEVMSB ("); Serial.print(" 0x05 )"); Serial.println(radio.readReg(0x05));
Serial.println(" ");
Serial.print("REG_PALEVEL ("); Serial.print(" 0x11 )"); Serial.println(radio.readReg(0x11));
Serial.print("REG_OCP ("); Serial.print(" 0x13 )"); Serial.println(radio.readReg(0x13));
Serial.print("REG_DIOMAPPING1 ("); Serial.print(" 0x25 )"); Serial.println(radio.readReg(0x25));
Serial.print("REG_DIOMAPPING2 ("); Serial.print(" 0x26 )"); Serial.println(radio.readReg(0x26));
Serial.print("REG_IRQFLAGS2 ("); Serial.print(" 0x28 )"); Serial.println(radio.readReg(0x28));
Serial.print("REG_RSSITHRESH ("); Serial.print(" 0x29 )"); Serial.println(radio.readReg(0x29));
Serial.print("REG_SYNCCONFIG ("); Serial.print(" 0x2E )"); Serial.println(radio.readReg(0x2E));
Serial.print("REG_SYNCVALUE1 ("); Serial.print(" 0x2F )"); Serial.println(radio.readReg(0x2F));
Serial.print("REG_SYNCVALUE2 ("); Serial.print(" 0x30 )"); Serial.println(radio.readReg(0x30));
Serial.print("REG_PACKETCONFIG1 ("); Serial.print(" 0x37 )"); Serial.println(radio.readReg(0x37));
Serial.print("REG_PAYLOADLENGTH ("); Serial.print(" 0x38 )"); Serial.println(radio.readReg(0x38));
Serial.print("REG_PACKETCONFIG2 ("); Serial.print(" 0x3D )"); Serial.println(radio.readReg(0x3D));
Serial.print("REG_TESTDAGC ("); Serial.print(" 0x6F )"); Serial.println(radio.readReg(0x6F));


/*
  Debug.Print("Printing REG-initialization:");
                Debug.Print("REG_OPMODE (0x" + REG_OPMODE.ToString("X2") + ") = 0x" + readReg(REG_OPMODE).ToString("X2"));
                Debug.Print("REG_DATAMODUL (0x" + REG_DATAMODUL.ToString("X2") + ") = 0x" + readReg(REG_DATAMODUL).ToString("X2"));
                Debug.Print("REG_BITRATEMSB (0x" + REG_BITRATEMSB.ToString("X2") + ") = 0x" + readReg(REG_BITRATEMSB).ToString("X2"));
                Debug.Print("REG_BITRATELSB (0x" + REG_BITRATELSB.ToString("X2") + ") = 0x" + readReg(REG_BITRATELSB).ToString("X2"));
                Debug.Print("REG_FDEVMSB (0x" + REG_FDEVMSB.ToString("X2") + ") = 0x" + readReg(REG_FDEVMSB).ToString("X2"));
                Debug.Print("REG_FDEVLSB (0x" + REG_FDEVLSB.ToString("X2") + ") = 0x" + readReg(REG_FDEVLSB).ToString("X2"));
                Debug.Print("REG_FRFMSB (0x" + REG_FRFMSB.ToString("X2") + ") = 0x" + readReg(REG_FRFMSB).ToString("X2"));
                Debug.Print("REG_FRFMID (0x" + REG_FRFMID.ToString("X2") + ") = 0x" + readReg(REG_FRFMID).ToString("X2"));
                Debug.Print("REG_FRFLSB (0x" + REG_FRFLSB.ToString("X2") + ") = 0x" + readReg(REG_FRFLSB).ToString("X2"));
                Debug.Print("");
                Debug.Print("REG_PALEVEL (0x" + REG_PALEVEL.ToString("X2") + ") = 0x" + readReg(REG_PALEVEL).ToString("X2"));
                Debug.Print("REG_OCP (0x" + REG_OCP.ToString("X2") + ") = 0x" + readReg(REG_OCP).ToString("X2"));
                Debug.Print("");
                Debug.Print("REG_RXBW (0x" + REG_RXBW.ToString("X2") + ") = 0x" + readReg(REG_RXBW).ToString("X2"));
                Debug.Print("");
                Debug.Print("REG_DIOMAPPING1 (0x" + REG_DIOMAPPING1.ToString("X2") + ") = 0x" + readReg(REG_DIOMAPPING1).ToString("X2"));
                Debug.Print("REG_DIOMAPPING2 (0x" + REG_DIOMAPPING2.ToString("X2") + ") = 0x" + readReg(REG_DIOMAPPING2).ToString("X2"));
                Debug.Print("");
                Debug.Print("REG_IRQFLAGS2 (0x" + REG_IRQFLAGS2.ToString("X2") + ") = 0x" + readReg(REG_IRQFLAGS2).ToString("X2"));
                Debug.Print("");
                Debug.Print("REG_RSSITHRESH (0x" + REG_RSSITHRESH.ToString("X2") + ") = 0x" + readReg(REG_RSSITHRESH).ToString("X2"));
                Debug.Print("REG_SYNCCONFIG (0x" + REG_SYNCCONFIG.ToString("X2") + ") = 0x" + readReg(REG_SYNCCONFIG).ToString("X2"));
                Debug.Print("REG_SYNCVALUE1 (0x" + REG_SYNCVALUE1.ToString("X2") + ") = 0x" + readReg(REG_SYNCVALUE1).ToString("X2"));
                Debug.Print("REG_SYNCVALUE2 (0x" + REG_SYNCVALUE2.ToString("X2") + ") = 0x" + readReg(REG_SYNCVALUE2).ToString("X2"));
                Debug.Print("REG_PACKETCONFIG1 (0x" + REG_PACKETCONFIG1.ToString("X2") + ") = 0x" + readReg(REG_PACKETCONFIG1).ToString("X2"));
                Debug.Print("REG_PAYLOADLENGTH (0x" + REG_PAYLOADLENGTH.ToString("X2") + ") = 0x" + readReg(REG_PAYLOADLENGTH).ToString("X2"));
                Debug.Print("REG_PACKETCONFIG2 (0x" + REG_PACKETCONFIG2.ToString("X2") + ") = 0x" + readReg(REG_PACKETCONFIG2).ToString("X2"));
                Debug.Print("REG_TESTDAGC (0x" + REG_TESTDAGC.ToString("X2") + ") = 0x" + readReg(REG_TESTDAGC).ToString("X2"));
         */
  Serial.print("\nListening at ");
  Serial.print(FREQUENCY==RF69_433MHZ ? 433 : FREQUENCY==RF69_868MHZ ? 868 : 915);
  Serial.println(" MHz");

#ifdef ENABLE_ATC
  Serial.println("RFM69_ATC Enabled (Auto Transmission Control)\n");
#endif
}

void loop() {
//check if something was received (could be an interrupt from the radio)
  if (radio.receiveDone())
  {
    //print message received to serial
    Serial.print('[');Serial.print(radio.SENDERID);Serial.print("] ");
    Serial.print((char*)radio.DATA);
    Serial.print("   [RX_RSSI:");Serial.print(radio.RSSI);Serial.print("]");

    //check if received message contains Hello World
    if (strstr((char *)radio.DATA, "Hello World"))
    {
      //check if sender wanted an ACK
      if (radio.ACKRequested())
      {
          
          unsigned long Start = millis();
          while(millis() - Start < 50);    // A delay is needed for NETMF (50 ms ?)
                                           // if ACK comes to early it is missed by NETMF
                           
          radio.sendACK();
          Serial.println(" - ACK sent");
      }
      Blink(LED, 40, 3); //blink LED 3 times, 40ms between blinks
    }  
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



