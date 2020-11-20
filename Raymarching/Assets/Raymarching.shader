Shader "PeerPlay/Raymarching"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "DistanceFunctions.cginc"

            sampler2D _MainTex;
            uniform sampler2D _CameraDepthTexture;
            uniform float4x4 _CamFrustum, _CamToWorld;
            uniform float _maxDistance;
            uniform float4 _sphere1;
            uniform float4 _box1;
            uniform float3 _modInterval;
            uniform float3 _lightDirection;
            uniform fixed4 _mainColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 ray : TEXCOORD1;
            };

            float distanceField(float3 position) {
                float ModX = pMod1(position.x, _modInterval.x);
                float ModY = pMod1(position.y, _modInterval.y);
                float ModZ = pMod1(position.z, _modInterval.z);

                float sphere1 = sdSphere(position - _sphere1.xyz, _sphere1.w);
                float box1 = sdBox(position - _box1.xyz, _box1.www);
                return opS(sphere1, box1);
            }

            float3 getNormal(float3 position) {
                const float2 offset = float2(0.001, 0.0);
                float3 normal = float3(
                    distanceField(position + offset.xyy) - distanceField(position - offset.xyy),
                    distanceField(position + offset.yxy) - distanceField(position - offset.yxy),
                    distanceField(position + offset.yyx) - distanceField(position - offset.yyx));
                return normalize(normal);
            }

            fixed4 raymarching(float3 rayOrigin, float3 rayDirection, float depth) {
                fixed4 result = fixed4(1, 1, 1, 1);

                const int maxIteration = 1000;
                float distanceTravelled = 0; // distance travelled along the ray direction

                // We loop through the iterations
                for (int i = 0; i < maxIteration; i++) {
                    if (distanceTravelled > _maxDistance || distanceTravelled > depth) {
                        // Envrionment color
                        result = fixed4(rayDirection, 0);
                        break;
                    }

                    float3 position = rayOrigin + rayDirection * distanceTravelled;
                    // Check for hit in distanceField
                    float distance = distanceField(position); // si < 0 dans objet, sinon extérieur
                    if (distance < 0.01) { // We have hit something
                        // Shading
                        float3 normal = getNormal(position);
                        float light = dot(-_lightDirection, normal);
                        result = fixed4(_mainColor.rgb * light, 1);
                        break;
                    }
                    distanceTravelled += distance;
                }

                return result;
            }

            v2f vert (appdata v)
            {
                v2f o;
                half index = v.vertex.z;
                v.vertex.z = 0;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                o.ray = _CamFrustum[(int)index].xyz;

                o.ray /= abs(o.ray.z);

                o.ray = mul(_CamToWorld, o.ray);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);
                depth *= length(i.ray);
                fixed3 col = tex2D(_MainTex, i.uv);
                float3 rayDirection = normalize(i.ray.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos;
                fixed4 result = raymarching(rayOrigin, rayDirection, depth);

                return fixed4(col* (1.0 - result.w) + result.xyz*result.w, 1.0);
            }
            ENDCG
        }
    }
}
