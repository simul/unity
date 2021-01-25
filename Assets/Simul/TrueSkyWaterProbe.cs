using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

using static simul.TrueSkyPluginRenderFunctionImporter;

namespace simul
{
	public class TrueSkyWaterProbe : MonoBehaviour
	{
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

		protected bool UsingIL2CPP()
		{
			return simul.trueSKY.GetTrueSky().UsingIL2CPP;
		}


		//! Values of a water probe
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct WaterProbeValues
		{
			public int ID;
			public float radius;
			public float dEnergy;
			public vec3 location;
			public vec3 velocity;
		};

		private trueSKY mTsInstance;
		private int ID;
		private bool active;
		private bool waterProbeCreated;
		private float depth;
		private Vector3 direction;
		private static int ProbeIDCount = 0;

		WaterProbeValues waterProbeValues = new WaterProbeValues();
		System.IntPtr waterProbeValuesPtr = Marshal.AllocHGlobal(Marshal.SizeOf(new WaterProbeValues()));

		public TrueSkyWaterProbe()
		{
			ProbeIDCount++;
			ID = ProbeIDCount;
			waterProbeCreated = false;
		}

		~TrueSkyWaterProbe()
		{
			if (mTsInstance.SimulVersion >= mTsInstance.MakeSimulVersion(4, 2))
			{
				StaticRemoveWaterProbe(ID);
			}
		}

		//Editor only function
		void OnDrawGizmos()
		{
			Gizmos.color = new Color(1, 0, 0, 1.0f);
			Gizmos.DrawWireSphere(transform.position, _radius);
		}

		public void UpdateProbeValues()
		{
			if (mTsInstance.SimulVersion >= mTsInstance.MakeSimulVersion(4, 2))
			{
				float[] values = new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
				StaticGetWaterProbeValues(ID, values);
				if (values[0] == -1.0 && values[1] == -1.0 && values[2] == -1.0 && values[3] == -1.0)
					active = false;
				else
					active = true;
				depth = values[0] + values[3];
				direction = new Vector3(values[2], values[1], values[3]);
			}
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
			if (mTsInstance.SimulVersion >= mTsInstance.MakeSimulVersion(4, 2))
			{
				waterProbeValues.ID = ID;
				waterProbeValues.radius = Radius;
				waterProbeValues.dEnergy = 0.0f;
				waterProbeValues.location.x = (transform.position.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit;
				waterProbeValues.location.y = (transform.position.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit;
				waterProbeValues.location.z = (transform.position.y + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit;
				waterProbeValues.velocity.x = 0.0f;
				waterProbeValues.velocity.y = 0.0f;
				waterProbeValues.velocity.z = 0.0f;

				bool il2cppScripting = UsingIL2CPP();
				Marshal.StructureToPtr(waterProbeValues, waterProbeValuesPtr, !il2cppScripting);

				if (!waterProbeCreated)
					waterProbeCreated = StaticAddWaterProbe(waterProbeValuesPtr);
				else
				{
					StaticUpdateWaterProbeValues(waterProbeValuesPtr);
				}
			}
		}

		private void Start()
		{
			mTsInstance = trueSKY.GetTrueSky();
			if (mTsInstance.SimulVersion >= mTsInstance.MakeSimulVersion(4, 2))
			{
				float[] location = new float[] {(transform.position.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
											(transform.position.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
											(transform.position.y + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit};
				waterProbeCreated = StaticAddWaterProbe(waterProbeValuesPtr);
			}
		}
	}
}
