using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using simul;

#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

[ExecuteInEditMode]
public class TrueSkyDirectionalLight : MonoBehaviour
{
	private trueSKY mTsInstance;
	private Light mLightComponent;
#if USING_HDRP
	private HDAdditionalLightData mHDAdditionalLightData;
#endif
	public float SunMultiplier = 1.0f;
	public float MoonMultiplier = 1.0f;
	public float AmbientMultiplier = 1.0f;
	public bool ApplyRotation = true;

	public enum LightUnits : byte
	{
		Radiometric = 0,
		Photometric = 1
	};
	public LightUnits Units = LightUnits.Radiometric;


	private void Start()
	{
		mTsInstance = trueSKY.GetTrueSky();
		mLightComponent = GetComponent<Light>();
		if (!mLightComponent)
		{
			Debug.LogError("This script should be attatched to an object with a light component");
		}
#if USING_HDRP
		mHDAdditionalLightData = GetComponent<HDAdditionalLightData>();//Get Reference to the class holding HDRP light properties
#endif

	}

	private void Update()
	{
		if (mLightComponent && mTsInstance)
		{
			if (mLightComponent.cookie == null)
				mLightComponent.cookie = mTsInstance.CloudShadowTexture.renderTexture;
#if USING_HDRP
			if (mHDAdditionalLightData == null)
				mHDAdditionalLightData = GetComponent<HDAdditionalLightData>();
#endif
			if (mLightComponent.cookie)
			{
				UpdateCookie();
			}

			UpdateLight();
		}
	}

	float intensity_scale = 0.1f;
	bool UpdateLight()
	{
		LightingQueryResult res = mTsInstance.LightingQuery(this.GetInstanceID(), transform.position);
		if (res.valid == 1)
		{
			//This is checking if the units are photometric, the Units Unity wants. 
			//We would need access to individual wavelengths for correct Value. 
			//If the values are Radiometric, then we can apply the brightness power multiplier in the functions below, else we want to convert into Lux...
			if (Units == LightUnits.Photometric)
			{
				//Ideally, we want: Lumen = 683 lm/W * Integral(from l = 380nm -> 830nm) [Power(l) * Photopic/Scotpic Luminous Efficacy * dl].
				//Or we can have the estimate instead of having photopic and scotopic values (Lux * 0.0079 = W/m2), (W/m2 * 127 = Lux). This does not take into account wavelength.
				//https://physics.stackexchange.com/questions/135618/rm-lux-and-w-m2-relationship#:~:text=There%20is%20no%20simple%20conversion,%3D590W%2Fm2

				const float PhotometricUnitConversion = 127.0f * 555.0f; //The 555 here is to because of original units are W/m2/nm. Photopic peak is at 555nm.
				res.sunlight.x *= PhotometricUnitConversion;
				res.sunlight.y *= PhotometricUnitConversion;
				res.sunlight.z *= PhotometricUnitConversion;
				res.sunlight.w *= PhotometricUnitConversion;
				res.moonlight.x *= PhotometricUnitConversion;
				res.moonlight.y *= PhotometricUnitConversion;
				res.moonlight.z *= PhotometricUnitConversion;
				res.moonlight.w *= PhotometricUnitConversion;
				res.ambient.x *= PhotometricUnitConversion;
				res.ambient.y *= PhotometricUnitConversion;
				res.ambient.z *= PhotometricUnitConversion;
				res.ambient.w *= PhotometricUnitConversion;
			}

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
					lrotation = mTsInstance.getMoonRotation();
				}
				else
				{
					linearColour.x = res.ambient.x;
					linearColour.y = res.ambient.y;
					linearColour.z = res.ambient.z;
					linearColour *= AmbientMultiplier;

					m = Mathf.Max(Mathf.Max(linearColour.x, linearColour.y), linearColour.z);
					l = Mathf.Max(m, 1.0f);
					if (m > 0.0f)
					{
						linearColour /= l;
						mLightComponent.shadows = LightShadows.Soft;
						mLightComponent.intensity = l * intensity_scale;
					}
					else
					{
						mLightComponent.shadows = LightShadows.None;
						mLightComponent.intensity = 0.0f;
					}
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
			//	Debug.LogError("Valid = "+ res.valid);
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
		uint currentKeyframe = mTsInstance.GetInterpolatedCloudKeyframe(0);
		float sunHeight = mTsInstance.GetKeyframeValue<float>(currentKeyframe, "cloudBase") * 1000.0f;
		float shadowSize = mTsInstance.getCloudShadowScale();
		//float halfShadowSize		= shadowSize * 0.5f;
		transform.position = new Vector3(0.0f, sunHeight, 0.0f);
		mLightComponent.cookieSize = shadowSize / 4.0f; //would be moved if using shaderGraph.
														// cookieSize does not work in HDRP, instead we use:
#if USING_HDRP
		if (mHDAdditionalLightData != null)
		{
			//mHDAdditionalLightData.intensity = 200000.0F*mLightComponent.intensity;//Change the intensity of the light
			mHDAdditionalLightData.SetCookie(mLightComponent.cookie, new Vector2(shadowSize / 4.0f, shadowSize / 4.0f));//Change size of the light cookie
		}
#endif

		//mLightComponent.areaSize = new Vector2Int(262144, 262144);
	}
}
