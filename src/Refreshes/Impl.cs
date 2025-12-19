global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Terraria;
global using Terraria.ModLoader;
using Daybreak.Common.Features.Authorship;
using Daybreak.Common.Features.ModPanel;

namespace Refreshes;

partial class ModImpl : IHasCustomAuthorMessage  {
    public ModImpl() {
        MusicAutoloadingEnabled = false;
    }

    public string GetAuthorText() => AuthorText.GetAuthorTooltip(this, Mods.Refreshes.UI.ModIcon.AuthorHeader.GetTextValue());
}