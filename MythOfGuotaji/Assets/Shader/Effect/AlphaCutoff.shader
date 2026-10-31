Shader "Effect/AlphaCutoff" {
    Properties{
        _MainTex("Base (RGB) Transparency (A)", 2D) = "" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
    }
        SubShader{
            Pass {
                AlphaTest Greater[_Cutoff]
                Material {
                    Diffuse(1,1,1,1)
                    Ambient(1,1,1,1)
                }
                Lighting off
                SetTexture[_MainTex] { combine texture * primary }
            }
        }
}