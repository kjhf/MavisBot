using SplatTagCore;
using System.Collections.Generic;
using System.Linq;

namespace MavisTests
{
  internal class ReadonlySourceableContainer : List<string>, IReadonlySourceable
  {
    public ReadonlySourceableContainer()
      : base()
    {
    }

    public ReadonlySourceableContainer(IEnumerable<string> collection)
      : base(collection)
    {
    }

    public IReadOnlyList<Source> Sources => this.Select(name => new Source(name)).ToArray();
  }
}