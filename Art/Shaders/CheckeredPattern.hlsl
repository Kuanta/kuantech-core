
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

void DottedPattern_float(float3 vertexPos, float distance, float radius, float4 dotColor, float4 backgroundColor, out float4 color)
{
    // Calculate the distance from the vertex position to the nearest dot center
    float3 adjustedPos = vertexPos;
    if(adjustedPos.x < 0) adjustedPos.x *= -1;
    if(adjustedPos.y < 0) adjustedPos.y *= -1;
    if(adjustedPos.z < 0) adjustedPos.z *= -1;
    
    float2 nearestDotCenter = floor(adjustedPos / distance) * distance + 0.5 * distance;
    
    // Calculate the distance from the vertex to the center of the nearest dot
    float2 delta = adjustedPos - nearestDotCenter;
    float distToDotCenter = length(delta);
    
    // If the distance to the dot center is less than the radius, color the dot, otherwise color the background
    if (distToDotCenter < radius)
    {
        color = dotColor;
    }
    else
    {
        color = backgroundColor;
    }
}

void TiledPattern_float(float2 vertexPosition, float cellSize, float tileSizeFraction, float tileCornerRadius,int rowCount, int colCount, float4 backgroundColor, float4 tileColor, out float4 color)
{
    if(vertexPosition.x < 0) vertexPosition.x *= -1;
    if(vertexPosition.y < 0) vertexPosition.y *= -1;
    
    // Calculate the tile size from the cell size and tile size fraction
    float tileSize = cellSize * tileSizeFraction;

    // Calculate the half size of the tile for easier calculations
    float halfTileSize = tileSize * 0.5;
    
    // Calculate the position within the current cell
    float2 cellPosition = fmod(vertexPosition, cellSize);
    
    // Calculate the center of the cell
    float2 cellCenter = cellSize * 0.5;
    
    // Calculate the position relative to the center of the cell
    float2 relativePosition = cellPosition - cellCenter;
    
    // Calculate the distance from the center of the tile to the edges
    float2 halfTileExtents = float2(halfTileSize, halfTileSize);
    
    // Check if the relative position is within the tile bounds, considering the rounded corners
    float2 cornerDistance = abs(relativePosition) - halfTileExtents + tileCornerRadius;
    float2 clampedCornerDistance = max(cornerDistance, 0.0);
    float cornerDistanceSquared = dot(clampedCornerDistance, clampedCornerDistance);
    
    // Determine the color based on whether the point is inside the tile bounds
    bool isInsideTile = (relativePosition.x <= halfTileExtents.x && relativePosition.y <= halfTileExtents.y) &&
                        (cornerDistanceSquared <= (tileCornerRadius * tileCornerRadius));

    if(rowCount > 0 && abs(vertexPosition.y) >= rowCount*cellSize|| colCount > 0 && (abs(vertexPosition.x) >= colCount*cellSize))
    {
        color = backgroundColor;
        return;
    }
    if(isInsideTile)
    {
        color = tileColor;
    }else
    {
        color = backgroundColor;
    }
}
