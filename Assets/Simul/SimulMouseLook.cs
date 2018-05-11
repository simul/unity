using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

/// MouseLook rotates the transform based on the mouse delta.
/// Minimum and Maximum values can be used to constrain the possible rotation

/// To make an FPS style character:
/// - Create a capsule.
/// - Add the MouseLook script to the capsule.
///   -> Set the mouse look to use LookX. (You want to only turn character but not tilt it)
/// - Add FPSInputController script to the capsule
///   -> A CharacterMotor and a CharacterController component will be automatically added.

/// - Create a camera. Make the camera a child of the capsule. Reset it's transform.
/// - Add a MouseLook script to the camera.
///   -> Set the mouse look to use LookY. (You want the camera to tilt up and down like a head. The character already turns.)
[AddComponentMenu("Camera-Control/Simul Mouse Look")]
[ExecuteInEditMode]
public class SimulMouseLook : MonoBehaviour
{
#if UNITY_EDITOR
	public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public bool workWhenPlaying = true;
	public float sensitivityX = 0.15F;
	public float sensitivityY = 0.15F;
	public float TiltTurn=0.0F;
	public float minimumX = -360F;
	public float maximumX = 360F;

	public float minimumY = -60F;
	public float maximumY = 60F;

	public float damping = 0.0F;
	void Start ()
	{
		// Make the rigid body not change rotation
		if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;
	}
	Vector2 lastPos;
	Vector3 spd;
	Vector3 newspd;
	void OnGUI()
	{
		if (Application.isPlaying && !workWhenPlaying)
			return;
		if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
		{
			EditorUtility.SetDirty(this); // this is important, if omitted, "Mouse down" will not be display
		}
		else if (Event.current.type == EventType.MouseDown)
		{
			lastPos = Event.current.mousePosition;
		}
		else if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
		{
			Vector3 diff = Event.current.mousePosition - lastPos;
			newspd = new Vector3(diff.x * sensitivityX, diff.y * sensitivityY,0);
			lastPos = Event.current.mousePosition;
			if (!Application.isPlaying)
			{
				ApplyRotation(newspd);
				SceneView.RepaintAll();
			}
		}
	}
	void ApplyRotation(Vector3 spd)
	{
		float rotationX = transform.localEulerAngles.y;
		float rotationY = -transform.localEulerAngles.x;
		float tiltZ		= transform.localEulerAngles.z;
		if(tiltZ>180.0F)
			tiltZ-=360.0F;
		tiltZ*=0.99F;
		if (axes == RotationAxes.MouseX || axes == RotationAxes.MouseXAndY)
			rotationX += spd.x;

		if (axes == RotationAxes.MouseY || axes == RotationAxes.MouseXAndY)
		{
			rotationY -= spd.y;
			//rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
		}
		tiltZ += spd.z;
		transform.localEulerAngles = new Vector3(-rotationY, rotationX, tiltZ);
	}
	void Update()
	{
		if (Application.isPlaying)
		{
			if (workWhenPlaying)
			{
				if (TiltTurn > 0.0F)
					newspd.z = -TiltTurn * newspd.x;
				spd *= (damping);
				spd += (1.0F - damping) * newspd;
				ApplyRotation(spd);
				newspd *= 0;
			}
		}
		else
		{
			spd = newspd;
		}
	}
#endif
}