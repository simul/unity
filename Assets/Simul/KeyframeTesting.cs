using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class KeyframeTesting : MonoBehaviour
{
	public simul.trueSKY trueSky=null;
	// Use this for initialization
	void Start ()
	{
	}

	// Update is called once per frame
	void Update()
	{
		/*trueSky = simul.trueSKY.GetTrueSky();
		if (!trueSky)
			return;

		int cloudLayerUid = (int)trueSky.GetCloudLayerByIndex(0);

		int skyKfCount = trueSky.GetNumSkyKeyframes();
		int cloudKfCount = trueSky.GetNumCloudKeyframes(0);
		int cloud2DKfCount = trueSky.GetNumCloud2DKeyframes();

		const bool testInsertAndDeleteKeyframes = false;
		if (testInsertAndDeleteKeyframes)
		{ 
			uint skyKfUid = trueSky.InsertSkyKeyframe(0.5f);
			uint cloudKfUid = trueSky.InsertCloudKeyframe(0.5f, 0);
			uint cloud2DKfUid = trueSky.Insert2DCloudKeyframe(0.5f);

			trueSky.DeleteKeyframe(skyKfUid);
			trueSky.DeleteKeyframe(cloudKfUid);
			trueSky.DeleteKeyframe(cloud2DKfUid);
		}

		uint skyKf0 = trueSky.GetSkyKeyframeByIndex(0);
		uint cloudKf00 = trueSky.GetCloudKeyframeByIndex(0, 0);
		uint cloud2DKf0 = trueSky.GetCloud2DKeyframeByIndex(0);

		uint skyInterpolKf = trueSky.GetInterpolatedSkyKeyframe();
		uint cloudInterpolKf = trueSky.GetInterpolatedCloudKeyframe(0);

		if (cloudKf00 != 0)
		{
			float value = 0.5f * Mathf.Sin(Time.time) + 0.5f;
			trueSky.SetKeyframeValue(cloudKf00, "cloudiness", value);
		}*/

	}
}
