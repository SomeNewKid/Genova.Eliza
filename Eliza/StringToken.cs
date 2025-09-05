using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genova.Eliza;

/// <summary>Plain string token (e.g., "0", "YOUR", "3").</summary>
internal sealed record StringToken(string Text) : PatternToken;
