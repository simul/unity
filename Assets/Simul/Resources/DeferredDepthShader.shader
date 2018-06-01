Shader "Custom/DepthShader"
{
	Properties
	{
	}
	CGINCLUDE
	#include "UnityCG.cginc"
	uniform sampler2D _CameraDepthTexture;
	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
	};
	v2f vert( appdata_img v )
	{
		v2f o;
		o.pos 	= UnityObjectToClipPos(v.vertex);
		o.uv 	= v.texcoord.xy;
		o.uv.y 	= o.uv.y;	
		return o;
	}
	float4 frag(v2f i) : SV_Target
	{
		float depth = tex2D(_CameraDepthTexture,i.uv).r;
		float4 ret 	= float4(depth,depth,depth,1.0);
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
