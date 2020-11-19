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

            sampler2D _MainTex;
            uniform float4x4 _CamFrustum, _CamToWorld;
            uniform float _maxDistance;
            uniform float4 _sphere1;

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

            float sdfSphere(float3 position, float radius) {
                return length(position) - radius;
            }

            float distanceField(float3 position) {
                float sphere1 = sdfSphere(position - _sphere1.xyz, _sphere1.w);
                return sphere1;
            }

            fixed4 raymarching(float3 rayOrigin, float3 rayDirection) {
                fixed4 result = fixed4(1, 1, 1, 1);

                const int maxIteration = 64;
                float distanceTravelled = 0; // distance travelled along the ray direction

                // We loop through the iterations
                for (int i = 0; i < maxIteration; i++) {
                    if (distanceTravelled > _maxDistance) {
                        // Envrionment color
                        result = fixed4(rayDirection, 1);
                        break;
                    }

                    float3 position = rayOrigin + rayDirection * distanceTravelled;
                    // Check for hit in distanceField
                    float distance = distanceField(position); // si < 0 dans objet, sinon extérieur
                    if (distance < 0.01) { // We have hit something
                        // Shading
                        result = fixed4(1, 1, 1, 1);
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
                float3 rayDirection = normalize(i.ray.xyz);
                float3 rayOrigin = _WorldSpaceCameraPos;
                fixed4 result = raymarching(rayOrigin, rayDirection);
                return result;
            }
            ENDCG
        }
    }
}
