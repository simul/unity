using UnityEngine;
#if UNITY_PS4
using UnityEngine.PS4;
#endif
#if UNITY_PS5
using UnityEngine.PS5;
#endif

namespace simul
{
    public class GamePad : MonoBehaviour
	{

		public int playerId = -1;
		public Transform[] touches;
		public Color inputOn = Color.white;
		public Color inputOff = Color.grey;

		private int stickID;
		private bool hasSetupGamepad = false;
#if UNITY_PS4
		private PS4Input.LoggedInUser loggedInUser;
		private PS4Input.ConnectionType connectionType;
#endif

		// Touchpad variables
		private int touchNum, touch0x, touch0y, touch0id, touch1x, touch1y, touch1id;
		private int touchResolutionX, touchResolutionY, analogDeadZoneLeft, analogDeadZoneRight;
		private float touchPixelDensity;

		// Volume sampling variables
		private int qSamples = 1024; // array size
		private float rmsValue = 0f; // sound level - RMS
		private float[] samples = new float[1024]; // audio samples

		void Start()
		{
			// Stick ID is the player ID + 1
			stickID = playerId + 1;
			ToggleGamePad(false);
		}

		void Update()
		{
#if UNITY_PS4
			if (PS4Input.PadIsConnected(playerId))
			{
				// Set the gamepad to the start values for the player
				if (!hasSetupGamepad)
					ToggleGamePad(true);

				// Handle each part individually
				Thumbsticks();

				// Options button is on its own, so we'll do it here
				if (Input.GetKey((KeyCode)System.Enum.Parse(typeof(KeyCode), "Joystick" + stickID + "Button7", true)))
				{

					// Reset the gyro orientation to default
					PS4Input.PadResetOrientation(playerId);
				}
			}
#elif UNITY_PS5
			Thumbsticks();
#else
			if (Application.isConsolePlatform)
			{
			}
			else if (hasSetupGamepad)
				ToggleGamePad(false);
#endif
		}

		// Toggle the gamepad between connected and disconnected states
		void ToggleGamePad(bool active)
		{
			if (active)
			{
#if UNITY_PS4
				// Set 3D Text to whoever's using the pad
				loggedInUser = PS4Input.RefreshUsersDetails(playerId);
#endif
				hasSetupGamepad = true;
			}
			else
			{
				// Hide the touches
				if(touches!=null&&touches.Length>=2)
				{ 
					touches[0].gameObject.SetActive(false);
					touches[1].gameObject.SetActive(false);
				}
				hasSetupGamepad = false;
			}
		}

		void Touchpad()
		{

		}

		// Change the pitch and volume of an audio source, via the inputs of 
		// the touchpad, and play it through the controller speaker
		void TouchpadAudio(int maxX, int maxY, int posX, int posY)
		{
		}
		public Vector3 leftStick;
		public Vector3 rightstick;
		void Thumbsticks()
		{
#if UNITY_PS5
#else
			// Move the thumbsticks around
			leftStick = new Vector3(Input.GetAxis("leftstick" + stickID + "horizontal"),
																		  Input.GetAxis("leftstick" + stickID + "vertical"),
																		  0);

			rightstick = new Vector3(Input.GetAxis("rightstick" + stickID + "horizontal"),
																		  Input.GetAxis("rightstick" + stickID + "vertical"),
																		   0);
#endif
		}

		// Make the Cross, Circle, Triangle and Square buttons light up when pressed
		void InputButtons()
		{
			//Input.GetKey((KeyCode)Enum.Parse(typeof(KeyCode), "Joystick" + stickID + "Button0", true)))
		}

		// Make the DPad directions light up when pressed
		void DPadButtons()
		{
			//if(Input.GetAxis("dpad" + stickID + "_horizontal") > 0)
		}

		void TriggerShoulderButtons()
		{
			// Make the triggers light up based on how "pulled" they are
			//if(Input.GetAxis("joystick" + stickID + "_left_trigger") != 0)
		}

		// Get the volume being played in-game, and make the speaker light up based on the volume
		void Speaker()
		{
		}

		string PadConnectionType(int connectionType)
		{
			switch (connectionType)
			{
				case 0:
					return "Local";
				case 1:
					return "Remote Vita";
				case 2:
					return "Remote DS4";
				default:
					return "Invalid connection type";
			}
		}

		// Get a usable Color from an int
		Color GetPlayerColor(int colorId)
		{
			switch (colorId)
			{
				case 0:
					return Color.blue;
				case 1:
					return Color.red;
				case 2:
					return Color.green;
				case 3:
					return Color.magenta;
				default:
					return Color.black;
			}
		}

		//Get the volume from an attached audio source component
		void GetVolume()
		{
			if (GetComponent<AudioSource>().time > 0f)
			{
				GetComponent<AudioSource>().GetOutputData(samples, 0); // fill array with samples
				int i;
				float sum = 0f;

				for (i = 0; i < qSamples; i++)
					sum += samples[i] * samples[i]; // sum squared samples

				rmsValue = Mathf.Sqrt(sum / qSamples); // rms = square root of average

				rmsValue *= GetComponent<AudioSource>().volume;
			}
			else
				rmsValue = 0f;
		}
	}
}