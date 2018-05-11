using UnityEngine;
using System.Collections;

namespace simul
{
    [ExecuteInEditMode]
    public class SimulSun : MonoBehaviour
    {
        public float ColorMultiplier = 0.03f;

        private Light mSunLight;
        private trueSKY mTsInstance;
        private Vector3 mCurOff;

        private void Start()
        {
            mSunLight = transform.GetComponent<Light>();
            if(!mSunLight)
            {
                Debug.LogWarning("SimulSun must be attached to a Light.");
            }
            mTsInstance = trueSKY.GetTrueSky();
            if(!mTsInstance)
            {
                Debug.LogWarning("Couldn't find a truesky instance in the scene.");
            }
            mCurOff = Vector3.zero;
        }

		private void Update()
		{
			// Set the position in relation with the cloud shadows
			Vector3 curShadowCenter = mTsInstance.getCloudShadowCentre() * 1000;
			// TO-DO: Why getCloudShadowCentre returns infinity values ?
			if (curShadowCenter.x >= Mathf.Infinity || curShadowCenter.z >= Mathf.Infinity)
			{
				curShadowCenter = Vector3.zero;
			}

			// Set current sun rotation
			transform.rotation = mTsInstance.getSunRotation();

			float shadowSize = mTsInstance.getCloudShadowScale();
			float halfShadowSize = shadowSize * 0.5f;
			transform.position = new Vector3(0, transform.position.y, 0);
			transform.position += mCurOff;
			mSunLight.cookieSize = shadowSize * 0.125f;

			// Set sun color
			Vector3 vecSunColor = mTsInstance.getSunColour(mTsInstance.transform.position) * ColorMultiplier;
			Color sunColor = new Color(vecSunColor.x, vecSunColor.y, vecSunColor.z, 1.0f);
			mSunLight.color = sunColor;
		}

		/// <summary>
		/// The offset parameter should be used for example, when we are changing the coordinates of the world 
		/// and camera to solvent floating point precission issues. By setting the offset we ensure that the shadows do not 
		/// jump.
		/// The offset applied should be the offset applied to the world and camera.
		/// </summary>
		/// <param name="off"> The offset</param>
		public void AddOffset(Vector3 off)
        {
            mCurOff += (2.0f * -off);
        }

        /// <summary>
        /// Returns the current offset applied to this light
        /// </summary>
        /// <returns> The cur offset </returns>
        public Vector3 GetOffset()
        {
            return mCurOff;
        }

        public void SetOffset(Vector3 off)
        {
            mCurOff = off;
        }
    }
}