/*

Author:
	Dr. Oisin Cawley
	Dept. of Computing
	Institute of Technology Carlow, Ireland.
	In support of the EU Erasmus+ project "Serious games and welfarre technology".

Hardware connections:

  Flex sensor:

    The flex sensor is the plastic strip with black stripes.
    It senses bending away from the striped side.

    The flex sensor has two pins, and since it's a resistor,
    the pins are interchangable.

    Connect one of the pins to ANALOG IN pin 0 on the Arduino.
    Connect the same pin, through a 10K Ohm resistor (brown
    black orange) to GND.
    Connect the other pin to 5V.
*/

// Define the analog input pin to measure flex sensor position:
const int flexpin = 0; 

void setup() 
{ 
  // Use the serial monitor window to help debug our sketch:

  Serial.begin(9600);
  // If required, the following line will wait until it detects a connection on the serial port.
  // This is sometimes useful when you do not want to miss any data being sent from the Arduino.
  // We will leave this out for now so that it starts communicating to our game straight away.
  // while (!Serial); //Wait for serial connection  
} 

void loop() 
{ 
  int flexPosition;   // Input value from the analog pin.
  int gamePosition;    // Output value for the game

  // Read the position of the flex sensor (0 to 1023):
  flexPosition = analogRead(flexpin);

  // The voltage divider circuit only returns a portion
  // of the 0-1023 range of analogRead().
  // The flex sensors we use are usually in the 600-900 range.

  // If you want, you can map the raw sensor data to a desired range as follows:
  // gamePosition = map(flexPosition, 710, 890, 0, 100);
  // gamePosition = constrain(gamePosition, 0, 100);
  // However, it may be best to do a callibration step within the game.
  // This will assist if you have to change the flex sensor, as each sesnsor has a slightly 
  // different output range.
  // For this examle we will simply use the raw sensor reading.
  gamePosition = flexPosition;
  
  // Because every flex sensor has a slightly different resistance,
  // the 600-900 range may not exactly cover the flex sensor's output. 
  // To help tune our program, we'll use the serial port to
  // print out our values to the serial monitor window:

// Uncomment these to debug.
//  Serial.print("sensor: ");
//  Serial.print(flexPosition);
//  Serial.print("  servo: ");
  Serial.println(gamePosition);

  // Note that all of the above lines are "print" except for the
  // last line which is "println". This puts everything on the
  // same line, then sends a final carriage return to move to
  // the next line.

  // After you upload the sketch, turn on the serial monitor
  // (the magnifying-glass icon to the right of the icon bar).
  // You'll be able to see the sensor values. Bend the flex sensor
  // and note its minimum and maximum values.

  // We will send a reading every 100 milliseconds.
  delay(100);  // wait 100ms between updates
} 
