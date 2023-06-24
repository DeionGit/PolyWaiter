Shader "Hidden/Technie/RigidColliderCreatorOverlay"
{
	Properties
	{
		//_Tint("Tint", Color) = (1.0, 1.0, 1.0, 1.0)
		_Color ("Color", Color) = (1,1,1,1)
		_RimColor("Rim Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_RimPower("Rim Power", Range(0.01, 10.0)) = 3.0
	}
	SubShader
	{
		Tags
		{
			"Queue"="Transparent-1"
			"RenderType"="Transparent"
		}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Offset -1.0, -1.0
			LOD 200

			CGPROGRAM
			#pragma vertex vert_surf
			#pragma fragment frag_surf
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 pos		: SV_POSITION;
				float4 posWorld : TEXCOORD0;
				fixed4 color	: COLOR;
				float3 normal	: NORMAL;
			};

			fixed4 _Color;
			float4 _RimColor;
			float _RimPower;

			v2f vert_surf (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.color = v.color * _Color;
				o.normal = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject));
				return o;
			}

			float4 frag_surf(v2f IN) : COLOR
			{
				float3 normal = normalize(IN.normal);
				float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - IN.posWorld.xyz);
				float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				float3 rim = pow(1.0 - saturate(dot(viewDirection, normal)), _RimPower);
				float3 rimLighting = rim * _RimColor.rgb;

				//return IN.color;
				return IN.color + float4(rimLighting.rgb, 0.0);
			}
			ENDCG
		}
	}
}
