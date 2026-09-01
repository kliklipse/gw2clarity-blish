// GridEffect.fx
//
// Shader optionnel pour le rendu des items de grille GW2Clarity : applique un tint
// multiplicatif sur l'icone puis compose une bordure et un glow en overlay simple.
// La pulsation du glow (amplitude/frequence) est deja resolue cote CPU par
// Style.Resolve (voir Models/Style.cs) - ce shader n'a donc PAS a recalculer de
// pulsation, seulement a appliquer les couleurs/tailles déjà figées pour la frame.
//
// Ce fichier est la SOURCE du shader. Il doit etre precompile en .mgfx avant de
// pouvoir etre charge par ContentsManager.GetEffect(...) :
//
//   mgfxc GridEffect.fx GridEffect.mgfx /Profile:DirectX_11
//
// (mgfxc est fourni par le package MonoGame.Content.Builder.Task, cf.
// https://blishhud.com/docs/modules/guides/shaders/). Tant que le .mgfx n'existe
// pas, GridRendererControl fonctionne sans lui : bordure et glow sont alors dessines
// par composition de sprites (rectangles) au lieu de ce shader. Le wiring optionnel
// se fait via GridRendererControl.BorderGlowEffect une fois le .mgfx disponible.

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

// Parametres pilotes depuis GridInstanceData (une valeur par instance dessinee ;
// avec SpriteBatch immediate mode, ils sont mis a jour avant chaque Draw()).
float4 Tint;
float4 BorderColor;
float BorderThickness;
float4 GlowColor;
float2 GlowSize;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float4 texColor = tex2D(SpriteTextureSampler, input.TextureCoordinates);
    float4 color = texColor * input.Color * Tint;

    // Bordure : bande fine pres du bord du quad (espace UV [0,1]).
    float2 edgeDist = min(input.TextureCoordinates, 1.0 - input.TextureCoordinates);
    float borderMask = saturate(step(edgeDist.x, BorderThickness) + step(edgeDist.y, BorderThickness));
    color.rgb = lerp(color.rgb, BorderColor.rgb, borderMask * BorderColor.a);

    // Glow : halo additif simple, plus fort pres du bord, taille pilotee par GlowSize.
    float2 glowDenom = max(GlowSize, float2(0.0001, 0.0001));
    float glowMask = saturate(1.0 - min(edgeDist.x / glowDenom.x, edgeDist.y / glowDenom.y));
    color.rgb += GlowColor.rgb * GlowColor.a * glowMask;

    return color;
}

technique SpriteBatch
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
