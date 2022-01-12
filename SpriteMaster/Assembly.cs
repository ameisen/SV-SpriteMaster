global using XNA = Microsoft.Xna.Framework;
global using DrawingColor = System.Drawing.Color;
global using DrawingPoint = System.Drawing.Point;
global using DrawingRectangle = System.Drawing.Rectangle;
global using DrawingSize = System.Drawing.Size;
global using XTilePoint = xTile.Dimensions.Location;
global using XTileRectangle = xTile.Dimensions.Rectangle;
global using XTileSize = xTile.Dimensions.Size;
global using half = System.Half;

using System.Runtime.CompilerServices;
using System.Security;

[assembly: CompilationRelaxations(CompilationRelaxations.NoStringInterning)]

// https://stackoverflow.com/questions/24802222/performance-of-expression-trees#comment44537873_24802222
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
// [assembly: SuppressUnmanagedCodeSecurity]

[module: SkipLocalsInit]
