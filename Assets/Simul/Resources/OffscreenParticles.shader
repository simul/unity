Shader "Simul/OffscreenParticles" {
    Properties {
		 _TintColor ("Tint Color", Color) = (0.15,0.5,0.5,0.5)
		 _MainTex ("Particle Texture", 2D) = "white" {}
		 _InvFade ("Soft Particles Factor", Range(0.01,3)) = 1
    }
    SubShader
	{
		Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
        Pass
		{
			Tags { "QUEUE"="Transparent" "RenderType"="Transparent" }
			ZWrite Off
			ZTest Less
			Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			AlphaTest Greater 0.01
			ColorMask RGBA
			Color (0,0,0,1)
			ColorMaterial Emission
			Lighting On
            SetTexture [_MainTex] {
                combine texture*primary
            }
        }
    }
}