//#define CHAR_NO_USE_FOG //캐릭터 포그 사용 안함
#define UNITY_APPLY_FOG_OFFSET UNITY_APPLY_FOG(i.fogCoord + 0.1f, col);

///////////////////////////////////////////////////////
//outline
struct outline_appdata
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
};

struct outline_v2f
{
	float4 vertex : POSITION;
};

float fov_based_outline_scale()
{
	return clamp(1.0f / unity_CameraProjection._m11 * 15.0f, 1.0f, 2.0f);
}

float distance_based_outline_scale(float4 vertex)
{
    float distPersent = length(ObjSpaceViewDir(vertex)) * 0.1f;
    
    return clamp(log10(distPersent) + 1.7, 0.0f, 2.0f);
}

fixed _Outline;
outline_v2f outline_vert(outline_appdata i)
{
	outline_v2f o;

	//오브젝트 공간에서 계산
	o.vertex.xyz = i.vertex.xyz + normalize(i.normal) * 
		_Outline * distance_based_outline_scale(i.vertex);

	o.vertex.w = i.vertex.w;
	o.vertex = UnityObjectToClipPos(o.vertex);

	return o;
}

fixed4 _OutlineColor;
fixed4 outline_frag(outline_v2f i) : SV_Target
{
	return _OutlineColor;
}

fixed4 _Color;
fixed4 outline_frag_alpha(outline_v2f i) : SV_Target
{
	fixed4 col = _OutlineColor;
	col.a = _Color.a;
	return col;
}

//////////////////////////////////////////////////////////

struct char_lit_appdata
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD0;	
	fixed4 color : COLOR;
};

struct char_lit_v2f
{
	float4 vertex   : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	float3 normal   : NORMAL;
	float3 rim      : TEXCOORD1;
	float2 cap      : TEXCOORD2; //Matcap
	fixed4 color    : COLOR; //VertexColor
};

sampler2D _MainTex;
float4 _MainTex_ST;
half _RimPower;
half _RimIntensity;
fixed4 _RimColor;
sampler2D _MatCap; //Matcap
sampler2D _MatCap1; //MatalicMatcap
fixed _MatCap1Intensity;

char_lit_v2f char_lit_vert(char_lit_appdata v)
{
	char_lit_v2f o;
	UNITY_INITIALIZE_OUTPUT(char_lit_v2f, o);

	o.vertex = UnityObjectToClipPos(v.vertex);
	o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

#ifdef _Spe_ON
	o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
#elif _Rim_ON
	o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
#endif

#ifdef _Rim_ON
	fixed3 viewDir = float3(0, 0, 1);
	fixed rim = pow(1 - saturate(dot(viewDir, o.normal)), _RimPower);
	o.rim = _RimColor * rim  * _RimIntensity;
#else
	o.rim = 0;
#endif

	o.color = v.color; //VertexColor
	float3 worldNormal = normalize(unity_WorldToObject[0].xyz * v.normal.x + unity_WorldToObject[1].xyz * v.normal.y + unity_WorldToObject[2].xyz * v.normal.z); //Matcap
	worldNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal); //Matcap
	o.cap.xy = worldNormal.xy * 0.5 + 0.5; //Matcap

	return o;
}

fixed4 _AddtiveColor;
fixed4 char_lit_frag(char_lit_v2f i) : SV_Target
{
	fixed4 col = tex2D(_MainTex, i.texcoord);
	fixed4 matcap = tex2D(_MatCap, i.cap);
	fixed4 mc1 = tex2D(_MatCap1, i.cap);
	fixed a = col.a;

	// 스펙큘러
	// NPC와 대화시 셰이더를 Character에서 OutlineShader로 교체, 스펙큘러, 림을 끔
//#ifdef _Spe_ON
	
//	fixed3 lightDir = fixed3(-0.3,0.25,0.4); //뷰 공간 라이트 방향
//	fixed3 viewDir = fixed3(0, 0, 0.8); //뷰 방향
//	i.normal = normalize(i.normal);

//	fixed3 h = normalize(lightDir + viewDir);
//	half NdotH = dot(i.normal, h) * 0.5 + 0.5;
//	half spec = pow(NdotH, 50) * col.a;
//	fixed4 specCol = spec * fixed4(1.0, 0.901, 0.76, 1);

//	col = col + (specCol * 0.5f);

//#endif

	//림
//#ifdef _Rim_ON
//	col.rgb = col.rgb + i.rim;
//#endif	
	
	col = lerp(col * _Color + _AddtiveColor, (col * matcap * unity_ColorSpaceDouble) * _Color + _AddtiveColor, i.color); //Matcap+VertexColor
#ifdef _Spe_ON
	col = lerp(col, col * mc1 * unity_ColorSpaceDouble, a * _MatCap1Intensity);
#endif

	return col;
}

