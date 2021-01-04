using UnityEngine;

namespace AsImpL
{
    /// <summary>
    /// Utility class for model import
    /// </summary>
    public class ModelUtil
    {
        /// <summary>
        /// Blend mode for Unity Material
        /// </summary>
        public enum MtlBlendMode { OPAQUE, CUTOUT, FADE, TRANSPARENT }


        /// <summary>
        /// Set up a Material for the given mode.
        /// </summary>
        /// <remarks>Here is replicated what is done when choosing a blend mode from Inspector.</remarks>
        /// <param name="mtl">material to be changed</param>
        /// <param name="mode">mode to be set</param>
        public static void SetupMaterialWithBlendMode(Material mtl, MtlBlendMode mode)
        {
            switch (mode)
            {
                case MtlBlendMode.OPAQUE:
                    mtl.SetOverrideTag("RenderType", "Opaque");
                    mtl.SetFloat("_Mode", 0);
                    mtl.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mtl.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mtl.SetInt("_ZWrite", 1);
                    mtl.DisableKeyword("_ALPHATEST_ON");
                    mtl.DisableKeyword("_ALPHABLEND_ON");
                    mtl.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mtl.renderQueue = -1;
                    break;
                case MtlBlendMode.CUTOUT:
                    mtl.SetOverrideTag("RenderType", "TransparentCutout");
                    mtl.SetFloat("_Mode", 1);
                    mtl.SetFloat("_Mode", 1);
                    mtl.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mtl.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mtl.SetInt("_ZWrite", 1);
                    mtl.EnableKeyword("_ALPHATEST_ON");
                    mtl.DisableKeyword("_ALPHABLEND_ON");
                    mtl.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mtl.renderQueue = 2450;
                    break;
                case MtlBlendMode.FADE:
                    mtl.SetOverrideTag("RenderType", "Transparent");
                    mtl.SetFloat("_Mode", 2);
                    mtl.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mtl.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mtl.SetInt("_ZWrite", 0);
                    mtl.DisableKeyword("_ALPHATEST_ON");
                    mtl.EnableKeyword("_ALPHABLEND_ON");
                    mtl.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mtl.renderQueue = 3000;
                    break;
                case MtlBlendMode.TRANSPARENT:
                    mtl.SetOverrideTag("RenderType", "Transparent");
                    mtl.SetFloat("_Mode", 3);
                    mtl.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mtl.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mtl.SetInt("_ZWrite", 0);
                    mtl.DisableKeyword("_ALPHATEST_ON");
                    mtl.DisableKeyword("_ALPHABLEND_ON");
                    mtl.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    mtl.renderQueue = 3000;
                    break;
            }
        }


        /// <summary>
        /// Scan a texture looking for transparent pixels and trying to guess the correct blend mode needed.
        /// </summary>
        /// <param name="texture">input rexture (it must be set to readable)</param>
        /// <param name="mode">blend mode set to FADE or CUTOUT if transparent pixels are found.</param>
        /// <returns>Return true if transparent pixels were found.</returns>
        public static bool ScanTransparentPixels(Texture2D texture, ref MtlBlendMode mode)
        {
            bool texCanBeTransparent = texture != null
                && (texture.format == TextureFormat.ARGB32
                || texture.format == TextureFormat.RGBA32
                || texture.format == TextureFormat.DXT5
                || texture.format == TextureFormat.ARGB4444
                || texture.format == TextureFormat.BGRA32
                // Only for DirectX support
#if UNITY_STANDALONE_WIN
                || texture.format == TextureFormat.DXT5Crunched
#endif
                //|| texture.format == ... (all alpha formats)
                );
            if (texCanBeTransparent)
            {
                bool stop = false;
                int pixelScanFrequency = 1;
                for (int x = 0; x < texture.width && !stop; x += pixelScanFrequency)
                {
                    for (int y = 0; y < texture.height && !stop; y += pixelScanFrequency)
                    {
                        float a = texture.GetPixel(x, y).a;
                        DetectMtlBlendFadeOrCutout(a, ref mode, ref stop);
                        if (stop)
                        {
                            return mode == MtlBlendMode.FADE || mode == MtlBlendMode.CUTOUT;
                        }
                    }
                }
            }
            return mode == MtlBlendMode.FADE || mode == MtlBlendMode.CUTOUT;

        }


