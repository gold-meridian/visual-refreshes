namespace Refreshes.Common.Decals;

/// <summary>
///     Structure representing in-world decal data for rendering
/// </summary>>
public struct DecalData(bool behindTiles, ushort type, Point position, float rotation, Vector2 scale, FramingData framing)
{
    public bool BehindTiles = behindTiles;
    public ushort Type = type;
    public Point Position = position;
    public float Rotation = rotation;
    public Vector2 Scale = scale;
    public FramingData Framing = framing;
    /* TODO later when the basics are approved
    public void UpdateFraming() { 
        
    }
    public DecalData Create()
    {

    }
    public void Destroy()
    {

    }*/
}
public record struct FramingData(int FramesX, int FramesY, int TimePerFrame, Point CycleDirection)
{
    public int FrameTimer;
    public int FrameX;
    public int FrameY;
}
