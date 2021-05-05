Shader "Custom/TestQuadShader"
{
	Properties
	{
	}

	SubShader
	{
		Cull Off ZWrite Off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
				uint   id     : SV_VertexID;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				float4 verts[4] = {
					float4(1, 1, 0, 1),
					float4(0, 1, 0, 1),
					float4(0, 0, 0, 1),
					float4(1, 0, 0, 1),
				};
				o.vertex = verts[v.id]*float4(0.02,0.02,0.02,1.0)+float4(-1.0,-1.0,0.0,0.0);
				o.uv = float2(0,0);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return fixed4(0,0,0,0);
			}
			ENDCG
		}
	}
} 