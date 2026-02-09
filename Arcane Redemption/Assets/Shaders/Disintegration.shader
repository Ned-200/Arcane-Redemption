Shader "TNTC/Disintegration_URP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        [HDR]_AmbientColor("Ambient Color", Color) = (0.4,0.4,0.4,1)

        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpStr("Normal Map Strength", float) = 1

        _FlowMap("Flow (RG)", 2D) = "black" {}
        _DissolveTexture("Dissolve Texture", 2D) = "white" {}
        _DissolveColor("Dissolve Color Border", Color) = (1,1,1,1)
        _DissolveBorder("Dissolve Border", float) = 0.05

        _Exapnd("Expand", float) = 1
        _Weight("Weight", Range(0,1)) = 0
        _Direction("Direction", Vector) = (0,0,0,0)

        [HDR]_DisintegrationColor("Disintegration Color", Color) = (1,1,1,1)
        _Glow("Glow", float) = 1

        _Shape("Shape Texture", 2D) = "white" {}
        _R("Radius", float) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Cull Off
        LOD 100

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.6

            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag

            // URP keywords (keep minimal; add more if you need shadows/additional lights)
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ---- Textures / samplers ----
            TEXTURE2D(_MainTex);         SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);         SAMPLER(sampler_BumpMap);
            TEXTURE2D(_FlowMap);         SAMPLER(sampler_FlowMap);
            TEXTURE2D(_DissolveTexture); SAMPLER(sampler_DissolveTexture);
            TEXTURE2D(_Shape);           SAMPLER(sampler_Shape);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FlowMap_ST;
                float4 _Color;
                float4 _AmbientColor;

                float _BumpStr;

                float4 _DissolveColor;
                float _DissolveBorder;

                float _Exapnd;
                float _Weight;
                float4 _Direction;

                float4 _DisintegrationColor;
                float _Glow;

                float _R;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;   // IMPORTANT for normal mapping in URP
                float2 uv         : TEXCOORD0;
            };

            struct V2G
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2; // xyz = tangentWS, w = handedness
                float3 positionWS : TEXCOORD3;
            };

            struct G2F
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;      // x used for lerp to disintegration color, w used as "brightness"/mode
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float  tangentW    : TEXCOORD3;
                float3 positionWS  : TEXCOORD4;
            };

            V2G vert (Attributes v)
            {
                V2G o;
                o.positionOS = v.positionOS;
                o.uv = v.uv;

                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS   = TransformObjectToWorldNormal(v.normalOS);

                float3 tWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.tangentWS = float4(normalize(tWS), v.tangentOS.w);

                return o;
            }

            float remap(float value, float from1, float to1, float from2, float to2)
            {
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }

            float4 remapFlowTexture(float4 tex)
            {
                return float4(
                    remap(tex.x, 0, 1, -1, 1),
                    remap(tex.y, 0, 1, -1, 1),
                    0,
                    remap(tex.w, 0, 1, -1, 1)
                );
            }

            [maxvertexcount(7)]
            void geom(triangle V2G IN[3], inout TriangleStream<G2F> triStream)
            {
                float2 avgUV = (IN[0].uv + IN[1].uv + IN[2].uv) / 3.0;
                float3 avgPosOS = (IN[0].positionOS.xyz + IN[1].positionOS.xyz + IN[2].positionOS.xyz) / 3.0;
                float3 avgPosWS = (IN[0].positionWS + IN[1].positionWS + IN[2].positionWS) / 3.0;
                float3 avgNormalWS = normalize((IN[0].normalWS + IN[1].normalWS + IN[2].normalWS) / 3.0);

                float dissolve_value = SAMPLE_TEXTURE2D_LOD(_DissolveTexture, sampler_DissolveTexture, avgUV, 0).r;
                float t = saturate(_Weight * 2.0 - dissolve_value);

                // flow sampled in world XZ like your original
                float2 flowUV = avgPosWS.xz * _FlowMap_ST.xy + _FlowMap_ST.zw;
                float4 flowVector = remapFlowTexture(SAMPLE_TEXTURE2D_LOD(_FlowMap, sampler_FlowMap, flowUV, 0));

                float3 pseudoRandomPosWS = avgPosWS + _Direction.xyz + (_Exapnd);
                float3 pWS = lerp(avgPosWS, pseudoRandomPosWS, t);
                float radius = lerp(_R, 0.0, t);

                // Spawn billboard quad “particles” once t > 0
                if (t > 0.0)
                {
                    // camera right/up in world
                    float3 camRightWS = normalize(UNITY_MATRIX_I_V[0].xyz);
                    float3 camUpWS    = normalize(UNITY_MATRIX_I_V[1].xyz);

                    float halfS = 0.5 * radius;

                    float3 v0 = pWS + halfS * camRightWS - halfS * camUpWS;
                    float3 v1 = pWS + halfS * camRightWS + halfS * camUpWS;
                    float3 v2 = pWS - halfS * camRightWS - halfS * camUpWS;
                    float3 v3 = pWS - halfS * camRightWS + halfS * camUpWS;

                    G2F o;
                    o.normalWS   = avgNormalWS;
                    o.tangentWS  = float3(1,0,0);
                    o.tangentW   = 1;
                    o.color      = float4(1,1,1,1); // w>0 => debris mode

                    o.positionWS = v0; o.positionHCS = TransformWorldToHClip(v0); o.uv = float2(1,0); triStream.Append(o);
                    o.positionWS = v1; o.positionHCS = TransformWorldToHClip(v1); o.uv = float2(1,1); triStream.Append(o);
                    o.positionWS = v2; o.positionHCS = TransformWorldToHClip(v2); o.uv = float2(0,0); triStream.Append(o);
                    o.positionWS = v3; o.positionHCS = TransformWorldToHClip(v3); o.uv = float2(0,1); triStream.Append(o);

                    triStream.RestartStrip();
                }

                // Original triangle (still rendered until clipped by dissolve)
                [unroll] for (int j = 0; j < 3; j++)
                {
                    G2F o;
                    float3 posWS = IN[j].positionWS;

                    o.positionWS  = posWS;
                    o.positionHCS = TransformWorldToHClip(posWS);

                    // Apply main tex tiling/offset in frag
                    o.uv = IN[j].uv;

                    o.normalWS  = IN[j].normalWS;
                    o.tangentWS = IN[j].tangentWS.xyz;
                    o.tangentW  = IN[j].tangentWS.w;

                    o.color = float4(0,0,0,0); // w==0 => mesh mode
                    triStream.Append(o);
                }

                triStream.RestartStrip();
            }

            float3 SampleNormalWS(G2F i)
            {
                float2 uvMain = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                float3 nTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvMain));
                nTS.xy *= _BumpStr;
                nTS = normalize(nTS);

                float3 N = normalize(i.normalWS);
                float3 T = normalize(i.tangentWS);
                float3 B = normalize(cross(N, T) * i.tangentW);

                float3x3 TBN = float3x3(T, B, N);
                return normalize(mul(nTS, TBN));
            }

            half4 frag (G2F i) : SV_Target
            {
                float2 uvMain = i.uv * _MainTex_ST.xy + _MainTex_ST.zw;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain) * _Color;

                // URP main light (simple diffuse)
                float3 N = SampleNormalWS(i);

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(N, -L));

                half3 lit = (_AmbientColor.rgb + (mainLight.color.rgb * NdotL));
                col.rgb *= lit;

                // disintegration tint
                col = lerp(col, _DisintegrationColor, i.color.x);

                float brightness = i.color.w * _Glow;
                if (brightness > 0)
                    col.rgb *= (brightness + _Weight);

                float dissolve = SAMPLE_TEXTURE2D(_DissolveTexture, sampler_DissolveTexture, uvMain).r;

                // Mesh mode: clip + edge border
                if (i.color.w == 0)
                {
                    clip(dissolve - 2.0 * _Weight);

                    if (_Weight > 0)
                    {
                        // border highlight like your original (step-based)
                        float edge = step(dissolve - 2.0 * _Weight, _DissolveBorder);
                        col.rgb += (_DissolveColor.rgb * _Glow * edge);
                    }
                }
                else
                {
                    // Debris mode: shape cutout
                    float s = SAMPLE_TEXTURE2D(_Shape, sampler_Shape, uvMain).r;
                    if (s < 0.5)
                        discard;
                }

                return col;
            }
            ENDHLSL
        }

        // Optional: you can add a URP ShadowCaster pass later.
        // Start without it to ensure the forward pass compiles first.
    }
}
