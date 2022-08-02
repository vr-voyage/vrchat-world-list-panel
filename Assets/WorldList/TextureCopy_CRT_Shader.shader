Shader "Voyage/CustomRenderTexture/TextureCopy"
{
    Properties
    {
        _Tex("InputTex", 2D) = "black" {}
    }

    SubShader
    {
        Lighting Off

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            Texture2D   _Tex;
            SamplerState sampler_Tex_point_clamp;

            fixed4 frag(v2f_customrendertexture i) : COLOR
            {
                fixed4 col = _Tex.Sample(sampler_Tex_point_clamp, i.localTexcoord.xy);
                col.r = LinearToGammaSpaceExact(col.r);
                col.g = LinearToGammaSpaceExact(col.g);
                col.b = LinearToGammaSpaceExact(col.b);
                return col;
            }
            ENDCG
        }
    }
}
