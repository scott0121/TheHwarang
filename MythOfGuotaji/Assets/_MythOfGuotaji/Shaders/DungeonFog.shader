Shader "MythOfGuotaji/DungeonFog"
{
    Properties
    {
        [Header(Fog Appearance)]
        _FogColor ("Fog Color", Color) = (0.015, 0.015, 0.025, 1.0)
        _MaxDarkness ("Max Darkness (Opacity)", Range(0.0, 1.0)) = 0.90

        [Header(Front Vision 160 Degrees)]
        _FovAngle ("Front FOV Angle (Degrees)", Float) = 160.0
        _FovSoftness ("FOV Edge Softness", Range(0.01, 0.2)) = 0.05

        [Header(Front Distance Visibility)]
        _ClearDistance ("Clear Distance (Fully Transparent)", Float) = 30.0
        _FalloffDistance ("Falloff Distance (Fully Dark)", Float) = 45.0

        [Header(Omnidirectional and Rear Vision)]
        _AlwaysClearRadius ("Always Clear Radius (10m)", Float) = 10.0
        _RearDarkDistance ("Rear Dark Distance (15m)", Float) = 15.0

        [Header(Doorway Vision Angle)]
        _DoorFadeAngle ("Door Fade Angle (Degrees)", Float) = 15.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent+150" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "DungeonFogPass"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _MaxDarkness;
                float _FovAngle;
                float _FovSoftness;
                float _ClearDistance;
                float _FalloffDistance;
                float _AlwaysClearRadius;
                float _RearDarkDistance;
                float _DoorFadeAngle;
            CBUFFER_END

            // C# 스크립트(DungeonFogManager)에서 매 프레임 주입하는 동적 전역 변수
            float4 _CharacterWorldPos;     // xyz: 캐릭터 실시간 월드 위치
            float4 _CharacterForward;      // xyz: 캐릭터 전방 벡터 (마우스 조준 방향)
            float4 _CurrentRoomCenter;      // xyz: 캐릭터가 위치한 방 중심 월드 좌표
            float _CurrentRoomHalfSize;     // 방 반너비(m)
            float4 _DoorData0;              // xyz: 0번 문 중심 위치, w: 문 반너비(m)
            float4 _DoorData1;              // xyz: 1번 문 중심 위치, w: 문 반너비(m)
            float4 _DoorData2;              // xyz: 2번 문 중심 위치, w: 문 반너비(m)
            float4 _DoorData3;              // xyz: 3번 문 중심 위치, w: 문 반너비(m)
            float _ActiveDoorCount;         // 현재 방의 개방된 유효 출입문 수

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;
                return output;
            }

            float ComputeDoorAngleOcclusion(float2 charPos, float2 pPos, float4 doorData, float fadeAngleDeg)
            {
                float2 doorPos = doorData.xz;
                float doorHalfWidth = doorData.w;

                // 문이 닫혀있거나 너비가 없으면 완전 차단
                if (doorHalfWidth <= 0.001)
                {
                    return 1.0;
                }

                float2 toDoor = doorPos - charPos;
                float2 toP = pPos - charPos;

                float distToDoor = length(toDoor);
                float distToP = length(toP);

                if (distToP < 0.01)
                {
                    return 0.0;
                }

                // 1) 캐릭터가 문턱(Door Threshold) 반경 내에 도달했을 때:
                // 문틀 중심으로부터 doorHalfWidth + 0.5m 이내에 있으면 문 자체에 의한 차단 완전 해제 (0.0)
                // (문턱에 서서 앞을 바라보는 중이므로 통로와 앞쪽 방 시야가 100% 트임)
                float thresholdDist = doorHalfWidth + 0.5;
                if (distToDoor <= thresholdDist)
                {
                    return 0.0;
                }

                // 2) 문 반대 방향 판정
                float dotProd = dot(toDoor, toP);
                if (dotProd <= 0.0)
                {
                    return 1.0; // 시야 차단
                }

                // 3) 캐릭터 시점 기준 문의 시야각(각도 반너비) 계산
                float doorAngleRad = atan2(doorHalfWidth, distToDoor);

                // 4) 캐릭터에서 점 P로 향하는 벡터와 문 중심 벡터 간의 사잇각 계산
                float cosAngle = dotProd / (distToDoor * distToP);
                cosAngle = clamp(cosAngle, -1.0, 1.0);
                float angleRad = acos(cosAngle);

                // 문 개방 시야각 경계로부터 벗어난 각도 (Radian -> Degree)
                float deltaAngleDeg = (angleRad - doorAngleRad) * 57.2957795; // 180 / PI

                float occlusion = 1.0;
                // 문 개방 각도 안쪽 (deltaAngle <= 0): 완전 개방 (0.0)
                if (deltaAngleDeg <= 0.0)
                {
                    occlusion = 0.0;
                }
                // 문에 가려진 0도 ~ 15도: 점진적으로 불투명(0.0 -> 1.0)
                else if (deltaAngleDeg <= fadeAngleDeg)
                {
                    occlusion = deltaAngleDeg / max(fadeAngleDeg, 0.001);
                }
                else
                {
                    occlusion = 1.0;
                }

                // 문턱에 가까워질수록 부드럽게 감쇄하여 팝 현상 방지
                float nearBlend = saturate((distToDoor - thresholdDist) / 2.5);
                return occlusion * nearBlend;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 charPos = _CharacterWorldPos.xz;
                float2 pPos = input.positionWS.xz;

                float2 toP = pPos - charPos;
                float dist = length(toP);

                // ========================================================
                // 1. 캐릭터 전방 160도 시야각 및 후방(뒷면) 10m~15m 감쇄 계산
                // - 0m ~ 10m: 캐릭터 주변 10m는 전방/후방 불문 항상 완전 투명 (0.0)
                // - 후방 (160도 외): 10m 벗어난 부분부터 15m까지 선형 페이드, 15m 이상 완전 암전 (1.0)
                // ========================================================
                float2 charFwd = _CharacterForward.xz;
                float fwdLen = length(charFwd);
                if (fwdLen > 0.001)
                {
                    charFwd /= fwdLen;
                }
                else
                {
                    charFwd = float2(0.0, 1.0);
                }

                // 후방 거리 감쇄 factor (10m 이하: 0.0, 10m~15m: 선형 증가, 15m 이상: 1.0)
                float rearDistFactor = 0.0;
                float clearR = _AlwaysClearRadius > 0.0 ? _AlwaysClearRadius : 10.0;
                float darkR = _RearDarkDistance > clearR ? _RearDarkDistance : 15.0;

                if (dist <= clearR)
                {
                    rearDistFactor = 0.0;
                }
                else if (dist >= darkR)
                {
                    rearDistFactor = 1.0;
                }
                else
                {
                    rearDistFactor = (dist - clearR) / max(darkR - clearR, 0.001);
                }

                // 전방 160도 판정 (반각 80도, thresholdCos = cos(80도) ≈ 0.1736)
                float fovAngle = _FovAngle > 10.0 ? _FovAngle : 160.0;
                float halfFovRad = radians(fovAngle * 0.5);
                float thresholdCos = cos(halfFovRad);

                float2 toPDir = toP / max(dist, 0.001);
                float frontDot = dot(charFwd, toPDir);

                float softness = max(_FovSoftness, 0.01);
                // frontDot >= thresholdCos + softness: 전방 (angleOcclusion = 0.0)
                // frontDot <= thresholdCos - softness: 후방 (angleOcclusion = 1.0)
                float angleOcclusion = 1.0 - smoothstep(thresholdCos - softness, thresholdCos + softness, frontDot);

                // 후방 차단 오클루전 (10m 이내는 rearDistFactor=0이므로 완전 투명)
                float rearOcclusion = angleOcclusion * rearDistFactor;

                // ========================================================
                // 2. 전방 거리 감쇄 (0m~30m: 0.0, 30m~45m: 선형 페이드, 45m+: 1.0)
                // ========================================================
                float frontDistFactor = 0.0;
                float clearDist = _ClearDistance > 0.0 ? _ClearDistance : 30.0;
                float falloffDist = _FalloffDistance > clearDist ? _FalloffDistance : 45.0;

                if (dist <= clearDist)
                {
                    frontDistFactor = 0.0;
                }
                else if (dist >= falloffDist)
                {
                    frontDistFactor = 1.0;
                }
                else
                {
                    frontDistFactor = (dist - clearDist) / max(falloffDist - clearDist, 0.001);
                }

                // ========================================================
                // 3. 문 기준 시야각 체크 (문 가림 및 15도 페이드)
                // ========================================================
                float doorOcclusion = 0.0;
                float halfSize = _CurrentRoomHalfSize > 0.0 ? _CurrentRoomHalfSize : 19.5;

                // 캐릭터가 위치한 현재 방 내부인지 판별
                float2 offsetFromRoomCenter = abs(pPos - _CurrentRoomCenter.xz);
                bool isInsideCurrentRoom = (offsetFromRoomCenter.x <= halfSize + 0.2) &&
                                          (offsetFromRoomCenter.y <= halfSize + 0.2);

                if (!isInsideCurrentRoom)
                {
                    // 현재 방 바깥은 열려있는 문들을 통해서만 시야 확보 가능
                    float minOcclusion = 1.0;
                    float doorFade = _DoorFadeAngle > 0.0 ? _DoorFadeAngle : 15.0;

                    if (_ActiveDoorCount > 0.5)
                    {
                        minOcclusion = min(minOcclusion, ComputeDoorAngleOcclusion(charPos, pPos, _DoorData0, doorFade));
                    }
                    if (_ActiveDoorCount > 1.5)
                    {
                        minOcclusion = min(minOcclusion, ComputeDoorAngleOcclusion(charPos, pPos, _DoorData1, doorFade));
                    }
                    if (_ActiveDoorCount > 2.5)
                    {
                        minOcclusion = min(minOcclusion, ComputeDoorAngleOcclusion(charPos, pPos, _DoorData2, doorFade));
                    }
                    if (_ActiveDoorCount > 3.5)
                    {
                        minOcclusion = min(minOcclusion, ComputeDoorAngleOcclusion(charPos, pPos, _DoorData3, doorFade));
                    }

                    doorOcclusion = minOcclusion;
                }

                // ========================================================
                // 4. 전방 거리 감쇄, 문 차단 감쇄, 후방 차단 결합
                // ========================================================
                float totalOcclusion = max(max(frontDistFactor, doorOcclusion), rearOcclusion);
                float finalAlpha = totalOcclusion * _MaxDarkness;

                return float4(_FogColor.rgb, clamp(finalAlpha, 0.0, _MaxDarkness));
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
