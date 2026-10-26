Shader "HomunCat/HC_TerrainTexShader"
{
    Properties
    {
        // 구역을 나누는 마스크 텍스처 (RGBA 채널 사용)
        [MainTexture] _AreaTex("Area Mask Texture (RGBA)", 2D) = "black" {}

        // Diffuse 텍스처
        _MainTex("Base (RGBA=0)", 2D) = "white" {}
        _MainTex_R("Red Channel Tex", 2D) = "white" {}
        _MainTex_G("Green Channel Tex", 2D) = "white" {}
        _MainTex_B("Blue Channel Tex", 2D) = "white" {}
        _MainTex_A("Alpha Channel Tex", 2D) = "white" {}

        // Normal 맵 (쌍으로 지정)
        [Normal] _NormalTex("Base Normal", 2D) = "bump" {}
        [Normal] _NormalTex_R("Red Normal", 2D) = "bump" {}
        [Normal] _NormalTex_G("Green Normal", 2D) = "bump" {}
        [Normal] _NormalTex_B("Blue Normal", 2D) = "bump" {}
        [Normal] _NormalTex_A("Alpha Normal", 2D) = "bump" {}
        
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 라이트맵 키워드
            #pragma multi_compile _ LIGHTMAP_ON
            
            // [수정] 그림자 키워드 보강 (스크린 스페이스 그림자 및 추가 조명 그림자 지원)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            // URP 포그 지원을 위한 키워드
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 uv1          : TEXCOORD1; 
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float2 uv           : TEXCOORD1;
                
                float3 normalWS     : TEXCOORD3;
                float3 tangentWS    : TEXCOORD4;
                float3 bitangentWS  : TEXCOORD5;

                float2 lightmapUV   : TEXCOORD6;
                float4 shadowCoord  : TEXCOORD7;
                
                half fogFactor      : TEXCOORD8;
            };

            // 텍스처 선언
            TEXTURE2D(_AreaTex);        SAMPLER(sampler_AreaTex);
            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_MainTex_R);      SAMPLER(sampler_MainTex_R);
            TEXTURE2D(_MainTex_G);      SAMPLER(sampler_MainTex_G);
            TEXTURE2D(_MainTex_B);      SAMPLER(sampler_MainTex_B);
            TEXTURE2D(_MainTex_A);      SAMPLER(sampler_MainTex_A);

            TEXTURE2D(_NormalTex);
            TEXTURE2D(_NormalTex_R);
            TEXTURE2D(_NormalTex_G);
            TEXTURE2D(_NormalTex_B);
            TEXTURE2D(_NormalTex_A);

            CBUFFER_START(UnityPerMaterial)
                float4 _AreaTex_ST;
                float4 _MainTex_ST;
                float4 _MainTex_R_ST;
                float4 _MainTex_G_ST;
                float4 _MainTex_B_ST;
                float4 _MainTex_A_ST;
                half _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = normalInput.bitangentWS;

                output.uv = input.uv; 

                // 라이트맵 UV 계산
                output.lightmapUV = input.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #else
                    output.shadowCoord = float4(0, 0, 0, 0);
                #endif

                // 클립 스페이스의 Z값을 기반으로 포그 팩터 계산
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Area 마스크 텍스처 UV 계산 및 샘플링
                float2 uvArea = TRANSFORM_TEX(input.uv, _AreaTex);
                half4 areaMask = SAMPLE_TEXTURE2D(_AreaTex, sampler_AreaTex, uvArea);

                half vR = areaMask.r; 
                half vG = areaMask.g; 
                half vB = areaMask.b; 
                half vA = areaMask.a;

                // 2. 개별 Diffuse 및 Normal용 UV 계산
                float2 uvBase = TRANSFORM_TEX(input.uv, _MainTex);
                float2 uvR    = TRANSFORM_TEX(input.uv, _MainTex_R);
                float2 uvG    = TRANSFORM_TEX(input.uv, _MainTex_G);
                float2 uvB    = TRANSFORM_TEX(input.uv, _MainTex_B);
                float2 uvA    = TRANSFORM_TEX(input.uv, _MainTex_A);

                // 3. Diffuse 블렌딩
                half4 texBase = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvBase);
                half4 texR    = SAMPLE_TEXTURE2D(_MainTex_R, sampler_MainTex_R, uvR);
                half4 texG    = SAMPLE_TEXTURE2D(_MainTex_G, sampler_MainTex_G, uvG);
                half4 texB    = SAMPLE_TEXTURE2D(_MainTex_B, sampler_MainTex_B, uvB);
                half4 texA    = SAMPLE_TEXTURE2D(_MainTex_A, sampler_MainTex_A, uvA);

                half4 finalAlbedo = texBase;
                finalAlbedo = lerp(finalAlbedo, texR, vR);
                finalAlbedo = lerp(finalAlbedo, texG, vG);
                finalAlbedo = lerp(finalAlbedo, texB, vB);
                finalAlbedo = lerp(finalAlbedo, texA, vA);

                // 4. Normal 블렌딩
                half3 nBase = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex, sampler_MainTex, uvBase));
                half3 nR    = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex_R, sampler_MainTex_R, uvR));
                half3 nG    = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex_G, sampler_MainTex_G, uvG));
                half3 nB    = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex_B, sampler_MainTex_B, uvB));
                half3 nA    = UnpackNormal(SAMPLE_TEXTURE2D(_NormalTex_A, sampler_MainTex_A, uvA));

                half3 finalNormalTS = nBase;
                finalNormalTS = lerp(finalNormalTS, nR, vR);
                finalNormalTS = lerp(finalNormalTS, nG, vG);
                finalNormalTS = lerp(finalNormalTS, nB, vB);
                finalNormalTS = lerp(finalNormalTS, nA, vA);
                finalNormalTS = normalize(finalNormalTS);

                half3x3 tbn = half3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                half3 normalWS = normalize(TransformTangentToWorld(finalNormalTS, tbn));
                
                // 5. URP 라이팅 데이터 구성
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SampleLightmap(input.lightmapUV, normalWS);
                #else
                    inputData.bakedGI = SampleSH(normalWS);
                #endif
                
                // [핵심 수정] 그림자 좌표(ShadowCoord) 올바르게 연산
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    // 기존 GetVertexPositionInputs 버그 수정: 월드 좌표 기반으로 그림자 좌표 생성
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                // Surface 데이터 구성
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo.rgb;
                surfaceData.alpha = finalAlbedo.a;
                surfaceData.metallic = 0.0;
                surfaceData.specular = half3(0.0, 0.0, 0.0);
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = finalNormalTS;
                surfaceData.occlusion = 1.0;

                // 6. 최종 라이팅 연산
                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                // 씬에 설정된 포그 색상과 머티리얼의 색상을 혼합
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }
        
       // [수정된 ShadowCaster Pass - 버전 호환성 완벽 대응]
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // 포인트 라이트/스팟 라이트 그림자를 위한 키워드
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // [핵심 추가] URP 엔진에서 전달해주는 빛 관련 변수를 직접 선언
            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
                // 1. 오브젝트 공간 좌표를 월드 공간으로 변환
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // 2. 빛의 방향 계산 (디렉셔널 라이트 vs 포인트/스팟 라이트 분기)
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                
                // 3. 그림자 아티팩트(Acne)를 방지하기 위한 Bias 적용
                float3 positionWS_Biased = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                
                // 4. 클립 스페이스로 변환
                output.positionCS = TransformWorldToHClip(positionWS_Biased);
                
                // 5. Z값(깊이) 클리핑 보정
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0; // 그림자 맵에는 깊이값만 기록하므로 색상은 0 반환
            }
            ENDHLSL
        }
    }
}