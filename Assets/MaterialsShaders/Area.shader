Shader "Area" 
{
	Properties 
	{
		_Color ("Color", Color) = (1,1,1)
	}
	Category
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True"}
		ZWrite Off
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		SubShader 
		{
			Color [_Color]
			Pass {}
		}
	}
}
