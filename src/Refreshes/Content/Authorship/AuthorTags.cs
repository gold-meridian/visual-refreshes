using Daybreak.Common.Features.Authorship;
using JetBrains.Annotations;

namespace Refreshes.Content;

internal abstract class CommonAuthorTag : AuthorTag
{
    private const string suffix = "Tag";

    public override string Name => base.Name.EndsWith(suffix) ? base.Name[..^suffix.Length] : base.Name;

    public override string Texture => string.Join('/', Assets.Images.UI.AuthorTags.Mathica.KEY.Split('/')[..^1]) + '/' + Name;
}

[UsedImplicitly]
internal class MathicaTag : CommonAuthorTag;

[UsedImplicitly]
internal class TobiasTag : CommonAuthorTag;

[UsedImplicitly]
internal class TomatTag : Daybreak.Content.Authorship.TomatTag;

[UsedImplicitly]
internal class BlockarozTag : Daybreak.Content.Authorship.BlockarozTag;

[UsedImplicitly]
internal class BabyBlueSheepTag : CommonAuthorTag;

[UsedImplicitly]
internal class RotonTag : CommonAuthorTag;