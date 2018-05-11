Shader "Simul/OverlayShader"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_OverlayTex ("Overlay (RGB)", 2D) = "white" {}
	}
	CGINCLUDE
	#include "UnityCG.cginc"
	uniform sampler2D _CameraDepthTexture;
	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
        float4 projPos : TEXCOORD1;
	};
	sampler2D _MainTex;
	sampler2D _OverlayTex;
	float _Intensity;
	float2 texelScale;
	v2f vert(appdata_img v)
	{
		v2f o;
		o.pos		=UnityObjectToClipPos(v.vertex);
		o.uv		=v.texcoord.xy;
        o.projPos	=ComputeScreenPos(o.pos);
		return o;
	}
	float4 frag(v2f IN) : SV_Target
	{
		float4 ret	=tex2D(_MainTex, IN.uv);
		float4 ovl	=tex2D(_OverlayTex, IN.uv);
		ret.rgb		*=ovl.a;
		ret.rgb		+=ovl.rgb;
		return ret;
	}
	ENDCG
	SubShader
	{
		Tags {"Queue"="Geometry+1"}
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
