Shader "Kuantech/HoleShader"
{
    Properties
    {
        [IntRange] _StencilID("Stencil ID", Range(0,255)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" "RenderPipeline"="UniversalPipeline"}
        Lighting Off
        Pass
        {
            Blend Zero One
            ZWrite Off
           Stencil
            {
                Ref [_StencilID]
                Comp Always
                Pass Replace
            }
        }
    }
}
