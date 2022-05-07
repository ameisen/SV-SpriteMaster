global using XNA = Microsoft.Xna.Framework;
global using XColor = Microsoft.Xna.Framework.Color;
global using XGraphics = Microsoft.Xna.Framework.Graphics;
global using XSpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;
global using XTexture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
global using XRectangle = Microsoft.Xna.Framework.Rectangle;
global using XVector2 = Microsoft.Xna.Framework.Vector2;
global using DefaultScaler = SpriteMaster.Resample.Scalers.xBRZ;
global using DrawingColor = System.Drawing.Color;
global using DrawingPoint = System.Drawing.Point;
global using DrawingRectangle = System.Drawing.Rectangle;
global using DrawingSize = System.Drawing.Size;
global using half = System.Half;
global using XTilePoint = xTile.Dimensions.Location;
global using XTileRectangle = xTile.Dimensions.Rectangle;
global using XTileSize = xTile.Dimensions.Size;
using System;
using System.Runtime.CompilerServices;
using System.Security;
// ReSharper disable StringLiteralTypo

// https://stackoverflow.com/questions/24802222/performance-of-expression-trees#comment44537873_24802222
[assembly: CLSCompliant(false)]
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: InternalsVisibleTo("xBRZ")]
[assembly: InternalsVisibleTo("Hashing")]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
[assembly: ChangeList("cacc4e2:0.14.0-1-gcacc4e2")]
[assembly: BuildComputerName("Palatinate")]
[assembly: FullVersion("0.14.0.300")]
// [assembly: SuppressUnmanagedCodeSecurity]

[module: CLSCompliant(false)]
[module: SkipLocalsInit]

[AttributeUsage(validOn: AttributeTargets.Assembly)]
internal sealed class ChangeListAttribute : Attribute {
	internal readonly string Value;
	internal ChangeListAttribute(string value) => Value = value;
}

[AttributeUsage(validOn: AttributeTargets.Assembly)]
internal sealed class BuildComputerNameAttribute : Attribute {
	internal readonly string Value;
	internal BuildComputerNameAttribute(string value) => Value = value;
}

[AttributeUsage(validOn: AttributeTargets.Assembly)]
internal sealed class FullVersionAttribute : Attribute {
	internal readonly string Value;
	internal FullVersionAttribute(string value) => Value = value;
}