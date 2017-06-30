using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.IO.Pipes;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Spritesheet for Flappy Bird found here: http://www.spriters-resource.com/mobile_phone/flappybird/sheet/59537/
/// Audio for Flappy Bird found here: https://www.youtube.com/watch?v=xY0sZUJWwA8
/// 
/// Adapted version for use in ExerGaming MOOC
/// 
/// Original: http://dgkanatsios.com/2014/07/02/a-flappy-bird-clone-in-unity-source-code-included-3/
/// 
/// </summary>
/// 
public class FlappyScript : MonoBehaviour
{

    public AudioClip FlyAudioClip, DeathAudioClip, ScoredAudioClip;
    public Sprite GetReadySprite;
    public float RotateUpSpeed = 1, RotateDownSpeed = 1;
    public GameObject IntroGUI, DeathGUI;
    public Collider2D restartButtonGameCollider;
    public float VelocityPerJump = 3;
    public float XSpeed = 1f;

	// SensorValues made public to show them in the inspector
	public string rawSensorValue = "";
	public int  currentSensorValue = 0;
	public int minSensorValue = 1024;
	public int maxSensorValue = -1;

	// Maximum and minimum y values foor bird.
	public float topPositionFlappy = 2f;
	public float bottomPositionFlappy = -1.5f;

	// bool to detect succes in sensorvalue conversion from string to int
	public bool sensorSucces;

	// After the player has selected the appropriate COM port for communication the game starts calibrating
	// The sensor should be in it's rest position at start.
	// After an increase of 'sensorTreshold' the calibration of the maximum sensorvalue starts.
	// The player should now apply maximum force to the sensor.
	// "calibrationTime" seconds after reaching a maximum sensorvalue the game starts.
	public int sensorTreshold = 20;
	public int calibrationTime = 3;

	// The sensorfacor is used to convert the minimum and maximum sensorvalues, measured during calibration to bird y positions
	public float sensorFactor = 200f;
	private float calibrationDoneTime;

	public Text debugText;
	public Dropdown comportSelect;

	public float debugValue = 0f;

	SerialPort stream;

	List<string> portOptions = new List<string>();

    // Use this for initialization
    void Start()
    {
		// Get a list of COM ports from the system (works on windows only)
		string[] ports = SerialPort.GetPortNames();

		if (ports.Length == 0) {
			// Nothing found, must be a Linux or MacOs system
			// On mac /dev/ returns a long list, search for a COM port starting with cu.modem 
			// Correct COM port is also visible in the Arduino Monitor.
			ports = System.IO.Directory.GetFiles ("/dev/");
			foreach (string port in ports) {
				if (port.IndexOf ("cu") > -1) {
					portOptions.Add (port);
				}
			}
		} else {
			
			foreach (string port in ports) {
				portOptions.Add (port);
			}
		}
		comportSelect.ClearOptions ();
		comportSelect.AddOptions(portOptions);

		calibrationDoneTime = 0;
    }

	public void setComPort (int comPortIndex) {
		// Initialize the COM port. Method is called from the DropDown comportSelect once the user makes a selection
		//
		// Remove dropdown, we're done
		comportSelect.gameObject.SetActive (false);
		// Convert the index returned to the name of the COM port
		string selectedComPort = portOptions [comPortIndex];
	
		if (selectedComPort.ToUpper().IndexOf ("COM") == 0 && selectedComPort.Length > 4) {
			// On Windows, if the number of the COM port is larger than 9 we need to place some backslashes and a dot in front of it to make it work.
			// Don't ask!
			// https://support.microsoft.com/en-us/help/115831/howto-specify-serial-ports-larger-than-com9
			selectedComPort = "\\\\.\\" + selectedComPort;
		}
		debugText.text = "Trying COMport: " + selectedComPort + "\n";
		// Open the port
		// Some error handling would be nice ;-)
		stream = new SerialPort (selectedComPort, 9600);
		stream.Open ();
		GameStateManager.GameState = GameState.Intro;
		stream.DiscardInBuffer ();
		stream.DiscardOutBuffer ();
		
	}

    FlappyYAxisTravelState flappyYAxisTravelState;

    enum FlappyYAxisTravelState
    {
        GoingUp, GoingDown
    }

