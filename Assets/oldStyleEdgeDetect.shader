Shader "Custom/oldStyleEdgeDetect" {
    Properties {
      _Color ("MainColor", Color) = (1,1,1,1)
      _Amount ("Edge Width", Range(1,2)) = 1.1
    }
    SubShader {
      Tags { "RenderType" = "Opaque" "Queue" = "Geometry-10"}
      Pass{
        Cull Front
        ZWrite Off
      }
      CGPROGRAM
      #pragma surface surf Lambert vertex:vert alpha

      struct Input {
          float4 screenPos;
      };
      
      float _Amount;
      float4 _Color;
      sampler2D _MainTex;
      
      void vert (inout appdata_full v) {
          v.vertex.xyz *= _Amount;
      }
      void surf (Input IN, inout SurfaceOutput o) {
          o.Emission = _Color.rgb;
          o.Alpha = _Color.a;
      }
      ENDCG
    } 
    Fallback "Unlit"
}
