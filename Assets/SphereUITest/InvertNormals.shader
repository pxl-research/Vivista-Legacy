Shader "Flipping Normals" 
{
	Properties
	{
		_MainTex ("Base (RGBA)", 2D) = "white" {}
	}
	   
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

		Cull Off
		ZWrite Off

		CGPROGRAM

		#pragma surface surf NoLighting novertexlights noforwardadd alpha:fade

		sampler2D _MainTex;
		float offset;
		static const float2 invAtan = float2(0.1591, 0.3183);
			
		struct Input {
			float3 worldNormal;
		};
		
		float2 SampleSphericalMap(float3 dir)
		{
			offset = 0.25;
			float2 uv = float2(atan2(dir.x, dir.z), asin(dir.y));
			uv *= invAtan;
			uv += float2(1 + offset, 0.5);
			uv.x = fmod(uv.x, 1);
			return uv;
		}

		void surf (Input i, inout SurfaceOutput o) 
		{
			float2 uv = SampleSphericalMap(normalize(i.worldNormal));
			
			fixed4 result = tex2D(_MainTex, uv);
		
			o.Albedo = result.rgb;
			o.Alpha = result.a;
		}

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) 
		{
			fixed4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}

		ENDCG
	}
}