    Vector3 birdRotation = Vector3.zero;
    // Update is called once per frame
    void Update()
	{
		//handle back key in Windows Phone
		if (Input.GetKeyDown (KeyCode.Escape))
			Application.Quit ();
		if (GameStateManager.GameState == GameState.Setup) {
			BoostOnYAxis (1.5f);
			return;
		}

		rawSensorValue = stream.ReadLine ();
		debugText.text = "Sensor says:\n" + rawSensorValue;

		if (GameStateManager.GameState == GameState.Intro) {
			MoveBirdOnXAxis ();
			// try to convert the message from the sensor to an integer
			sensorSucces = int.TryParse (rawSensorValue, out currentSensorValue);
			if (!sensorSucces) {
				return;
			}
			//
			// calibrate for lowest value
			if (currentSensorValue < minSensorValue) {
				minSensorValue = currentSensorValue;
			}
			// Find maximum sensor value in calibration period.
			// ToDo: Inform user about required action ;-)
			if (calibrationDoneTime < calibrationTime) {
				if (currentSensorValue > maxSensorValue) {
					maxSensorValue = currentSensorValue;
					// Reset calibration period on new maximum.
					calibrationDoneTime = 0;
				}
				// Has a significant increase in the sensor value occurred?
				if (maxSensorValue - currentSensorValue > sensorTreshold) {
					calibrationDoneTime += Time.deltaTime;
					if (calibrationDoneTime > calibrationTime) {
						// Done calibrating, calculate sensorFactor and start game
						GameStateManager.GameState = GameState.Playing;
						IntroGUI.SetActive (false);
						ScoreManagerScript.Score = 0;
						sensorFactor = (topPositionFlappy - bottomPositionFlappy) / (maxSensorValue - minSensorValue);
					}
				}
			}
			BoostOnYAxis(1.5f);
       
		}
        else if (GameStateManager.GameState == GameState.Playing)
        {
			MoveBirdOnXAxis();
			sensorSucces = int.TryParse(rawSensorValue, out currentSensorValue);
			if (!sensorSucces)
				return;
			// Modify position of bird depending on sensor values
			BoostOnYAxis((currentSensorValue - minSensorValue) * sensorFactor);
			debugValue = (currentSensorValue - minSensorValue);

        }

        else if (GameStateManager.GameState == GameState.Dead)
        {
            Vector2 contactPoint = Vector2.zero;

            if (Input.touchCount > 0)
                contactPoint = Input.touches[0].position;
            if (Input.GetMouseButtonDown(0))
                contactPoint = Input.mousePosition;

            //check if user wants to restart the game
            if (restartButtonGameCollider == Physics2D.OverlapPoint
                (Camera.main.ScreenToWorldPoint(contactPoint)))
            {
                GameStateManager.GameState = GameState.Intro;
                Application.LoadLevel(Application.loadedLevelName);
            }
        }

    }


    void FixedUpdate()
    {
        //just jump up and down on intro screen
        if (GameStateManager.GameState == GameState.Intro)
        {
            if (GetComponent<Rigidbody2D>().velocity.y < -1) //when the speed drops, give a boost
                GetComponent<Rigidbody2D>().AddForce(new Vector2(0, GetComponent<Rigidbody2D>().mass * 5500 * Time.deltaTime)); //lots of play and stop 
                                                        //and play and stop etc to find this value, feel free to modify
        }
        else if (GameStateManager.GameState == GameState.Playing || GameStateManager.GameState == GameState.Dead)
        {
            FixFlappyRotation();
        }
    }

    bool WasTouchedOrClicked()
    {
        if (Input.GetButtonUp("Jump") || Input.GetMouseButtonDown(0) || 
            (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Ended))
            return true;
        else
            return false;
    }

    void MoveBirdOnXAxis()
    {
        transform.position += new Vector3(Time.deltaTime * XSpeed, 0, 0);
    }

	void BoostOnYAxis(float newPosition)
    {

		float velocityChange = newPosition - GetComponent<Rigidbody2D> ().position.y + bottomPositionFlappy;
		Debug.Log (newPosition);
		if (velocityChange > 0) 
		GetComponent<Rigidbody2D>().velocity = new Vector2(0, velocityChange);
		//GetComponent<Rigidbody2D> ().position = new Vector2 (GetComponent<Rigidbody2D> ().position.x, velocityChange);
        //GetComponent<AudioSource>().PlayOneShot(FlyAudioClip);
    }



    /// <summary>
    /// when the flappy goes up, it'll rotate up to 45 degrees. when it falls, rotation will be -90 degrees min
    /// </summary>
    private void FixFlappyRotation()
    {
        if (GetComponent<Rigidbody2D>().velocity.y > 0) flappyYAxisTravelState = FlappyYAxisTravelState.GoingUp;
        else flappyYAxisTravelState = FlappyYAxisTravelState.GoingDown;

        float degreesToAdd = 0;

        switch (flappyYAxisTravelState)
        {
            case FlappyYAxisTravelState.GoingUp:
                degreesToAdd = 6 * RotateUpSpeed;
                break;
            case FlappyYAxisTravelState.GoingDown:
                degreesToAdd = -3 * RotateDownSpeed;
                break;
            default:
                break;
        }
        //solution with negative eulerAngles found here: http://answers.unity3d.com/questions/445191/negative-eular-angles.html

        //clamp the values so that -90<rotation<45 *always*
        birdRotation = new Vector3(0, 0, Mathf.Clamp(birdRotation.z + degreesToAdd, -90, 45));
        transform.eulerAngles = birdRotation;
    }

    /// <summary>
    /// check for collision with pipes
    /// </summary>
    /// <param name="col"></param>
    void OnTriggerEnter2D(Collider2D col)
    {
        if (GameStateManager.GameState == GameState.Playing)
        {
            if (col.gameObject.tag == "Pipeblank") //pipeblank is an empty gameobject with a collider between the two pipes
            {
                GetComponent<AudioSource>().PlayOneShot(ScoredAudioClip);
                ScoreManagerScript.Score++;
            }
            else if (col.gameObject.tag == "Pipe")
            {
                FlappyDies();
            }
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (GameStateManager.GameState == GameState.Playing)
        {
            if (col.gameObject.tag == "Floor")
            {
                FlappyDies();
            }
        }
    }

    void FlappyDies()
    {
        GameStateManager.GameState = GameState.Dead;
        DeathGUI.SetActive(true);
        GetComponent<AudioSource>().PlayOneShot(DeathAudioClip);
    }

}