struct appdata_base_ex
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float2 texcoord : TEXCOORD0;
	float2 texcoord1 : TEXCOORD1;
	fixed4 color : COLOR;	
};

struct char_lit_v2f_ex
{
	float4 vertex : SV_POSITION;
	float2 texcoord : TEXCOORD0;
	float3 normal : NORMAL;
	//float4 rim : TEXCOORD1;
	float3 cap : TEXCOORD1; //Matcap
	fixed4 color : COLOR; //VertexColor
};

uniform float4 _MeshIndex;
uniform sampler2D _Cape;
uniform float4 _Cape_ST;
uniform sampler2D _Head;
uniform float4 _Head_ST;
uniform sampler2D _Pants;
uniform float4 _Pants_ST;
uniform sampler2D _Upper;
uniform float4 _Upper_ST;
uniform sampler2D _Hair;
uniform float4 _Hair_ST;

char_lit_v2f_ex char_lit_vert_ex(appdata_base_ex v)
{
	char_lit_v2f_ex o;

	o.vertex = UnityObjectToClipPos(v.vertex);
	o.texcoord = v.texcoord;

//#ifdef _Spe_ON
//	o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
//#elif _Rim_ON
//	o.normal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
//#endif

//#ifdef _Rim_ON
//	fixed3 viewDir = float3(0, 0, 1);
//	fixed rim = pow(1 - saturate(dot(viewDir, o.normal)), _RimPower);
//	o.rim = _RimColor * rim * _RimIntensity;
//	o.rim.w = v.texcoord1.x;
//#else
	//o.rim = float4(0,0,0, v.texcoord1.x);
	o.cap.z = v.texcoord1.x;
//#endif

	o.color = v.color; //VertexColor
	float3 worldNormal = normalize(unity_WorldToObject[0].xyz * v.normal.x + unity_WorldToObject[1].xyz * v.normal.y + unity_WorldToObject[2].xyz * v.normal.z); //Matcap
	worldNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal); //Matcap
	o.cap.xy = worldNormal.xy * 0.5 + 0.5; //Matcap

	return o;
}

