using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace simul
{
	public class TrueSkyWaterProbe : MonoBehaviour
	{
		#region imports
		[DllImport(SimulImports.renderer_dll)]	private static extern void StaticAddWaterProbe(uint ID, float[] location);
		[DllImport(SimulImports.renderer_dll)]	private static extern void StaticRemoveWaterProbe(uint ID);
		[DllImport(SimulImports.renderer_dll)]	private static extern void StaticUpdateWaterProbePosition(uint ID, float[] location);
		[DllImport(SimulImports.renderer_dll)]	private static extern Vector4 StaticGetWaterProbeValues(uint ID);
		#endregion

		[SerializeField]
		float _radius = 4.0f;
		public float Radius
		{
			get
			{
				return _radius;
			}
			set
			{
				_radius = value;
			}
		}
		private trueSKY mTsInstance;
		private uint ID;
		private bool active;
		private float depth;
		private Vector3 direction;
		private static uint ProbeIDCount = 0;

		public TrueSkyWaterProbe()
		{
			ProbeIDCount++;
			ID = ProbeIDCount;
		}

		~TrueSkyWaterProbe()
		{
			StaticRemoveWaterProbe(ID);
		}

		//Editor only function
		void OnDrawGizmos()
		{
			Gizmos.color = new Color(1, 0, 0, 1.0f);
			Gizmos.DrawWireSphere(transform.position, _radius);
		}

		public void UpdateProbeValues()
		{
			Vector4 values = StaticGetWaterProbeValues(ID);
			if (values.x == -1.0 && values.y == -1.0 && values.z == -1.0 && values.w == -1.0)
				active = false;
			else
				active = true;
			depth = values.x + values.w;
			direction = new Vector3(values.z, values.y, values.w);
		}

		public bool IsActive()
		{
			return active;
		}

		public float GetDepth()
		{
			return depth;
		}

		public Vector3 GetDirection()
		{
			return direction;
		}

		void Update()
		{
			float[] location = new float[] {(transform.position.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
											(transform.position.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
											(transform.position.y + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit};
			StaticUpdateWaterProbePosition(ID, location);
		}

		private void Start()
		{
			mTsInstance = trueSKY.GetTrueSky();
			float[] location = new float[] {(transform.position.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
											(transform.position.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
											(transform.position.y + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit};
			StaticAddWaterProbe(ID, location);
		}
	}
}
