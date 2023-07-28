
void CheckeredPattern_float(float3 position, float squareSize, float4 colorA, float4 colorB, out float4 Out)
{
    const int isWhite = (int(floor(position.x / squareSize)) + int(floor(position.z / squareSize))) % 2;
    Out = isWhite == 0 ? colorA : colorB;
}

void StripePattern_float(float3 position, float stripteSize, float4 colorA, float4 colorB, out float4 Out)
{
    const int isWhite = int(floor(position.z / stripteSize)) % 2;
    Out = isWhite == 0 ? colorA : colorB;
}

void GridPattern_float(float3 position, float3 offset, float gridSize, float gridLineWidth, int rowCount, int colCount,float4 ColorBase, float4 ColorGrid, out float4 color)
{
    float3 offsetedPosition = position + offset;
    // Calculate the normalized coordinates within the grid
    float2 normalizedCoords = abs(fmod(offsetedPosition.xz, gridSize)) / gridSize;

    // Calculate the distance to the nearest grid line
    float2 distanceToGridLine = abs(normalizedCoords - 0.5) * gridSize;

    // Determine if the vertex is on a grid line based on the grid line width
    bool isOnGridLine = distanceToGridLine.x < gridLineWidth || distanceToGridLine.y < gridLineWidth;

    // Calculate the interpolation factor based on the distance to the grid line
    float interpolationFactor = smoothstep(0.0, gridLineWidth, min(distanceToGridLine.x, distanceToGridLine.y));

    // Set the vertex color based on the interpolation factor
    color = isOnGridLine ? ColorGrid : ColorBase;
    
    if(rowCount > 0 && abs(position.z) >= rowCount*gridSize*0.5 || colCount > 0 && (abs(position.x) >= colCount*gridSize*0.5))
    {
        color = ColorGrid;
    }
}
