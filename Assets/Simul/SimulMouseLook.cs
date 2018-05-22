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
    public float translationSpeed = 10.0F;
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
	Vector3 rot_speed;
	Vector3 new_rot_speed;
    Vector3 speed;
    Vector3 new_speed;
    void OnGUI()
	{
		if (Application.isPlaying && !workWhenPlaying)
			return;
        if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
        {
            EditorUtility.SetDirty(this); // this is important, if omitted, "Mouse down" will not be displayed
        }
        else if (Event.current.type == EventType.MouseDown)
        {
            lastPos = Event.current.mousePosition;
        }
        else if (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove)
        {
            Vector3 diff = Event.current.mousePosition - lastPos;
                if (Event.current.button == 0)
                {
				new_speed = 0.1f*new Vector3(diff.x, diff.y, 0);
				ApplyTranslation(new_speed);
                }
                else if (Event.current.button == 1)
                {
				new_speed*= 0;
                    ApplyRotation(new_rot_speed);
				new_rot_speed = new Vector3(diff.x, diff.y, 0);
                }
			lastPos = Event.current.mousePosition;
            if (!Application.isPlaying)
            {

                SceneView.RepaintAll();
            }
        }
        else if (Event.current.type == EventType.KeyDown)
        {
            if(Event.current.keyCode==KeyCode.W)
            {
                new_speed.z = 1.0F;
            }
            if (Event.current.keyCode == KeyCode.S)
            {
                new_speed.z = -1.0F;
            }
            if (Event.current.keyCode == KeyCode.A)
            {
                new_speed.x = -1.0F;
            }
            if (Event.current.keyCode == KeyCode.D)
            {
                new_speed.x = 1.0F;
            }
        }
        else if (Event.current.type == EventType.KeyUp)
        {
            if (Event.current.keyCode == KeyCode.W || Event.current.keyCode == KeyCode.S)
            {
                new_speed.z = 0.0F;
            }
            if (Event.current.keyCode == KeyCode.A || Event.current.keyCode == KeyCode.D)
            {
                new_speed.x = 0.0F;
            }
        }
    }
    void ApplyTranslation(Vector3 rspeed)
    {
        Vector3 up = transform.up;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        transform.position-=up*speed.y* translationSpeed;
        transform.position += right * speed.x* translationSpeed;
        transform.position += forward * speed.z * translationSpeed;
    }
    void ApplyRotation(Vector3 rot_speed)
	{
		float rotationX = transform.localEulerAngles.y;
		float rotationY = -transform.localEulerAngles.x;
		float tiltZ		= transform.localEulerAngles.z;
		if(tiltZ>180.0F)
			tiltZ-=360.0F;
		tiltZ*=0.99F;
		if (axes == RotationAxes.MouseX || axes == RotationAxes.MouseXAndY)
			rotationX += rot_speed.x * sensitivityX;

		if (axes == RotationAxes.MouseY || axes == RotationAxes.MouseXAndY)
		{
			rotationY -= rot_speed.y * sensitivityY;
			//rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
		}
		tiltZ += rot_speed.z;
		transform.localEulerAngles = new Vector3(-rotationY, rotationX, tiltZ);
	}
	void Update()
	{
		if (Application.isPlaying)
		{
			if (workWhenPlaying)
			{
				if (TiltTurn > 0.0F)
					new_rot_speed.z = -TiltTurn * new_rot_speed.x;
				rot_speed *= (damping);
				rot_speed += (1.0F - damping) * new_rot_speed;
				ApplyRotation(rot_speed);
				new_rot_speed *= 0;

                speed *= (damping);
                speed += (1.0F - damping) * new_rot_speed;
                ApplyTranslation(speed);
                new_speed *= 0;
            }
		}
		else
		{
			rot_speed = new_rot_speed;
            speed = new_speed;
            ApplyTranslation(speed);
        }
	}
#endif
}