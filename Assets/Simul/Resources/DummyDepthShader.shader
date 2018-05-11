// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/DepthShader"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		texelScale ("texelScale", Vector) = (0, 0,0) 
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
	float _Intensity;
	float2 texelScale;
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv =  v.texcoord.xy;
        o.projPos = ComputeScreenPos(o.pos);
		return o;
	}
	float4 frag(v2f i) : SV_Target
	{
		float4 ret;
		float height=i.pos.y;
		ret.a=1.0;
		float depth =  (tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD(i.projPos)).r);
		ret.rgb=1.0;
		return ret;//tex2D(_MainTex, i.uv) * _Intensity; 
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
