// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Effect/AdditionEffectShader"
{
	Properties
	{
		_TintColor("Tint Color", Color) = (1,1,1,1)
		_MainTex ("Texture", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.CompareFunction)] unity_GUIZTestMode("ZTest", Float) = 4
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

		Blend SrcAlpha One
		ColorMask RGB
		Cull Off 
		Lighting Off 
		ZWrite Off 
		ZTest [unity_GUIZTestMode]
		Fog { Mode Off }

		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
				float4 vertex : SV_POSITION;
			};

			fixed4 _TintColor;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}
			
			fixed4 frag (v2f i) : COLOR
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return 2.0f* i.color  * col * _TintColor;
			}
			ENDCG
		}
	}
}
