Shader "Custom/DepthShader"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		texelScale ("texelScale", Vector) = (0, 0,0) 
		xoffset ("X Offset", Float) = 0
		xscale ("X Scale", Float) = 0
	}
	CGINCLUDE
	#include "UnityCG.cginc"
	uniform sampler2D _CameraDepthTexture;
	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv : TEXCOORD0;
	};
	sampler2D _MainTex;
	float xoffset;
	float xscale;
	float2 texelScale;
	v2f vert( appdata_img v )
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv =  v.texcoord.xy;
		o.pos.x*=xscale;
		o.pos.x+=xoffset;
		return o;
	}
	float4 frag(v2f i) : SV_Target
	{
		float4 ret;
		ret.a=1.0;
		float depth =  tex2D(_CameraDepthTexture,i.uv).r;
		ret.rgb=float3(depth,depth,depth);
		
		return ret;
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
