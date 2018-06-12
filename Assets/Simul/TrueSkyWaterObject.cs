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
	[ExecuteInEditMode]
	public class TrueSkyWaterObject : MonoBehaviour
	{
		#region imports
		[DllImport(SimulImports.renderer_dll)] private static extern bool StaticCreateBoundedWaterObject(uint ID, float[] dimension, float[] location);
		[DllImport(SimulImports.renderer_dll)]  private static extern void StaticRemoveBoundedWaterObject(uint ID);
		[DllImport(SimulImports.renderer_dll)]  private static extern void StaticSetWaterFloat(string name, int ID, float value);
		[DllImport(SimulImports.renderer_dll)]  private static extern void StaticSetWaterInt(string name, int ID, int value);
		[DllImport(SimulImports.renderer_dll)]  private static extern void StaticSetWaterBool(string name, int ID, bool value);
		[DllImport(SimulImports.renderer_dll)]  private static extern void StaticSetWaterVector(string name, int ID, float[] value);
		[DllImport(SimulImports.renderer_dll)]  private static extern void StaticSetRenderBool(string name, bool value);
		[DllImport(SimulImports.renderer_dll)]  private static extern void StaticSetRenderFloat(string name, float value);
		#endregion
		#region API
		[SerializeField]
		bool _render = false;
		bool boundedWaterObjectCreated = false;
		private trueSKY mTsInstance;
		public bool Render
		{
			get
			{
				return _render;
			}
			set
			{
				_render = value;
				StaticSetWaterBool("Render", ID, _render);
				if (boundlessIdentifier == this)
				{
					StaticSetRenderBool("EnableBoundlessOcean", _render && _boundlessOcean);
				} else if(!boundedWaterObjectCreated && _render)
				{
					float[] location = new float[] {(transform.localPosition.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
													(transform.localPosition.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
													((transform.localPosition.y + _dimension.y / 2.0f) + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit };
					float[] dimension = new float[] { _dimension.x * mTsInstance.MetresPerUnit, _dimension.z * mTsInstance.MetresPerUnit, _dimension.y * mTsInstance.MetresPerUnit };

					boundedWaterObjectCreated = StaticCreateBoundedWaterObject((uint)ID, dimension, location);
				}
			}
		}

		[SerializeField]
		bool _boundlessOcean = false;
		public bool BoundlessOcean
		{
			get
			{
				return _boundlessOcean;
			}
			set
			{
				if ((boundlessIdentifier != null) && (boundlessIdentifier != this) && !_boundlessOcean)
				{
					_boundlessOcean = false;
					return;
				}
				else if (((boundlessIdentifier == null) || (boundlessIdentifier == this)) && value)
				{
					_boundlessOcean = value;
					boundlessIdentifier = this;

					StaticSetWaterBool("render", ID, false);
					StaticSetRenderBool("EnableBoundlessOcean", _render && _boundlessOcean);
					if (boundedWaterObjectCreated)
					{ 
						StaticRemoveBoundedWaterObject((uint)ID);
						boundedWaterObjectCreated = false;
					}
				}
				else if ((boundlessIdentifier == this) && !value)
				{
					_boundlessOcean = value;
					boundlessIdentifier = null;

					float[] location = new float[] {(transform.localPosition.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
													(transform.localPosition.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
													((transform.localPosition.y + _dimension.y / 2.0f) + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit };
					float[] dimension = new float[] { _dimension.x * mTsInstance.MetresPerUnit, _dimension.z * mTsInstance.MetresPerUnit, _dimension.y * mTsInstance.MetresPerUnit };
					
					boundedWaterObjectCreated = StaticCreateBoundedWaterObject((uint)ID, dimension, location);
					StaticSetWaterBool("Render", ID, _render);
					StaticSetRenderBool("EnableBoundlessOcean", _render && _boundlessOcean);
				}
				else
				{
					_boundlessOcean = false;
				}
			}
		}

		[SerializeField]
		float _beaufortScale = 3.0f;
		public float BeaufortScale
		{
			get
			{
				return _beaufortScale;
			}
			set
			{
				_beaufortScale = value;
				if (_boundlessOcean)
				{
					StaticSetWaterFloat("beaufortScale", -1, _beaufortScale);
				}
				else
				{
					StaticSetWaterFloat("beaufortScale", ID, _beaufortScale);
				}
			}
		}

		[SerializeField]
		float _windDirection = 0.0f;
		public float WindDirection
		{
			get
			{
				return _windDirection;
			}
			set
			{
				_windDirection = value;
				if (_boundlessOcean)
				{
					StaticSetWaterFloat("windDirection", -1, _windDirection * 6.28f);
				} else
				{
					StaticSetWaterFloat("windDirection", ID, _windDirection * 6.28f);
				}
			}
		}

		[SerializeField]
		float _windDependency = 0.95f;
		public float WindDependency
		{
			get
			{
				return _windDependency;
			}
			set
			{
				_windDependency = value;
				if (_boundlessOcean)
				{
					StaticSetWaterFloat("windDependency", -1, _windDependency);
				}
				else
				{
					StaticSetWaterFloat("windDependency", ID, _windDependency);
				}
			}
		}

		[SerializeField]
		Vector3 _scattering = new Vector3(0.17f, 0.2f, 0.234f);
		public Vector3 Scattering
		{
			get
			{
				return _scattering;
			}
			set
			{
				_scattering = value;
				float[] output = new float[] { _scattering.x, _scattering.y, _scattering.z };
				if (_boundlessOcean)
				{
					StaticSetWaterVector("scattering", -1, output);
				}
				else
				{
					StaticSetWaterVector("scattering", ID, output);
				}
			}
		}

		[SerializeField]
		Vector3 _absorption = new Vector3(0.2916f, 0.0474f, 0.0092f);
		public Vector3 Absorption
		{
			get
			{
				return _absorption;
			}
			set
			{
				_absorption = value;
				float[] output = new float[] { _absorption.x, _absorption.y, _absorption.z };
				if (_boundlessOcean)
				{
					StaticSetWaterVector("absorption", -1, output);
				}
				else
				{
					StaticSetWaterVector("absorption", ID, output);
				}
			}
		}

		[SerializeField]
		Vector3 _dimension = new Vector3(2.0f, 2.0f, 2.0f);
		public Vector3 Dimension
		{
			get
			{
				return _dimension;
			}
			set
			{
				_dimension = value;
				float[] output = new float[] { _dimension.x, _dimension.z, _dimension.y };
				StaticSetWaterVector("dimension", ID, output);
			}
		}

		[SerializeField]
		bool _advancedWaterOptions = false;
		public bool AdvancedWaterOptions
		{
			get
			{
				return _advancedWaterOptions;
			}
			set
			{
				_advancedWaterOptions = value;
			}
		}

		[SerializeField]
		float _windSpeed = 30.0f;
		public float WindSpeed
		{
			get
			{
				return _windSpeed;
			}
			set
			{
				_windSpeed = value;
				if (_boundlessOcean)
				{
					StaticSetWaterFloat("windSpeed", -1, _windSpeed);
				}
				else
				{
					StaticSetWaterFloat("windSpeed", ID, _windSpeed);
				}
			}
		}

		[SerializeField]
		float _waveAmplitude = 0.5f;
		public float WaveAmplitude
		{
			get
			{
				return _waveAmplitude;
			}
			set
			{
				_waveAmplitude = value;
				if (_boundlessOcean)
				{
					StaticSetWaterFloat("waveAmplitude", -1, _waveAmplitude);
				}
				else
				{
					StaticSetWaterFloat("waveAmplitude", ID, _waveAmplitude);
				}
			}
		}

		[SerializeField]
		float _choppyScale = 2.0f;
		public float ChoppyScale
		{
			get
			{
				return _choppyScale;
			}
			set
			{
				_choppyScale = value;
				if (_boundlessOcean)
				{
					StaticSetWaterFloat("choppyScale", -1, _choppyScale);
				}
				else
				{
					StaticSetWaterFloat("choppyScale", ID, _choppyScale);
				}
			}
		}

		[SerializeField]
		bool _enableFoam = true;
		public bool EnableFoam
		{
			get
			{
				return _enableFoam;
			}
			set
			{
				_enableFoam = value;
				if (_boundlessOcean)
					StaticSetRenderBool("EnableFoam", _enableFoam);
			}
		}

		[SerializeField]
		float _foamHeight = 2.0f;
		public float FoamHeight
		{
			get
			{
				return _foamHeight;
			}
			set
			{
				_foamHeight = value;
				if (_boundlessOcean)
					StaticSetRenderFloat("OceanFoamHeight", _foamHeight);
			}
		}

		[SerializeField]
		float _foamChurn = 4.0f;
		public float FoamChurn
		{
			get
			{
				return _foamChurn;
			}
			set
			{
				_foamChurn = value;
				if (_boundlessOcean && (boundlessIdentifier == this))
					StaticSetRenderFloat("OceanFoamChurn", _foamChurn);
			}
		}
		#endregion

		public int ID;

		private static TrueSkyWaterObject boundlessIdentifier = null;
		private static int IDCount = 0;

		public TrueSkyWaterObject()
		{
			IDCount++;
			ID = IDCount;
		}

		~TrueSkyWaterObject()
		{
			if (this == boundlessIdentifier)
			{
				boundlessIdentifier = null;
				StaticSetRenderBool("EnableBoundlessOcean", false);
			}
			StaticRemoveBoundedWaterObject((uint)ID);
			boundedWaterObjectCreated = false;
		}

		//Editor only function
		void OnDrawGizmos()
		{
			Gizmos.color = new Color(0, 0, 1, 0.75F);
			if (!_boundlessOcean)
				Gizmos.DrawCube(transform.position, _dimension);
			else
			{
				Vector3 tempDimension = new Vector3(300000.0f, 10.0f, 300000.0f);
				Vector3 tempPosition = new Vector3(transform.position.x, transform.position.y + (_dimension.y / 2.0f) - 5.0f, transform.position.z);
				Gizmos.DrawCube(tempPosition, tempDimension);
			}
		}


		void Update()
		{
			float[] location = new float[] {(transform.localPosition.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
											(transform.localPosition.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
											((transform.localPosition.y + _dimension.y / 2.0f) + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit} ;
			float[] dimension = new float[] { _dimension.x * mTsInstance.MetresPerUnit, _dimension.z * mTsInstance.MetresPerUnit, _dimension.y * mTsInstance.MetresPerUnit };

			if (_boundlessOcean)
			{
				if (boundedWaterObjectCreated)
				{
					StaticSetWaterBool("Render", ID, false);
					StaticRemoveBoundedWaterObject((uint)ID);
					boundedWaterObjectCreated = false;
				}
				StaticSetRenderBool("EnableBoundlessOcean", _render && _boundlessOcean);
				StaticSetWaterVector("location", -1, location);
			}
			else
			{
				if (transform.hasChanged)
				{
					_dimension = new Vector3(2.0f * transform.localScale.x, 2.0f * transform.localScale.y, 2.0f * transform.localScale.z);
					transform.hasChanged = false;
				}

				if (!boundedWaterObjectCreated)
				{
					
					boundedWaterObjectCreated = StaticCreateBoundedWaterObject((uint)ID, dimension, location);
					StaticSetWaterBool("Render", ID, _render);
				}
				StaticSetWaterBool("Render", ID, _render);
				StaticSetWaterVector("location", ID, location);
				StaticSetWaterVector("dimension", ID, dimension);

			}
		}

		void Start()
		{
			/*if (boundlessIdentifier != null && _boundlessOcean)
			{
				UnityEngine.Debug.LogError("Only one boundless water object should be instantiated.");
				_boundlessOcean = false;
			}
			else
			{
				boundlessIdentifier = this;
				_boundlessOcean = true;
			}*/

			mTsInstance = trueSKY.GetTrueSky();

			float[] location = new float[] {(transform.localPosition.z + mTsInstance.transform.position.x) * mTsInstance.MetresPerUnit,
											(transform.localPosition.x + mTsInstance.transform.position.z) * mTsInstance.MetresPerUnit,
											((transform.localPosition.y + _dimension.y / 2.0f) + mTsInstance.transform.position.y) * mTsInstance.MetresPerUnit};
			float[] dimension = new float[] { _dimension.x * mTsInstance.MetresPerUnit, _dimension.z * mTsInstance.MetresPerUnit, _dimension.y * mTsInstance.MetresPerUnit };

			if (!_boundlessOcean)
			{

				boundedWaterObjectCreated = StaticCreateBoundedWaterObject((uint)ID, dimension, location);
				StaticSetWaterBool("Render", ID, _render);
				StaticSetWaterVector("location", ID, location);
				StaticSetWaterVector("dimension", ID, dimension);
				StaticSetWaterFloat("beaufortScale", ID, _beaufortScale);
				StaticSetWaterFloat("windDirection", ID, _windDirection * 6.28f);
				StaticSetWaterFloat("windDependency", ID, _windDependency);
			}
			else
			{
				if (boundlessIdentifier == null)
				{
					boundlessIdentifier = this;
					if (boundedWaterObjectCreated)
					{
						StaticRemoveBoundedWaterObject((uint)ID);
						boundedWaterObjectCreated = false;
					}
					StaticSetRenderBool("EnableBoundlessOcean", _render && _boundlessOcean);
					//StaticSetWaterBool("Render", ID, false);
					StaticSetWaterVector("location", -1, location);
					StaticSetWaterFloat("beaufortScale", -1, _beaufortScale);
					StaticSetWaterFloat("windDirection", -1, _windDirection * 6.28f);
					StaticSetWaterFloat("windDependency", -1, _windDependency);
				}
				else
				{
					_boundlessOcean = false;
					boundedWaterObjectCreated = StaticCreateBoundedWaterObject((uint)ID, dimension, location);
					StaticSetWaterBool("Render", ID, _render);
					StaticSetWaterVector("location", ID, location);
					StaticSetWaterVector("dimension", ID, dimension);
					StaticSetWaterFloat("beaufortScale", ID, _beaufortScale);
					StaticSetWaterFloat("windDirection", ID, _windDirection * 6.28f);
					StaticSetWaterFloat("windDependency", ID, _windDependency);
				}

			}
		}
	}
}