Shader "Simul/TransparencyTestShader" 
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		_FadeFactor("Fade Factor(trueSKY)",Range(0.01,5.0)) = 1.5
		_Loss("Loss (trueSKY)", 2D) = "white" {}
		_Inscatter("Inscatter (trueSKY)", 2D) = "black" {}
		_CloudVisibility("Cloud Visibility (trueSKY)", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
		LOD 200
		ZWrite Off

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard fullforwardshadows alpha
		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _Loss;
		sampler2D _Inscatter;
		sampler2D _CloudVisibility;

		struct Input 
		{
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldPos;
			float4 screenPos;
		};

		half _FadeFactor;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void surf(Input IN, inout SurfaceOutputStandard o) 
		{
			// Get opacity from the cloud vis texture
			float2 screenUV			= IN.screenPos.xy / IN.screenPos.w;
			float4 vis				= tex2D(_CloudVisibility, screenUV);
			const float maxDistance = 300000.0; 
			float dist				= length(_WorldSpaceCameraPos - IN.worldPos) / maxDistance;
			float opacity			= 1.0 - saturate((dist - vis.w) / (vis.x * _FadeFactor));
		
			// The loss is given by distance and angular elevation from the viewer.
			float3 view				= normalize(IN.viewDir);
			float sine_elevation	= view.z;
			float2 fade_texc		= float2(sqrt(dist),1.0 - (0.5 * (sine_elevation + 1.0 )));

			fixed4 loss				= tex2D(_Loss,fade_texc);
			fixed4 insc				= tex2D(_Inscatter,fade_texc);
			fixed4 c				= tex2D(_MainTex, IN.uv_MainTex) * _Color;

			o.Albedo				= loss.rgb * c.rgb;
			o.Emission				= insc.rgb*c.a*opacity;
			o.Metallic				= _Metallic*c.a*opacity;
			o.Smoothness			= _Glossiness*c.a*opacity;
			o.Alpha					= c.a*opacity;

			// Unity clamps the alpha value so we need to discard the pixel
			if (o.Alpha < 0.02)discard;
		}
		ENDCG
	}
}