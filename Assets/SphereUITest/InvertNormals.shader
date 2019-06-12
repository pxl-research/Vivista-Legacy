Shader "Flipping Normals" 
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	   
	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

		Cull Off
		ZWrite Off

		CGPROGRAM

		#pragma surface surf Lambert vertex:vert alpha:fade
		sampler2D _MainTex;
			
		struct Input {
			float4 pos : POSITION;
			float2 uv_MainTex;
			float4 color : COLOR;
		};
			
		void vert(inout appdata_full v) {
			//v.normal.xyz = v.normal * -1;
		}
			
		void surf (Input i, inout SurfaceOutput o) 
		{
			//float3 dir = normalize(i.pos);
			//float2 longlat = float2(atan2(dir.x, dir.z) + UNITY_PI, acos(-dir.y));
			//float2 uv = longlat / float2(2.0 * UNITY_PI, UNITY_PI);
			
			float2 flippedUv = float2(1 - i.uv_MainTex.x, i.uv_MainTex.y);

			fixed4 result = tex2D(_MainTex, flippedUv);
			o.Albedo = result.rgb;
			o.Alpha = result.a;
		}

		ENDCG
	}
	
	Fallback "Diffuse"
}