using UnityEngine;
using System.Collections;
using System.Threading;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

using static simul.TrueSkyPluginRenderFunctionImporter;

namespace simul
{
	[ExecuteInEditMode]
	public class TrueSkyCameraBase : MonoBehaviour
	{
		public class RenderTextureHolder
		{
			public RenderTexture renderTexture = null;
			public System.IntPtr GetNative()
			{
				if (cachedRenderTexture != renderTexture)
				{
					_nativeTexturePtr = (System.IntPtr)0;
				}
				if (_nativeTexturePtr == (System.IntPtr)0 && renderTexture != null)
				{
					_nativeTexturePtr = renderTexture.GetNativeTexturePtr();
				}
				return _nativeTexturePtr;
			}

			protected Texture cachedRenderTexture = null;
			protected System.IntPtr _nativeTexturePtr = (System.IntPtr)0;
			protected System.IntPtr _nativeDepthTexturePtr = (System.IntPtr)0;
		};

		//! An event ID that will hopefully be sufficiently unique to trueSKY - if not, change this.
		protected const int TRUESKY_EVENT_ID = 13476;
		public static int GetTRUESKY_EVENT_ID()
		{
			return TRUESKY_EVENT_ID;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct UnityViewStruct
		{
			public System.IntPtr nativeColourRenderBuffer;
			public System.IntPtr nativeDepthRenderBuffer;
			public int view_id;
			public int framenumber;
			public float exposure;
			public float gamma;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)] public float[] viewMatrices4x4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)] public float[] projMatrices4x4;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public float[] overlayProjMatrix4x4;
			public System.IntPtr depthTexture;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public int4[] depthViewports;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)] public Viewport[] targetViewports;
			public RenderStyle renderStyle;
			public UnityRenderOptions unityRenderOptions;
			public System.IntPtr colourTexture;
		}

        protected bool UsingIL2CPP()
        {
            return simul.trueSKY.GetTrueSky().UsingIL2CPP;
        }

        protected int view_ident;
        protected int view_id=-1;
		protected static int last_view_ident = 0;

        protected float[] viewMatrices = new float[48];
        protected float[] projMatrices = new float[48];
        protected  int4[] depthViewports = new int4[3];
        public TrueSkyCameraBase ()
		{
            view_ident = last_view_ident + 1;
			last_view_ident++;
        }
        ~TrueSkyCameraBase()
        {
            StaticRemoveView(view_id);
        }
		protected void RemoveBuffer(string name)
		{
			Camera cam=GetComponent<Camera>();
			CommandBuffer[] opaque=cam.GetCommandBuffers(CameraEvent.BeforeImageEffectsOpaque);
			CommandBuffer[] after=cam.GetCommandBuffers(CameraEvent.AfterEverything);
            CommandBuffer[] afterF = cam.GetCommandBuffers(CameraEvent.AfterForwardAlpha);
			CommandBuffer[] bufs = new CommandBuffer[opaque.Length + afterF.Length + after.Length];
			opaque.CopyTo(bufs, 0);
			after.CopyTo(bufs, opaque.Length);
            afterF.CopyTo(bufs, opaque.Length + after.Length);
			for (int i =0;i<bufs.Length; i=i+1)
			{
				CommandBuffer b=bufs[i];
				if(b.name==name)
				{
					cam.RemoveCommandBuffer(CameraEvent.AfterEverything,b);
                    cam.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, b);
					cam.RemoveCommandBuffer(CameraEvent.BeforeImageEffectsOpaque,b);
					return;
				}
			}
		}
		protected static Mutex mut  = new Mutex ();
        public bool flippedView     = true;
        public float exposure       = 0.5F;
        public float gamma          = 0.5F;
		static protected Texture2D _dummyTexture;
		protected RenderTextureHolder depthTexture      = new RenderTextureHolder();
		protected RenderTextureHolder unityDepthTexture = new RenderTextureHolder();
		static protected Material _flippedDepthMaterial = null;
		static protected Material _deferredDepthMaterial= null;
		static protected Shader _flippedShader  = null;
		static protected Shader _deferredShader = null;
		protected CommandBuffer mainCommandBuffer  = null;
		protected int cbuf_view_id              = -1;
		public int GetViewId()
		{
			return view_id;
		}
		protected virtual int InternalGetViewId()
		{
			return StaticGetOrAddView((System.IntPtr)view_ident);
        }
		protected virtual int GetRequiredDepthTextureWidth()
        {
            var cam = GetComponent<Camera>();
            int w =cam.pixelWidth;
            return w;
		}
		protected virtual void EnsureDepthTexture()
		{
			int required_width = GetRequiredDepthTextureWidth();
			if (depthTexture.renderTexture == null
				|| depthTexture.renderTexture.width != required_width
				|| depthTexture.renderTexture.height != GetComponent<Camera>().pixelHeight)
			{
				Camera camera = GetComponent<Camera>();
#if TRUESKY_LOGGING
                UnityEngine.Debug.Log ("EnsureDepthTexture resized texture for "+view_id +" to "+required_width+","+camera.pixelHeight); 
#endif
                RenderTexture rt = new RenderTexture(required_width, (int)camera.pixelHeight, 32, RenderTextureFormat.ARGBFloat);
				depthTexture.renderTexture=rt;
				rt.Create();
			}
		}
		protected void PreRender()
		{
			if (!enabled || !gameObject.activeInHierarchy)
			{
				UnityEngine.Debug.Log ("OnPreRender disabled"); 
				return;
			}
			SimulImports.Init ();
			mut.WaitOne ();
			try
			{
				view_id = InternalGetViewId();
				EnsureDepthTexture();
                // Unity can't Blit without a source texture, so we create a small, unused dummy texture.
                if (_dummyTexture == null) 
					_dummyTexture=new Texture2D(1,1,TextureFormat.Alpha8,false);
			}
			finally
			{
			}
			mut.ReleaseMutex ();

		}
		void OnPreRender()
		{
			PreRender();
		}
		static public void MatrixTransform(float[] mat)
		{
			mat[00]=-1f;
			mat[01]=0f;
			mat[02]=0f;
			mat[03]=0f;
			  
			mat[04]=0;
			mat[05]=0;
			mat[06]=-1f;
			mat[07]=0;
			   
			mat[08]=0f;
			mat[09]=-1f;
			mat[10]=0f;
			mat[11]=0f;
			  
			mat[12]=0f;
			mat[13]=0f;
			mat[14]=0f;
			mat[15]=1f;
		}
		//protected float[] projMatrix = new float[16];
		protected float[] overlayProjMatrix = new float[16];
		protected void ProjMatrixToTrueSkyFormat(RenderStyle renderStyle, Matrix4x4 m,float[] proj, int offset = 0)
        {
            offset *= 16;
            float metresPerUnit = trueSKY.GetTrueSky().MetresPerUnit;

            proj[offset+00] = m.m00;
			proj[offset+01] = m.m01;
			proj[offset+02] = m.m02;
			proj[offset+03] = m.m03* metresPerUnit;
			  
			proj[offset+04] = m.m10;
			proj[offset+05] = m.m11;
			proj[offset+06] = m.m12;
			proj[offset+07] = m.m13 * metresPerUnit;

			proj[offset+08] = m.m20;
			proj[offset+09] = m.m21;
			proj[offset+10] = m.m22;
			proj[offset+11] = m.m23*metresPerUnit;
			
			proj[offset+12] = m.m30;
			proj[offset+13] = m.m31;
			proj[offset+14] = m.m32;
			proj[offset+15] = m.m33 * metresPerUnit;
		}
		static public void ViewMatrixToTrueSkyFormat(RenderStyle renderStyle,Matrix4x4 m,float[] view,int offset=0,bool swap_yz=true)
		{
		    // transform?
			if (trueSKY.GetTrueSky() == null)
				return;
            offset *= 16;
			float metresPerUnit = trueSKY.GetTrueSky().MetresPerUnit;
			Matrix4x4 transform = trueSKY.GetTrueSky().transform.worldToLocalMatrix.inverse;
			m=m*transform;
			m=m.transpose;
			Matrix4x4 n=m.inverse;
			Matrix4x4 y;
			if (swap_yz)
			{
				// Swap the y and z columns - this makes a left-handed matrix into right-handed:
				y.m00 = n.m00;
				y.m01 = n.m02;
				y.m02 = n.m01;
				y.m03 = n.m03;

				y.m10 = n.m10;
				y.m11 = n.m12;
				y.m12 = n.m11;
				y.m13 = n.m13;

				y.m20 = n.m20;
				y.m21 = n.m22;
				y.m22 = n.m21;
				y.m23 = n.m23;
				// Swap the position values as well, as Unity uses y=up, we use z:
				y.m30 = n.m30 * metresPerUnit;
				y.m31 = n.m32 * metresPerUnit;
				y.m32 = n.m31 * metresPerUnit;
				y.m33 = n.m33;
			}
			else
			{
				// Swap the y and z columns - this makes a left-handed matrix into right-handed:
				y.m00 = n.m00;
				y.m01 = n.m01;
				y.m02 = n.m02;
				y.m03 = n.m03;

				y.m10 = n.m10;
				y.m11 = n.m11;
				y.m12 = n.m12;
				y.m13 = n.m13;

				y.m20 = n.m20;
				y.m21 = n.m21;
				y.m22 = n.m22;
				y.m23 = n.m23;
				// Swap the position values as well, as Unity uses y=up, we use z:
				y.m30 = n.m30 * metresPerUnit;
				y.m31 = n.m31 * metresPerUnit;
				y.m32 = n.m32 * metresPerUnit;
				y.m33 = n.m33;
			}

			Matrix4x4 z=y.inverse;
			view[offset+00] = z.m00;
			view[offset+01] = z.m01;
			view[offset+02] = z.m02;
			view[offset+03] = z.m03;

			view[offset+04] = z.m10;
			view[offset+05] = z.m11;
			view[offset+06] = z.m12;
			view[offset+07] = z.m13;

			view[offset+08] = z.m20;
			view[offset+09] = z.m21;
			view[offset+10] = z.m22;
			view[offset+11] = z.m23;
			 		   
			view[offset+12] = z.m30;
			view[offset+13] = z.m31;
			view[offset+14] = z.m32;
			view[offset+15] = z.m33;
		}
		public virtual RenderStyle GetRenderStyle()
		{
			RenderStyle renderStyle = RenderStyle.UNITY_STYLE_DEFERRED;
            var cam = GetComponent<Camera>();
			if (cam.actualRenderingPath != RenderingPath.DeferredLighting
				&&cam.actualRenderingPath!=RenderingPath.DeferredShading)
			{
				if (flippedView)
					renderStyle = RenderStyle.UNITY_STYLE_DEFERRED;
				else
					renderStyle = RenderStyle.UNITY_STYLE;
			}
			return renderStyle;
		}
		bool _initialized = false;

		void Awake ()
		{
			if (_initialized)
				return;
			_initialized = true;
		}

		void OnDisable ()
		{
			_flippedDepthMaterial = null;
			_deferredDepthMaterial = null;
			_flippedShader = null;
			_deferredShader = null;
		}

		void OnEnable ()
		{
		}
	}
}
