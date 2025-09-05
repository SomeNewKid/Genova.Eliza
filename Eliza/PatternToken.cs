using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Genova.Eliza;

/// <summary>Finite set of known token types in patterns.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(StringToken), typeDiscriminator: "s")]
[JsonDerivedType(typeof(SetToken), typeDiscriminator: "set")]
[JsonDerivedType(typeof(TagToken), typeDiscriminator: "tag")]
internal abstract record PatternToken;
