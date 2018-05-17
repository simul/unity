Shader "Unlit/LinearDepth"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
         Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				//float depth;
				//depth=-UnityObjectToClipPos( i.vertex ).z;
				float depth = (i.vertex.z/i.vertex.w);
				return float4(depth, 0, 0, 0);
			}
			ENDCG
		}
	}
}
