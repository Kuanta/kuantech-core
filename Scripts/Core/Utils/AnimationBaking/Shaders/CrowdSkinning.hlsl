#ifndef KUANTECH_CROWD_SKINNING_INCLUDED
#define KUANTECH_CROWD_SKINNING_INCLUDED

// -----------------------------------------------------------------------------------------------
// GPU skinning for baked crowd agents.
//
// This replaces what a SkinnedMeshRenderer does on the CPU. A skinned vertex is
//
//     p_final = sum_i( weight_i * (Bone_i * Bindpose_i) * p_bind )
//
// and the only part of that which changes per frame is the (Bone * Bindpose) product — the skin
// matrix. CrowdAnimationBaker evaluates those matrices once in the editor and stores them in a
// texture, so all that is left here is the weighted sum, which is what a vertex shader is for.
//
// Texture layout (see CrowdAnimationSet for the authoring side):
//     x = bone index * 3 + row   (a 3x4 affine matrix; the fourth row is always 0,0,0,1)
//     y = global frame index     (every clip concatenated)
//
// Read with Load, never with a sampler: we want exact texels. Smoothing between frames is explicit —
// two rows are read and blended by Weight, which the CPU also reuses for locomotion blends and
// cross-fades. See CrowdAnimator for what the pair means in each case.
//
// -----------------------------------------------------------------------------------------------
// Wiring it into a Shader Graph
// -----------------------------------------------------------------------------------------------
// The whole system only touches the vertex stage, so nothing about a graph's lighting or surface work
// is affected. In any graph that should render crowd agents:
//
//   1. Add two blackboard properties and set their Reference names exactly:
//        Texture2D  ->  _CrowdAnimationTexture
//        Float      ->  _CrowdBoneCount
//      CrowdRenderer writes both through a MaterialPropertyBlock, so their default values do not matter.
//   2. Add a Custom Function node, Type = File, Source = this file.
//        Name = CrowdSkin4   (mesh baked with 4 bone influences)
//             or CrowdSkin2  (mesh baked with 2 — half the texture reads, use it on mobile)
//      Write the name WITHOUT the _float suffix; Shader Graph appends it from the node's precision.
//      For the same reason the node (or the graph) must be set to Single precision — only the _float
//      variants exist here.
//   3. Give it these inputs, in this order — the order is the function signature:
//        AnimationTexture (Texture2D), BoneCount (Float), PositionOS (Vector3),
//        NormalOS (Vector3), TangentOS (Vector3), BoneIndices (Vector4), BoneWeights (Vector4)
//      and these outputs:
//        OutPositionOS (Vector3), OutNormalOS (Vector3), OutTangentOS (Vector3)
//   4. Feed it: the two properties, Position/Normal Vector/Tangent Vector all set to Object space,
//      a UV node on channel UV2 -> BoneIndices, a UV node on channel UV3 -> BoneWeights.
//      Wire the outputs into the VertexDescription Position / Normal / Tangent blocks.
//   5. Turn on "Enable GPU Instancing" on the material made from this graph (Material inspector,
//      under Advanced Options). Without it every agent draws with the same instance data.
//
// Everything below the vertex blocks — base colour, fresnel, emission, hit flashes — is untouched and
// keeps working exactly as before.
// -----------------------------------------------------------------------------------------------

// Per-agent state, written by CrowdRenderer. Must stay byte-identical to AgentGpuData on the C# side.
struct CrowdAgentData
{
    float frame0;
    float frame1;
    float weight;
    float padding;
    float4 effect;
};

StructuredBuffer<CrowdAgentData> _CrowdAgentData;

// The buffer is indexed by the instance id of the current draw. CrowdRenderer gives every draw call
// its own buffer, so index zero is always the first agent of that call and no offset is needed.
//
// The guard has to test the macro's VALUE, not whether it is defined: UnityInstancing.hlsl always
// defines UNITY_ANY_INSTANCING_ENABLED, as 1 or as 0, so defined() is true either way. unity_InstanceID
// only exists in the 1 case, so a defined() guard would let non-instanced variants — the ShadowCaster
// and depth passes compile one — reach an identifier that is not there.
uint CrowdGetInstanceIndex()
{
#if UNITY_ANY_INSTANCING_ENABLED
    return unity_InstanceID;
#else
    return 0;
#endif
}

void CrowdGetAgentState(out int frame0, out int frame1, out float weight)
{
#if defined(SHADERGRAPH_PREVIEW)
    // The graph preview has no buffer bound; show the bind pose rather than reading garbage.
    frame0 = 0;
    frame1 = 0;
    weight = 0.0;
#else
    CrowdAgentData agent = _CrowdAgentData[CrowdGetInstanceIndex()];
    frame0 = (int)agent.frame0;
    frame1 = (int)agent.frame1;
    weight = agent.weight;
#endif
}

