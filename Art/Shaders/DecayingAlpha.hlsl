void FadeTowardsEdges_float(float3 position, float3 pivotPoint, float distanceThreshold, float alphaDecay, out float Out)
{
    float3 distance = abs(position - pivotPoint);
    float singleDistance = max(distance.x, distance.y);
    if(singleDistance <= distanceThreshold)
    {
        Out = 0.0f;
        return;
    }
    Out  = max(1 -singleDistance*alphaDecay, 0);
}

void FadeTowardsEdges2D_float(float2 position, float2 rectSize, out float Out)
{
    position.x = clamp(position.x, -rectSize.x, rectSize.x);
    position.y = clamp(position.y, -rectSize.y, rectSize.y);
    const float horizontal = (1-abs(position.x/rectSize.x));
    const float vertical = (1- abs(position.y/rectSize.y));
    Out = horizontal * vertical;
}

void FadeTowardsEdgesCircular2D_float(float2 position, float radius, out float Out)
{
    //Implement circular here
    const float2 center = 0;
    const float distance = length(position - center);
    const float normalizedDistance = distance / (radius); // Assuming width and height are the same for a circular shape
    Out = max(1 - normalizedDistance, 0);
}

void FadeTowardsEdges1D_float(float position, float rectSize, out float Out)
{
    Out = (1- pow((position/rectSize),4));

}