Shader "ShowCubemap" {
   Properties {
      _Cube ("Environment Map", Cube) = "white" {}
   }
  SubShader {
      Tags { "RenderType" = "Opaque" }

		CGPROGRAM
		samplerCUBE _Cube;
      #pragma surface surf Lambert
      struct Input {
          float3 worldRefl;
      };
      void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = 0;
            float4 f= texCUBE (_Cube, IN.worldRefl);
		o.Emission =f.rgb;
      }
      ENDCG
    }
    Fallback "Diffuse"
 } 	