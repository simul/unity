using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using simul;

[ExecuteInEditMode]
public class TrueSkyDirectionalLight : MonoBehaviour
{
    private trueSKY mTsInstance;
    private Light   mLightComponent;

    public float SunMultiplier  = 1.0f;
    public float MoonMultiplier = 1.0f;
    public bool ApplyRotation   = true;
    

    private void Start()
    {
        mTsInstance = trueSKY.GetTrueSky();
        mLightComponent = GetComponent<Light>();
        if(!mLightComponent)
        {
            Debug.LogError("This script should be attatched to an object with a light component");
        }
    }

    private void Update()
    {
        if(mLightComponent && mTsInstance)
        {
			if (mLightComponent.cookie)
			{
				UpdateCookie();
			}
			UpdateLight();
        }
    }
    float intensity_scale = 0.1F;
    bool UpdateLight()
    {
        LightingQueryResult res = mTsInstance.StaticLightingQuery(this.GetInstanceID(), transform.position);
		if (res.valid == 1)
		{
			Vector4 linearColour = new Vector4();
			linearColour.x = res.sunlight.x;
			linearColour.y = res.sunlight.y;
			linearColour.z = res.sunlight.z;
			linearColour *= SunMultiplier;

			float m = Mathf.Max(Mathf.Max(linearColour.x, linearColour.y), linearColour.z);
			float l = Mathf.Max(m, 1.0f);
			Quaternion lrotation = new Quaternion();
			if (m > 0.0f)
			{
				linearColour /= l;
				mLightComponent.shadows = LightShadows.Soft;
				mLightComponent.intensity = l * intensity_scale;
				// mLightComponent.intensity   = 1.0f;
				lrotation = mTsInstance.getSunRotation();
			}
			else
			{
				linearColour.x = res.moonlight.x;
				linearColour.y = res.moonlight.y;
				linearColour.z = res.moonlight.z;
				linearColour *= MoonMultiplier;

				m = Mathf.Max(Mathf.Max(linearColour.x, linearColour.y), linearColour.z);
				l = Mathf.Max(m, 1.0f);
				if (m > 0.0f)
				{
					linearColour /= l;
					mLightComponent.shadows = LightShadows.Soft;
					mLightComponent.intensity = l * intensity_scale;
					//mLightComponent.intensity   = 1.0f;
					lrotation = mTsInstance.getMoonRotation();
				}
				else
				{
					mLightComponent.shadows = LightShadows.None;
					mLightComponent.intensity = 0.0f;
				}
			}

			mLightComponent.color = new Color(linearColour.x, linearColour.y, linearColour.z, 1.0f);
			if (ApplyRotation)
			{
				transform.rotation = lrotation;
			}
			return true;
		}
		else
		{
			Debug.LogError("Valid = "+ res.valid);
		}
		return false;
    }

    void UpdateCookie()
    {
        Vector3 curShadowCenter = mTsInstance.getCloudShadowCentre();
        if (curShadowCenter.x >= Mathf.Infinity || curShadowCenter.z >= Mathf.Infinity)
        {
            curShadowCenter = Vector3.zero;
        }
        uint currentKeyframe        = mTsInstance.GetInterpolatedCloudKeyframe(0);
        float sunHeight             = (float)mTsInstance.GetKeyframeValue(currentKeyframe, "cloudBase") * 1000;
        float shadowSize            = mTsInstance.getCloudShadowScale();
        //float halfShadowSize        = shadowSize * 0.5f;
        transform.position          = new Vector3(0.0f, sunHeight, 0.0f);
        mLightComponent.cookieSize  = shadowSize / 4.0f;
    }
}
