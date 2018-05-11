using UnityEngine;
using System.Collections;

namespace simul
{
	[ExecuteInEditMode]
	public class SimulMoon : MonoBehaviour
	{
        public float ColorMultiplier = 0.75f;

        private Light mMoonLight;
        private trueSKY mTsInstance;
        private Vector3 mCurOff;

        private void Start()
        {
            mMoonLight = transform.GetComponent<Light>();
            if (!mMoonLight)
            {
                Debug.LogWarning("SimulMoon must be attached to a Light.");
            }
            mTsInstance = trueSKY.GetTrueSky();
            if (!mTsInstance)
            {
                Debug.LogWarning("Couldn't find a truesky instance in the scene.");
            }
            mCurOff = Vector3.zero;
        }

        private void Update()
        {
            // Set the position in relation with the cloud shadows
            Vector3 curShadowCenter = mTsInstance.getCloudShadowCentre();
            // TO-DO: Why getCloudShadowCentre returns infinity values ?
            if (curShadowCenter.x >= Mathf.Infinity || curShadowCenter.z >= Mathf.Infinity)
            {
                curShadowCenter = Vector3.zero;
            }
            float shadowSize = mTsInstance.getCloudShadowScale();
            float halfShadowSize = shadowSize * 0.5f;
            transform.position = new Vector3(curShadowCenter.x + halfShadowSize, transform.position.y, curShadowCenter.z + halfShadowSize);
            transform.position += mCurOff;
            mMoonLight.cookieSize = shadowSize;

            // Set current sun rotation
            transform.rotation = mTsInstance.getMoonRotation();

            // Set moon color
            Vector3 vecMoonColor = mTsInstance.getMoonColour(transform.position) * ColorMultiplier;
            Color moonColor = new Color(vecMoonColor.x, vecMoonColor.y, vecMoonColor.z, 1.0f);
            mMoonLight.color = moonColor;
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