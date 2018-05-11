// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "trueSKY Unity skybox" {
   Properties {
      _Cube ("Environment Map", Cube) = "white" {}
   }
 
   SubShader {
      Tags { "Queue"="Background"  }
 
      Pass {
         ZWrite Off 
         Cull Off 
 
         CGPROGRAM
         #pragma vertex vert
         #pragma fragment frag
 
         // User-specified uniforms
         samplerCUBE _Cube;
 
         struct vertexInput {
            float4 vertex : POSITION;
            float3 texcoord : TEXCOORD0;
         };
 
         struct vertexOutput {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
         };
 
         vertexOutput vert(vertexInput input)
         {
            vertexOutput output;
            output.vertex = UnityObjectToClipPos(input.vertex);
            output.texcoord = input.texcoord;
            return output;
         }
 
         fixed4 frag (vertexOutput input) : SV_Target
         {
            fixed4 f= texCUBE (_Cube, input.texcoord);
			return f;
         }
         ENDCG 
      }
   } 	
}