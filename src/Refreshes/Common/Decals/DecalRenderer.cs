namespace Refreshes.Common.Decals;

public abstract class DecalRenderer : ModType
{
    protected sealed override void Register()
    {
        ModTypeLookup<DecalRenderer>.Register(this);
    }
    public ushort Type { get; internal set; }

    public abstract void Draw(DecalData data, Tile tile, SpriteBatch spriteBatch);
}