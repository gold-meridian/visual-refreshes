using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refreshes.Common.Decals;

public interface IDecalRenderer
{
    public ushort Type { get; }

    public void Draw(DecalData data, SpriteBatch spriteBatch);

}