Shader "Custom/DisappearMask"
{
    Properties
    {
        [Header(Base Properties)]
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        
        [Header(Disappear Settings)]
        _Cutoff ("Cutoff Position", Range(-10, 10)) = 1.0
        _Offset ("Y Offset", Float) = 0
        _UseWorldSpace ("Use World Space", Float) = 1
        _Direction ("Direction (1=TopDown, -1=BottomUp)", Float) = 1
        _FadeRange ("Fade Range", Range(0.01, 5)) = 0.5
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
            "ForceNoShadowCasting"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Lighting Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 color : COLOR;
                float clipSpaceY : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            float _UseWorldSpace;
            float _Offset;
            float _Direction;
            float _FadeRange;
            fixed4 _Color;

            v2f vert (appdata v) {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // 关键：正确传递颜色（TextMesh的顶点颜色通常包含文本颜色）
                o.color = v.color * _Color;
                
                o.clipSpaceY = o.vertex.y / o.vertex.w;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 选择使用世界空间还是裁剪空间
                float comparePos;
                if (_UseWorldSpace > 0.5) {
                    comparePos = i.worldPos.y;
                } else {
                    comparePos = i.clipSpaceY;
                }
                
                // 计算裁剪位置（考虑偏移）
                float cutoffPos = _Cutoff + _Offset;
                
                // 计算距离（考虑方向）
                float distanceToCutoff = (cutoffPos - comparePos) * _Direction;
                
                // 获取纹理颜色（对于TextMesh，纹理是字体贴图，只有alpha通道有数据）
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // 对于TextMesh，直接使用顶点颜色（已经在顶点着色器中乘了_Color）
                fixed4 col = fixed4(i.color.rgb, texColor.a * i.color.a);
                
                // 如果纹理完全透明，跳过
                if (texColor.a < 0.01) {
                    discard;
                }
                
                // 渐变区域处理
                if (distanceToCutoff < -_FadeRange) {
                    // 完全透明区域
                    discard;
                }
                else if (distanceToCutoff < 0) {
                    // 在裁剪线以下，但还在渐变范围内
                    float fade = saturate((distanceToCutoff + _FadeRange) / _FadeRange);
                    col.a *= fade;
                } 
                else if (distanceToCutoff < _FadeRange) {
                    // 在裁剪线以上，渐变范围内
                    float fade = saturate(distanceToCutoff / _FadeRange);
                    col.a = lerp(0, col.a, fade);
                }
                // 距离大于_FadeRange的部分完全显示
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "GUI/Text Shader"
}