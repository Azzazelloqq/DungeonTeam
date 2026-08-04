#ifndef DUNGEON_TEAM_WALL_OCCLUSION_CLIP_INCLUDED
#define DUNGEON_TEAM_WALL_OCCLUSION_CLIP_INCLUDED

#define WALL_OCCLUSION_MAX_TARGETS 16

float4 _WallOcclusionTargets[WALL_OCCLUSION_MAX_TARGETS];
int _WallOcclusionTargetCount;
float _WallOcclusionRadius;
float _WallOcclusionFeather;
float _WallOcclusionDepthBias;

float WallOcclusionDither(float2 pixelPosition)
{
    return frac(52.9829189 * frac(dot(pixelPosition, float2(0.06711056, 0.00583715))));
}

void ApplyWallOcclusionClip(float4 positionCS)
{
    if (_WallOcclusionTargetCount <= 0)
    {
        return;
    }

    float2 screenUV = GetNormalizedScreenSpaceUV(positionCS);
    float fragmentDepth = LinearEyeDepth(positionCS.z, _ZBufferParams);
    float aspect = _ScaledScreenParams.x / max(_ScaledScreenParams.y, 1.0);
    float visibility = 1.0;

    UNITY_LOOP
    for (int index = 0; index < _WallOcclusionTargetCount; index++)
    {
        float4 target = _WallOcclusionTargets[index];
        if (fragmentDepth >= target.z - _WallOcclusionDepthBias)
        {
            continue;
        }

        float2 offset = screenUV - target.xy;
        offset.x *= aspect;
        float distanceToTarget = length(offset);
        float targetVisibility = _WallOcclusionFeather > 0.0
            ? smoothstep(
                _WallOcclusionRadius - _WallOcclusionFeather,
                _WallOcclusionRadius,
                distanceToTarget)
            : step(_WallOcclusionRadius, distanceToTarget);
        visibility = min(visibility, targetVisibility);
    }

    if (visibility < 1.0)
    {
        clip(visibility - WallOcclusionDither(positionCS.xy) - 0.0001);
    }
}

#endif
