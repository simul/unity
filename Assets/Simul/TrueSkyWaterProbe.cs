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
		[DllImport(SimulImports.renderer_dll)]	private static extern bool StaticAddWaterProbe(uint ID, float[] location);
		[DllImport(SimulImports.renderer_dll)]	private static extern void StaticRemoveWaterProbe(uint ID);
		[DllImport(SimulImports.renderer_dll)]	private static extern void StaticUpdateWaterProbePosition(uint ID, float[] location);
		[DllImport(SimulImports.renderer_dll)]	private static extern void StaticGetWaterProbeValues(uint ID, float[] result);
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
		private bool waterProbeCreated;
		private float depth;
		private Vector3 direction;
		private static uint ProbeIDCount = 0;

		public TrueSkyWaterProbe()
		{
			ProbeIDCount++;
			ID = ProbeIDCount;
			waterProbeCreated = false;
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
			float[] values = new float[] {0.0f, 0.0f, 0.0f, 0.0f};
			StaticGetWaterProbeValues(ID, values);
			if (values[0] == -1.0 && values[1] == -1.0 && values[2] == -1.0 && values[3] == -1.0)
				active = false;
			else
				active = true;
			depth = values[0] + values[3];
			direction = new Vector3(values[2], values[1], values[3]);
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

			if(!waterProbeCreated)
				waterProbeCreated = StaticAddWaterProbe(ID, location);

			StaticUpdateWaterProbePosition(ID, location);
		}

		private void Start()
		{
			mTsInstance = trueSKY.GetTrueSky();
			float[] location = new float[] {(transform.position.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
											(transform.position.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
											(transform.position.y + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit};
			waterProbeCreated = StaticAddWaterProbe(ID, location);
		}
	}
}
