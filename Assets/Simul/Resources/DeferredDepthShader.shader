Shader "Custom/DeferredDepthShader"
{
	CGINCLUDE
	#pragma target 3.0
	#pragma enable_d3d11_debug_symbols
	#include "UnityCG.cginc"

	uniform sampler2D _CameraDepthTexture;
	struct v2f
	{
		float2 uv : TEXCOORD0;
	};

	float2 GetUv(uint id)
	{
		float2 uvs[6];
#if defined(USING_STEREO_MATRICES)
		if (unity_StereoEyeIndex == 0)
		{
			uvs[0] = float2(0.0, 0.0);
			uvs[1] = float2(0.5, 1.0);
			uvs[2] = float2(0.0, 1.0);

			uvs[3] = float2(0.0, 0.0);
			uvs[4] = float2(0.5, 0.0);
			uvs[5] = float2(0.5, 1.0);
		}
		else
		{
			uvs[0] = float2(0.5, 0.0);
			uvs[1] = float2(1.0, 1.0);
			uvs[2] = float2(0.5, 1.0);

			uvs[3] = float2(0.5, 0.0);
			uvs[4] = float2(1.0, 0.0);
			uvs[5] = float2(1.0, 1.0);
		}
#else
		uvs[0] = float2(0.0, 0.0);
		uvs[1] = float2(1.0, 1.0);
		uvs[2] = float2(0.0, 1.0);

		uvs[3] = float2(0.0, 0.0);
		uvs[4] = float2(1.0, 0.0);
		uvs[5] = float2(1.0, 1.0);
#endif
		return uvs[id];
	}

	v2f vert( appdata_img v, uint vid : SV_VertexID, out float4 outpos : SV_POSITION)
	{
		float3 vertices[6];
		vertices[0] = float3(-1.0, 1.0, 1.0);
		vertices[1] = float3( 1.0,-1.0, 1.0);
		vertices[2] = float3(-1.0,-1.0, 1.0);
		vertices[3] = float3(-1.0, 1.0, 1.0);
		vertices[4] = float3( 1.0, 1.0, 1.0);
		vertices[5] = float3( 1.0,-1.0, 1.0);
		v2f o;
		outpos		= float4(vertices[vid],1.0);
		o.uv		= GetUv(vid);
		return o;
	}	

	float4 frag(v2f i, UNITY_VPOS_TYPE screenPos: VPOS) : SV_Target
	{
		float depth = tex2D(_CameraDepthTexture,i.uv).r;
        return float4(depth, depth, depth, 1.0);
	}
	ENDCG

	SubShader
	{
		Pass
		{
  			Blend Off
  
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }      

			CGPROGRAM
			#pragma fragmentoption ARB_precision_hint_nicest
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	} 
Fallback off
}
