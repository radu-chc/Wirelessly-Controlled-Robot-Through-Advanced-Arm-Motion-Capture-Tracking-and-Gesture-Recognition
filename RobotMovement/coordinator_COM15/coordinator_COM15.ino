// COORDINATOR - Receives pitch and roll


#include <SPI.h>

//Add the SdFat Libraries
#include <SdFat.h>
#include <SdFatUtil.h>

//and the MP3 Shield Library
#include <SFEMP3Shield.h>

SdFat sd;

/**
 * \brief Object instancing the SFEMP3Shield library.
 *
 * principal object for handling all the attributes, members and functions for the library.
 */
SFEMP3Shield MP3player;

#define LEFT_PWD 5 //support analogwrite modulation
#define LEFT_DIR 3 

#define RIGHT_PWD 10 //support analogwrite modulation
#define RIGHT_DIR 4

#define LED_PIN 13 


static short velocity = 100;
int byteReceived;

bool blinkState = false;
long randNumber;
void setup() {
  // initialize serial communication:
  Serial.begin(9600);
   
  //Initialize the SdCard.
  if(!sd.begin(SD_SEL, SPI_FULL_SPEED)) sd.initErrorHalt();
  // depending upon your SdCard environment, SPI_HAVE_SPEED may work better.
  if(!sd.chdir("/")) sd.errorHalt("sd.chdir");

   
  //pinMode(LED_PIN, OUTPUT);   
  
  pinMode(LEFT_PWD, OUTPUT);
  pinMode(LEFT_DIR, OUTPUT);
   
  pinMode(RIGHT_PWD, OUTPUT);
  pinMode(RIGHT_DIR, OUTPUT);
 
  
  analogWrite(LEFT_PWD, 0);
  analogWrite(RIGHT_PWD, 0);  
  
  
  //Initialize the MP3 Player Shield
  MP3player.begin(); 
  MP3player.setVolume(35,35); 
  playMusic(1); 
  randomSeed(analogRead(0));
}

int songWait = 0; // noLoops to wait before next song

void loop() {
  
  
  
  // see if there's incoming serial data:
  if (Serial.available() > 0) {
    
    if(!MP3player.isPlaying())
    {
      randNumber = random(3, 350);
      
      if(randNumber < 9)
        playMusic(randNumber);
    }
    // read pitch
    //pitch = Serial.read();
    byteReceived = Serial.read();
    // if it's a capital H (ASCII 72), turn on the LED:
    if(byteReceived == 'L')
      {
        analogWrite(LEFT_PWD, velocity);  
        digitalWrite(LEFT_DIR, HIGH);
        analogWrite(RIGHT_PWD, velocity);  
        digitalWrite(RIGHT_DIR, LOW);    
      }
      else if(byteReceived == 'R')
      {
        analogWrite(LEFT_PWD, velocity);  
        digitalWrite(LEFT_DIR, LOW); 
        analogWrite(RIGHT_PWD, velocity);  
        digitalWrite(RIGHT_DIR, HIGH); 
      }
      else if(byteReceived == 'F')
      {
        
        analogWrite(LEFT_PWD, velocity);  
        digitalWrite(LEFT_DIR, HIGH);
        analogWrite(RIGHT_PWD, velocity);  
        digitalWrite(RIGHT_DIR, HIGH);    
      }
      else if(byteReceived == 'B')
      {
        analogWrite(LEFT_PWD, velocity);  
        digitalWrite(LEFT_DIR, LOW); 
        analogWrite(RIGHT_PWD, velocity);  
        digitalWrite(RIGHT_DIR, LOW); 
      }
      else if(byteReceived == 'S')
      {
        analogWrite(LEFT_PWD, 0); 
        analogWrite(RIGHT_PWD, 0); 
      }
      
      
      
      else if(byteReceived == 'a')
      { 
          playMusic(1); 
          songWait = 5;
      }
      
      else if(byteReceived == 's')
      {
          MP3player.stopTrack();       
      }
      
      if(songWait > 0)
        songWait --;
        
      }
    }

void playMusic(int16_t fn_index)
{
    SdFile file;
    char filename[13];
    sd.chdir("/",true);
    uint16_t count = 1;
    while (file.openNext(sd.vwd(),O_READ))
    {
      file.getFilename(filename);
      if ( isFnMusic(filename) ) {

        if (count == fn_index) {
          //Serial.print(F("Index "));
          //SerialPrintPaddedNumber(count, 5 );
          //Serial.print(F(": "));
          //Serial.println(filename);
          //Serial.print(F("Playing filename: "));
          //Serial.println(filename);
          
          //check result, see readme for error codes.
          if(MP3player.isPlaying())
            MP3player.stopTrack();
          
          delay(20);
          
          MP3player.playMP3(filename);
          break;
          
        }
        count++;
      }
      file.close();
    }
}
