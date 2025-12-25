using ReLogic.Content;
using Terraria.ID;

namespace Refreshes.Common;

/// <summary>
///     The basis for rendering a gem tree tile (gem tree, gem tree sapling).
///     <br />
///     <br />
///     Implementations are responsible for providing textures to use in
///     rendering as well as determining information about what kind of tile is
///     being rendered and how/where.
/// </summary>
public abstract class GemTreeRenderer
{
    /// <summary>
    ///     Creates the renderer context for this renderer.  This is primarily
    ///     responsible for determining the biome the tree is rendered as part
    ///     of.
    /// </summary>
    public abstract RendererContext GetContext(
        int tileX,
        int tileY
    );

    /// <summary>
    ///     Determines what the tile ID is of the tree tile being rendered by
    ///     this renderer.
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public abstract int GetTreeTileType(
        Tile tile
    );

    /// <summary>
    ///     Provides the asset used to render the basic tile, not including its
    ///     gems.
    /// </summary>
    public abstract Asset<Texture2D> GetTileDrawTexture(
        RendererContext ctx,
        GemTreeVanityProfile profile
    );

    /// <summary>
    ///     Provides the assets used to render the gems over the unlit tile.
    /// </summary>
    public abstract void GetGlowData(
        RendererContext ctx,
        GemTreeVanityProfile profile,
        Tile tile,
        int tileWidth,
        int tileHeight,
        out Texture2D glowTexture,
        out Rectangle glowSourceRect
    );
}

/// <summary>
///     The default implementation of <see cref="GemTreeRenderer" /> for gem
///     trees.
/// </summary>
internal sealed class TreeRenderer : GemTreeRenderer
{
    public override RendererContext GetContext(
        int tileX,
        int tileY
    )
    {
        WorldGen.GetTreeBottom(
            tileX,
            tileY,
            out var treeBottomX,
            out var treeBottomY
        );

        var tileHoldingTree = Framing.GetTileSafely(treeBottomX, treeBottomY);
        var currentBiome = GemTreeRendering.GetBiomeFromTile(tileHoldingTree);
        return new RendererContext(this, currentBiome);
    }

    public override int GetTreeTileType(
        Tile tile
    )
    {
        return tile.TileType;
    }

    public override Asset<Texture2D> GetTileDrawTexture(
        RendererContext ctx,
        GemTreeVanityProfile profile
    )
    {
        return profile.GetDescription(ctx.CurrentBiome).Trunk;
    }

    public override void GetGlowData(
        RendererContext ctx,
        GemTreeVanityProfile profile,
        Tile tile,
        int tileWidth,
        int tileHeight,
        out Texture2D glowTexture,
        out Rectangle glowSourceRect
    )
    {
        glowTexture = (profile.GetDescription(ctx.CurrentBiome).TrunkGems ?? profile.Purity.TrunkGems!).Value;
        glowSourceRect = new Rectangle(tile.frameX, tile.TileFrameY, tileWidth, tileHeight);
    }
}

/// <summary>
///     The default implementation of <see cref="GemTreeRenderer" /> for gem
///     tree saplings.
/// </summary>
internal sealed class SaplingRenderer : GemTreeRenderer
{
    public override RendererContext GetContext(
        int tileX,
        int tileY
    )
    {
        var tile = Framing.GetTileSafely(tileX, tileY++);
        while (tile.TileType == TileID.GemSaplings)
        {
            tile = Framing.GetTileSafely(tileX, tileY++);
        }

        var currentBiome = GemTreeRendering.GetBiomeFromTile(tile);
        return new RendererContext(this, currentBiome);
    }

    public override int GetTreeTileType(
        Tile tile
    )
    {
        return GetCorrespondingTileType(tile.TileFrameX);
    }

    public override Asset<Texture2D> GetTileDrawTexture(
        RendererContext ctx,
        GemTreeVanityProfile profile
    )
    {
        return profile.GetSaplingDescription(ctx.CurrentBiome).Sapling;
    }

    public override void GetGlowData(RendererContext ctx, GemTreeVanityProfile profile, Tile tile, int tileWidth, int tileHeight, out Texture2D glowTexture, out Rectangle glowSourceRect)
    {
        glowTexture = (profile.GetSaplingDescription(ctx.CurrentBiome).SaplingGems ?? profile.PuritySaplings.SaplingGems!).Value;
        glowSourceRect = new Rectangle(tile.frameX, tile.TileFrameY, tileWidth, tileHeight);
    }

    private static int GetCorrespondingTileType(int frameX)
    {
        return (frameX / 54) switch
        {
            0 => 583,
            1 => 584,
            2 => 585,
            3 => 586,
            4 => 587,
            5 => 588,
            6 => 589,
            _ => 583,
        };
    }
}
