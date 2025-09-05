using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genova.Eliza;

/// <summary>Tag(s) token: {"tag":"BELIEF"} or {"tags":[...]}</summary>
internal sealed record TagToken(List<string> Tags) : PatternToken
{
    public TagToken(string singleTag) : this(new List<string> { singleTag }) { }
}