// -----------------------------------------------------------------------------------------------
// Per-agent effect data — a hit flash, a dissolve, anything that has to differ between two agents
// drawn by the same call. Cloning the material per agent is the usual way to do this and it is exactly
// what must not happen here: one shared material is what collapses the crowd into a single draw call.
//
// Wiring it into a Shader Graph:
//   1. Add a second Custom Function node, Type = File, Source = this file, Name = CrowdGetAgentEffect.
//      It takes no inputs and has one output, Effect (Vector4).
//   2. Put it wherever the value is needed, including the FRAGMENT stage — no custom interpolator is
//      required. URP's generated passes call UNITY_TRANSFER_INSTANCE_ID in BuildVaryings and
//      UNITY_SETUP_INSTANCE_ID at the top of frag, before BuildSurfaceDescription runs, so the instance
//      id is live there too. Reading in the fragment stage costs one buffer fetch per pixel instead of
//      per vertex; move it to the vertex stage behind a custom interpolator only if that ever shows up
//      in a profile.
//   3. Interpret the four floats however the shader likes; CrowdInstance.EffectData is the other end.
//
// CrowdFlashShaderEffect, the one effect shipped with this system, packs them as:
//      Effect.x    flash amount, 0 to 1
//      Effect.yzw  flash colour, HDR — the buffer is float, so values above 1 come through intact
// which the fragment stage consumes as  lerp(BaseColor, Effect.yzw, Effect.x).
// -----------------------------------------------------------------------------------------------
void CrowdGetAgentEffect_float(out float4 Effect)
{
#if defined(SHADERGRAPH_PREVIEW)
    Effect = 0.0;
#else
    Effect = _CrowdAgentData[CrowdGetInstanceIndex()].effect;
#endif
}

// One bone's skin matrix at one frame: three consecutive texels starting at bone * 3.
float3x4 CrowdLoadBoneMatrix(Texture2D animationTexture, int bone, int frame)
{
    int x = bone * 3;
    float4 row0 = animationTexture.Load(int3(x + 0, frame, 0));
    float4 row1 = animationTexture.Load(int3(x + 1, frame, 0));
    float4 row2 = animationTexture.Load(int3(x + 2, frame, 0));
    return float3x4(row0, row1, row2);
}

// The blended skin matrix for a single influence: the same bone at both frames, lerped by weight.
// Lerping matrices is not strictly correct — the mathematically right thing is to blend rotations as
// quaternions — but between neighbouring frames of a 30 fps bake the difference is not visible.
float3x4 CrowdBoneMatrixBlended(Texture2D animationTexture, int bone, int frame0, int frame1, float weight)
{
    float3x4 a = CrowdLoadBoneMatrix(animationTexture, bone, frame0);
    float3x4 b = CrowdLoadBoneMatrix(animationTexture, bone, frame1);
    return lerp(a, b, weight);
}

void CrowdApplySkin(float3x4 skin, float3 positionOS, float3 normalOS, float3 tangentOS,
                    out float3 outPositionOS, out float3 outNormalOS, out float3 outTangentOS)
{
    outPositionOS = mul(skin, float4(positionOS, 1.0));

    // Non-uniform scale would need the inverse transpose here, but skin matrices are rigid transforms
    // (rotation plus translation), so the upper 3x3 is enough. The bitangent sign in the mesh's tangent.w
    // is untouched by a rigid transform, which is why Shader Graph's Vector3 tangent is enough here.
    float3x3 rotation = (float3x3)skin;
    outNormalOS = normalize(mul(rotation, normalOS));
    outTangentOS = normalize(mul(rotation, tangentOS));
}

// -----------------------------------------------------------------------------------------------
// Shader Graph entry points. Two variants rather than a keyword: the influence count is decided at
// bake time and never changes at runtime, so picking the matching node is simpler than branching.
// -----------------------------------------------------------------------------------------------

void CrowdSkin4_float(UnityTexture2D AnimationTexture, float BoneCount,
                      float3 PositionOS, float3 NormalOS, float3 TangentOS,
                      float4 BoneIndices, float4 BoneWeights,
                      out float3 OutPositionOS, out float3 OutNormalOS, out float3 OutTangentOS)
{
#if defined(SHADERGRAPH_PREVIEW)
    OutPositionOS = PositionOS;
    OutNormalOS = NormalOS;
    OutTangentOS = TangentOS;
#else
    int frame0, frame1;
    float weight;
    CrowdGetAgentState(frame0, frame1, weight);

    int lastBone = max((int)BoneCount - 1, 0);
    float3x4 skin = (float3x4)0.0;

    [unroll]
    for (int i = 0; i < 4; i++)
    {
        int bone = clamp((int)BoneIndices[i], 0, lastBone);
        skin += BoneWeights[i] * CrowdBoneMatrixBlended(AnimationTexture.tex, bone, frame0, frame1, weight);
    }

    CrowdApplySkin(skin, PositionOS, NormalOS, TangentOS, OutPositionOS, OutNormalOS, OutTangentOS);
#endif
}

void CrowdSkin2_float(UnityTexture2D AnimationTexture, float BoneCount,
                      float3 PositionOS, float3 NormalOS, float3 TangentOS,
                      float4 BoneIndices, float4 BoneWeights,
                      out float3 OutPositionOS, out float3 OutNormalOS, out float3 OutTangentOS)
{
#if defined(SHADERGRAPH_PREVIEW)
    OutPositionOS = PositionOS;
    OutNormalOS = NormalOS;
    OutTangentOS = TangentOS;
#else
    int frame0, frame1;
    float weight;
    CrowdGetAgentState(frame0, frame1, weight);

    int lastBone = max((int)BoneCount - 1, 0);
    float3x4 skin = (float3x4)0.0;

    [unroll]
    for (int i = 0; i < 2; i++)
    {
        int bone = clamp((int)BoneIndices[i], 0, lastBone);
        skin += BoneWeights[i] * CrowdBoneMatrixBlended(AnimationTexture.tex, bone, frame0, frame1, weight);
    }

    CrowdApplySkin(skin, PositionOS, NormalOS, TangentOS, OutPositionOS, OutNormalOS, OutTangentOS);
#endif
}

#endif // KUANTECH_CROWD_SKINNING_INCLUDED
