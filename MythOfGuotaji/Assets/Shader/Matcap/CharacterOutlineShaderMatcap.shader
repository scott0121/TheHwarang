Shader "ValkiriII/CharacterOutlineShaderMatcap"
{
	Properties
	{
		//[Header(Base)]
		_MainTex ("Texture", 2D) = "white" {}
		_MatCap ("Matcap", 2D) = "white" {}  //Matcap
		_MatCap1 ("Metallic Matcap", 2D) = "white" {}
		[HideInInspector]_Color("Tint Color", Color) = (1, 1, 1, 1)
		[HideInInspector]_AddtiveColor("Addtive Color", Color) = (0, 0, 0, 1)
		_MatCap1Intensity ("Metallic Intensity", Range(0.0, 1.0)) = 0.7

		//[Header(Specular(A))]
		// NPC와 대화시 셰이더를 Character에서 OutlineShader로 교체, 스펙큘러, 림을 끔
		[MaterialToggle(_Spe_ON)] _Spe("Enable Specular", float) = 1

		//[Header(Rim)]
		// 이펙트에서 사용
		//[MaterialToggle(_Rim_ON)] _Rim("Enable Rim", float) = 1
		//_RimColor ("Rim Color", Color) = (0.40, 0.458, 0.486, 1)
		//_RimPower ("Rim Range", Range(0, 16)) = 6.0
		//_RimIntensity ("Rim Intensity", Range(0, 4)) = 0.7

		//[Space(20)]
		// 아웃라인의 스케일, 색을 코드에서 받아옴
		[Header(Outline)]
		_Outline("Outline Width", Range(0.0, 0.03)) = 0.003
		_OutlineColor("OutlineColor", Color) = (0.41, 0.36, 0.26, 1)
	}

	SubShader
	{
		Tags { "Queue"="Geometry+1" "RenderType"="Opaque" }
		LOD 200
		Lighting Off

		Pass
		{
			Name "OUTLINE"

			Cull front
			ZWrite off

			/*stencil
			{
				ref 0
				comp equal
				pass incrSat
				WriteMask 1
				ReadMask 1
			}*/

			CGPROGRAM
			#pragma vertex outline_vert
			#pragma fragment outline_frag		
			#include "UnityCG.cginc"
			#include "CharacterCG_test.cginc" //Matcap
			ENDCG
		}

		Pass
		{
			Name "BASE"

			CGPROGRAM
			#pragma vertex char_lit_vert
			#pragma fragment char_lit_frag
			#include "UnityCG.cginc"
			#include "CharacterCG_test.cginc" //Matcap

			//#pragma multi_compile_fog // make fog work
			#pragma shader_feature _Spe_ON
			#pragma shader_feature _Rim_ON
			ENDCG
		}
	}
	Fallback "Diffuse"
}