fixed4 char_lit_frag_ex(char_lit_v2f_ex i) : SV_Target
{

	int temp_output_414_0 = (_MeshIndex.x + _MeshIndex.y);
	int temp_output_417_0 = (temp_output_414_0 + _MeshIndex.z);
	//fixed4 c = lerp(tex2D(_Hair, i.texcoord * _Hair_ST.xy + _Hair_ST.zw), tex2D(_Upper, i.texcoord * _Upper_ST.xy + _Upper_ST.zw), step(_MeshIndex.x, i.rim.a));
	//fixed4 b = lerp(c, tex2D(_Pants, i.texcoord * _Pants_ST.xy + _Pants_ST.zw), step(temp_output_414_0, i.rim.a));
	//fixed4 a = lerp(b, tex2D(_Head, i.texcoord * _Head_ST.xy + _Head_ST.zw), step(temp_output_417_0, i.rim.a));
	//fixed4 col = lerp(a, tex2D(_Cape, i.texcoord * _Cape_ST.xy + _Cape_ST.zw), step((temp_output_417_0 + _MeshIndex.w), i.rim.a));

	// Sampler의 _ST가 필요없음.
	//fixed4 col = lerp(lerp(lerp(lerp(tex2D(_Hair, i.texcoord * _Hair_ST.xy + _Hair_ST.zw), tex2D(_Upper, i.texcoord * _Upper_ST.xy + _Upper_ST.zw), step(_MeshIndex.x, i.cap.z)), tex2D(_Pants, i.texcoord * _Pants_ST.xy + _Pants_ST.zw), step(temp_output_414_0, i.cap.z)), tex2D(_Head, i.texcoord * _Head_ST.xy + _Head_ST.zw), step(temp_output_417_0, i.cap.z)), tex2D(_Cape, i.texcoord * _Cape_ST.xy + _Cape_ST.zw), step((temp_output_417_0 + _MeshIndex.w), i.cap.z));

	fixed4 col = lerp(lerp(lerp(lerp(tex2D(_Hair, i.texcoord ), tex2D(_Upper, i.texcoord ), step(_MeshIndex.x, i.cap.z)), tex2D(_Pants, i.texcoord ), step(temp_output_414_0, i.cap.z)), tex2D(_Head, i.texcoord ), step(temp_output_417_0, i.cap.z)), tex2D(_Cape, i.texcoord ), step((temp_output_417_0 + _MeshIndex.w), i.cap.z));

	fixed4 matcap = tex2D(_MatCap, i.cap); //Matcap
	fixed4 mc1 = tex2D(_MatCap1, i.cap);
	fixed a = col.a;

	// 스펙큘러
	// NPC와 대화시 셰이더를 Character에서 OutlineShader로 교체, 스펙큘러, 림을 끔
//#ifdef _Spe_ON

//	fixed3 lightDir = fixed3(-0.3,0.25,0.4); //뷰 공간 라이트 방향
//	fixed3 viewDir = fixed3(0, 0, 0.8); //뷰 방향
//	i.normal = normalize(i.normal);

//	fixed3 h = normalize(lightDir + viewDir);
//	half NdotH = dot(i.normal, h) * 0.5 + 0.5;
//	half spec = pow(NdotH, 50) * col.a;
//	fixed4 specCol = spec * fixed4(1.0, 0.901, 0.76, 1);

//	col = col + (specCol * 0.5f);
//#endif

	//림
//#ifdef _Rim_ON
//	col.rgb = col.rgb + i.rim.rgb;
//#endif
	
	col = lerp(col * _Color + _AddtiveColor, (col * matcap * unity_ColorSpaceDouble) * _Color + _AddtiveColor, i.color); //Matcap+VertexColor
	col = lerp(col, col * mc1 * unity_ColorSpaceDouble, a * _MatCap1Intensity);

	return col;
}

//?? 이게 모지
/*
fixed4 char_lit_frag_alpha(char_lit_v2f i) : SV_Target
{
	fixed4 col = tex2D(_MainTex, i.texcoord) * _Color;

#ifdef _Rim_ON
	col.rgb = col.rgb + i.rim;
#endif

	col.a = _Color.a;

	return col;
}
*/

// CharacterDissolveShader ////////////////////////////////////////////////
sampler2D _DissolveTex;
float4 _DissolveTex_ST;
sampler2D _BurnTex;
float4 _BurnTex_ST;
fixed _DissolvePower;

fixed4 char_lit_dissolve_frag(char_lit_v2f i) : SV_Target
{
	fixed4 col = tex2D(_MainTex, i.texcoord) * _Color + _AddtiveColor;

	//디졸브
	//디졸브 택스쳐로 컷오프 영역
	fixed4 dissolveTex = tex2D(_DissolveTex, TRANSFORM_TEX(i.texcoord, _DissolveTex));
	fixed dissolve = dissolveTex.r + ((_DissolvePower * 1.0) +- 0.5);
	clip(dissolve - 0.5);

	//컷오프 영역(타는 영역) 경계선 구함
	//burnUv를 dissolve를 범위를 키워서 해상도를 넓혀서 샤픈을 준다
	//타는영역만 구하기 위해 역수를 구해 uv로 쓴다.
	fixed2 burnUv = fixed2(1 - saturate((dissolve * 4.0 +- 2.0)), 0); //1D 택스쳐라 R값만 씀
	fixed4 burnCol = tex2D(_BurnTex, TRANSFORM_TEX(burnUv, _BurnTex));

	//림
#ifdef _Rim_ON
		col.rgb = col.rgb + i.rim;
#endif

	return lerp(col, (burnCol) + (col * 0.3), burnCol.r);
} 