Shader "Custom/IDPackTerrain"
{
	Properties
	{
		_Noise("_Noise", 2D) = "white" {}
		_TexArrayBlend("_TexArrayBlend", 2D) = "white" {}
	}

	// Universal Render Pipeline subshader. If URP is installed this will be used.
	SubShader
	{
			Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}

			Pass
			{
				Tags { "LightMode" = "UniversalForward" }

				HLSLPROGRAM
				#pragma multi_compile _ _HeightBlend
				
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing
				

				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

				struct Attributes
				{
					float4 positionOS   : POSITION;
					float2 uv           : TEXCOORD0;

					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct Varyings
				{
					float2 uv : TEXCOORD0;
					float3 positionWS : TEXCOORD1;
					float3 viewDirWS : TEXCOORD2;
					float4 positionHCS  : SV_POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				TEXTURE2D_ARRAY(_AlbedoArray);         SAMPLER(sampler_AlbedoArray);
				TEXTURE2D_ARRAY(_NormalArray);         SAMPLER(sampler_NormalArray);
				TEXTURE2D_ARRAY(_HeightArray);         SAMPLER(sampler_HeightArray);

				TEXTURE2D(_Noise);        SAMPLER(sampler_Noise);
				Texture2D _TexArrayBlend;
				float4 _TexArrayBlend_TexelSize;
				float4 _AlbedoArray_TexelSize;
				float2 _AlphaMapSize;
				int _TotalArrayLength;
				float _BlendScale[8];
				float _BlendSharpness[8];
				float _HeightBlendEnd;

				Varyings vert(Attributes IN)
				{
					Varyings OUT;
					UNITY_SETUP_INSTANCE_ID(IN);
					UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

					VertexPositionInputs Attributes = GetVertexPositionInputs(IN.positionOS.xyz);
					OUT.positionWS = Attributes.positionWS;
					OUT.positionHCS = Attributes.positionCS;
					OUT.viewDirWS = GetWorldSpaceViewDir(Attributes.positionWS);
					OUT.uv = 1 - IN.uv;
					return OUT;
				}

				half4 frag(Varyings IN) : SV_Target
				{
					float2 texUV = IN.uv * 1024 / 30;
					float texSize = _AlphaMapSize.x;
					float texNei = _AlphaMapSize.y;
					float2 orignUV = IN.uv * texSize;
					int2 uvInt1 = floor(orignUV);
					uint2 uvInt2 = uvInt1 + uint2(0, 1);
					if(orignUV.x - uvInt1.x > orignUV.y - uvInt1.y)
					{
						uvInt2 = uvInt1 + uint2(1, 0);
					}
					uint2 uvInt3 = uvInt1 + uint2(1, 1);
					uint blendData1 = _TexArrayBlend.Load(int3(uvInt1, 0)).r * 0xFFFF;
					uint blendData2 = _TexArrayBlend.Load(int3(uvInt2, 0)).r * 0xFFFF;
					uint blendData3 = _TexArrayBlend.Load(int3(uvInt3, 0)).r * 0xFFFF;
					int and5 = (1 << 5) - 1;
					int2 blend1 = int2(blendData1 >> 11, (blendData1 >> 6) & and5);
					int2 blend2 = int2(blendData2 >> 11, (blendData2 >> 6) & and5);
					int2 blend3 = int2(blendData3 >> 11, (blendData3 >> 6) & and5);
					float2 uv1 = uvInt1 * texNei;
					float2 uv2 = uvInt2 * texNei;
					float2 uv3 = uvInt3 * texNei;
					float w3 = ((uv1.y - uv2.y)*IN.uv.x + (uv2.x - uv1.x)*IN.uv.y + uv1.x * uv2.y - uv2.x * uv1.y) / ((uv1.y - uv2.y) * uv3.x + (uv2.x - uv1.x)*uv3.y + uv1.x*uv2.y - uv2.x*uv1.y);
					float w2 = ((uv1.y - uv3.y)*IN.uv.x + (uv3.x - uv1.x)*IN.uv.y + uv1.x * uv3.y - uv3.x * uv1.y) / ((uv1.y - uv3.y) * uv2.x + (uv3.x - uv1.x)*uv2.y + uv1.x*uv3.y - uv3.x*uv1.y);
					float w1 = 1 - w2 - w3;
					int and6 = (1 << 6) - 1;
					float inv64 = 0.015625;//1/64
					float diff1 =  ((blendData1 & and6) + 1) * inv64;
					float2 weight1 = float2(0.5*(1 + diff1), 0.5 *(1-diff1)) * w1;
					float diff2 =  ((blendData2 & and6) + 1) * inv64;
					float2 weight2 = float2(0.5*(1 + diff2), 0.5 *(1-diff2)) * w2;
					float diff3 =  ((blendData3 & and6) + 1) * inv64;
					float2 weight3 = float2(0.5*(1 + diff3), 0.5 *(1-diff3)) * w3;
					float2 m[8];
					int i,j;
					for(i=0;i<8;i++)
						m[i] = float2(i, 0);
					#if _HeightBlend
					float dis = distance(IN.positionWS, _WorldSpaceCameraPos);
					float blendA = saturate((_HeightBlendEnd - dis) / _HeightBlendEnd);
					//return blendA;
					float height0,height1,bf1,bf2,bf12;
					if(blend1.x != blend1.y)
					{
						height0 = SAMPLE_TEXTURE2D_ARRAY(_HeightArray, sampler_HeightArray, texUV, blend1.x).a;//此处应该使用alpha8的HeightMap,测试暂用AlbedoArray
						height1 = SAMPLE_TEXTURE2D_ARRAY(_HeightArray, sampler_HeightArray, texUV, blend1.y).a;//此处应该使用alpha8的HeightMap,测试暂用AlbedoArray
						bf1 = saturate((blendA * (height0 - height1) + (weight1.x - 0.5) * _BlendScale[blend1.x]) * _BlendSharpness[blend1.x] + 0.5);
						bf2 = saturate((blendA * (height1 - height0) + (weight1.y - 0.5) * _BlendScale[blend1.y]) * _BlendSharpness[blend1.y] + 0.5);
						bf12 = max(bf1 + bf2, 0.001);bf1 /= bf12;bf2 /=bf12;
						weight1.x *= bf1;
						weight1.y *= bf2;
					}

					if(blend2.x != blend2.y)
					{
						height0 = SAMPLE_TEXTURE2D_ARRAY(_HeightArray, sampler_HeightArray, texUV, blend2.x).a;//此处应该使用alpha8的HeightMap,测试暂用AlbedoArray
						height1 = SAMPLE_TEXTURE2D_ARRAY(_HeightArray, sampler_HeightArray, texUV, blend2.y).a;//此处应该使用alpha8的HeightMap,测试暂用AlbedoArray
						bf1 = saturate((blendA * (height0 - height1) + (weight2.x - 0.5) * _BlendScale[blend2.x]) * _BlendSharpness[blend2.x] + 0.5);
						bf2 = saturate((blendA * (height1 - height0) + (weight2.y - 0.5) * _BlendScale[blend2.y]) * _BlendSharpness[blend2.y] + 0.5);
						bf12 = max(bf1 + bf2, 0.001);bf1 /= bf12;bf2 /=bf12;
						weight2.x *= bf1;
						weight2.y *= bf2;
					}

					if(blend3.x != blend3.y)
					{
						height0 = SAMPLE_TEXTURE2D_ARRAY(_HeightArray, sampler_HeightArray, texUV, blend3.x).a;//此处应该使用alpha8的HeightMap,测试暂用AlbedoArray
						height1 = SAMPLE_TEXTURE2D_ARRAY(_HeightArray, sampler_HeightArray, texUV, blend3.y).a;//此处应该使用alpha8的HeightMap,测试暂用AlbedoArray
						bf1 = saturate((blendA * (height0 - height1) + (weight3.x - 0.5) * _BlendScale[blend3.x]) * _BlendSharpness[blend3.x] + 0.5);
						bf2 = saturate((blendA * (height1 - height0) + (weight3.y - 0.5) * _BlendScale[blend3.y]) * _BlendSharpness[blend3.y] + 0.5);
						bf12 = max(bf1 + bf2, 0.001);bf1 /= bf12;bf2 /=bf12;
						weight3.x *= bf1;
						weight3.y *= bf2;
					}
					#endif
					
					m[blend1.x].y += weight1.x;
					m[blend1.y].y += weight1.y;
					m[blend2.x].y += weight2.x;
					m[blend2.y].y += weight2.y;
					m[blend3.x].y += weight3.x;
					m[blend3.y].y += weight3.y;

					
					float2 temp;
					for(j = 0;j < 3;j++)
					{
						for(i = 0; i < 7 - j;i++)
						{
							if(m[i].y > m[i+1].y)
							{
								temp = m[i+1];
								m[i+1] = m[i];
								m[i] = temp;
							}
						}
					}
					int index1 = round(m[7].x);
					int index2 = round(m[6].x);
					int index3 = round(m[5].x);
					half4 albedo1 = SAMPLE_TEXTURE2D_ARRAY(_AlbedoArray, sampler_AlbedoArray, texUV, index1);
					half4 albedo2 = SAMPLE_TEXTURE2D_ARRAY(_AlbedoArray, sampler_AlbedoArray, texUV, index2);
					half4 albedo3 = SAMPLE_TEXTURE2D_ARRAY(_AlbedoArray, sampler_AlbedoArray, texUV, index3);
					//float aveHeight = (albedo1.a + albedo2.a + albedo3.a) * 0.333333;
					
					//w1 = saturate((_BlendA[index1] * (albedo1.a - aveHeight) + (m[7].y - 0.5) * _BlendScale[index1]) * _BlendSharpness[index1] + 0.5);
					//w2 = saturate((_BlendA[index2] * (albedo2.a - aveHeight) + (m[6].y - 0.5) * _BlendScale[index2]) * _BlendSharpness[index2] + 0.5);
					//w3 = saturate((_BlendA[index3] * (albedo3.a - aveHeight) + (m[5].y - 0.5) * _BlendScale[index3]) * _BlendSharpness[index3] + 0.5);
					w1 = m[7].y;
					w2 = m[6].y;
					w3 = m[5].y;
					float tt = 1.0/ (w1 + w2 + w3);
					w1 *=tt;w2*=tt;w3*=tt;
					half3 albedo = albedo1.rgb * w1 + albedo2.rgb * w2  + albedo3 * w3;
					half4 nrm1 = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, texUV, index1);
					half4 nrm2 = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, texUV, index2);
					half4 nrm3 = SAMPLE_TEXTURE2D_ARRAY(_NormalArray, sampler_NormalArray, texUV, index3);

					half3 normal1 = UnpackNormalScale(nrm1, 1.0f);
					half3 normal2 = UnpackNormalScale(nrm2, 1.0f);
					half3 normal3 = UnpackNormalScale(nrm3, 1.0f);
					
					half3 normal =  normal1.rgb * w1 + normal2.rgb * w2  + normal3 * w3;
					normal = normalize(normal);
					
					BRDFData brdfData;
					half alpha = 1;
					InitializeBRDFData(albedo, 0, half3(0,0,0), 0, alpha, brdfData);
					half4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
					Light mainLight = GetMainLight(shadowCoord, IN.positionWS, 0);

					BRDFData brdfDataClearCoat = (BRDFData)0; 
					half3 color = LightingPhysicallyBased(brdfData, brdfDataClearCoat,
									mainLight,
									normal.xzy, IN.viewDirWS,
									0, false);
					return  half4(color, 1);
				}

				ENDHLSL
			}
	}
}
