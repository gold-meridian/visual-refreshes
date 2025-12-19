using Daybreak.Common.Features.Authorship;
using Daybreak.Common.Features.ModPanel;

namespace Refreshes;

partial class ModImpl : IHasCustomAuthorMessage
{
    public ModImpl()
    {
        MusicAutoloadingEnabled = false;
    }

    string IHasCustomAuthorMessage.GetAuthorText()
    {
        return AuthorText.GetAuthorTooltip(this, Mods.Refreshes.UI.ModIcon.AuthorHeader.GetTextValue());
    }
}
