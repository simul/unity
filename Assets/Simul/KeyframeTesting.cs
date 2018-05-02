using UnityEngine;
using System.Collections;

public class KeyframeTesting : MonoBehaviour
{
	public simul.trueSKY trueSky=null;
	bool initialized=false;
	// Use this for initialization
	void Start ()
	{
		initialized=false;
	}
	// Update is called once per frame
	void Update ()
	{
		if(!initialized)
		{
			UnityEngine.Debug.Log("Running 'Start' in KeyframeTesting script. Setting keyframe values");
			if(trueSky!=null)
			{
				int numk=trueSky.GetNumCloudKeyframes();
				UnityEngine.Debug.Log("Got "+numk+" cloud keyframes.");
				for(int i=0;i<numk;i++)
				{
					uint uid=trueSky.GetCloudKeyframeByIndex(i);
					UnityEngine.Debug.Log("Setting "+uid+" cloudiness to 0.8");
					trueSky.SetKeyframeValue(uid,"cloudiness",0.8);
				}
			}
			initialized=true;
		}
	}
}