        /// <summary>
        /// Detect if the blend mode must be set to FADE or CUTOUT
        /// according to the given alpha value and the current value of mode.
        /// </summary>
        /// <param name="alpha">input alpha value</param>
        /// <param name="mode">blend mode set to FADE or CUTOUT</param>
        /// <param name="noDoubt">flag set to true if the mode is finally detected (it can be used to break from a scan loop)</param>
        public static void DetectMtlBlendFadeOrCutout(float alpha, ref MtlBlendMode mode, ref bool noDoubt)
        {
            if (noDoubt)
            {
                return;
            }
            if (alpha < 1.0f)
            {
                //mode = MtlBlendMode.TRANSPARENT;
                if (alpha == 0.0f)
                {
                    // assume there is a "cutout texture"
                    mode = MtlBlendMode.CUTOUT;
                } // else 0<alpha<1
                else if (mode != MtlBlendMode.FADE)
                {
                    // assume there is a "fade texture"
                    mode = MtlBlendMode.FADE;
                    noDoubt = true;
                }
            }
        }


        /// <summary>
        /// Convert a bump map to a normal map
        /// </summary>
        /// <param name="bumpMap">input bump map</param>
        /// <param name="amount">optionally adjust the bump effect with the normal map</param>
        /// <returns>The new normal map</returns>
        public static Texture2D HeightToNormalMap(Texture2D bumpMap, float amount = 1f)
		{
			int h = bumpMap.height;
			int w = bumpMap.width;

            Texture2D normalMap = new Texture2D(w, h, TextureFormat.ARGB32, true);

			return HeightToNormal(amount, h, w, bumpMap, normalMap); ;
		}

		private static Texture2D HeightToNormal(float amount, int h, int w, Texture2D bumpMap, Texture2D normalMap)
		{
            float changeNeg, changePos;
            float /*h0,*/ h1, h2, h3/*, h4*/;
            Color col = Color.black;
            var data = bumpMap.GetPixels();
            var data2 = new Color[data.Length];
            var grayscale = new float[data.Length];

			for (int i = 0; i < h; i++)
            {
				for (int j = 0; j < w; j++)
				{
					grayscale[i * h + j] = data[i * h + j].grayscale;
				}
			}

            for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					Vector3 n = new Vector3();
					// CHANGE IN X
					h1 = grayscale[y * w + (x + w - 1) % w];
					h2 = grayscale[y * w + x];
					h3 = grayscale[y * w + (x + w + 1) % w];

					changeNeg = h2 - h1;
					changePos = h3 - h2;

					n.x = -(changePos + changeNeg);

					// CHANGE IN Y

					h1 = grayscale[(y + h - 1) % h * w + y];
					h2 = grayscale[y * w + y];
					h3 = grayscale[(y + h + 1) % h * w + y];

					changeNeg = h2 - h1;
					changePos = h3 - h2;

					n.y = -(changePos + changeNeg);

					// SCALE OF BUMPINESS

					if (amount != 1.0f) n *= amount;

					/// Get depth component
					n.z = Mathf.Sqrt(1.0f - (n.x * n.x + n.y * n.y));

					// Scale in (0..0.5);
					n *= 0.5f;

					// set the pixel

                    //TODO(Simon): Do we need clamping?
					//col.r = Mathf.Clamp01(n.x + 0.5f);
					//col.g = Mathf.Clamp01(n.y + 0.5f);
					//col.b = Mathf.Clamp01(n.z + 0.5f);

                    col.r = (n.x + 0.5f);
                    col.g = (n.y + 0.5f);
                    col.b = (n.z + 0.5f);

					col.a = col.r;
					data2[y * w + x] = col;
				}
			}

            normalMap.SetPixels(data2);
            normalMap.Apply();
            return normalMap;
		}


		/// <summary>
		/// Wrap the given value pos inside the range (0..boundary).
		/// </summary>
		/// <param name="pos">input value</param>
		/// <param name="boundary">range boundary</param>
		/// <returns>the wrapped value</returns>
		private static int WrapInt(int pos, int boundary)
        {
            if (pos < 0)
                pos = boundary + pos;
            else if (pos >= boundary)
                pos -= boundary;

            return pos;
        }
    }
}
