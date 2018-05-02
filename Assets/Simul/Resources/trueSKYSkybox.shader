// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "trueSKYSkybox" {
   Properties {
      _Cube ("Environment Map", Cube) = "white" {}
   }
 
   SubShader {
      Tags { "Queue"="Background"  }
 
      Pass {
         ZWrite On 
 
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
 
         samplerCUBE _Cube;
 
         struct vertexInput 
		 {
            float4 vertex : POSITION;
            float3 texcoord : TEXCOORD0;
         };
 
         struct vertexOutput 
		 {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
         };
 
         vertexOutput vert(vertexInput input)
         {
            vertexOutput output;
            output.vertex = UnityObjectToClipPos(float4(input.vertex.xyz, 1.0));
            output.texcoord = input.vertex.xyz;
            return output;
         }
 
         fixed4 frag (vertexOutput input) : SV_Target
         {
            fixed4 f= texCUBE (_Cube, input.texcoord);
			// NOTE: any value other than 1.0 here causes the skybox to show the underlying junk in reflection probe targets
			f.a=1.0;
			return f;
         }
         ENDCG 
      }
   } 	
}