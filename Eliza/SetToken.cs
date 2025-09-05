using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genova.Eliza;

/// <summary>Set token: {"set": ["MOTHER","FATHER", ...]}</summary>
internal sealed record SetToken(List<string> Items) : PatternToken;
