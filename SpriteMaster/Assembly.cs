global using SMConfig = SpriteMaster.Configuration.Config;
global using XNA = Microsoft.Xna.Framework;
global using XColor = Microsoft.Xna.Framework.Color;
global using XGraphics = Microsoft.Xna.Framework.Graphics;
global using XSpriteBatch = Microsoft.Xna.Framework.Graphics.SpriteBatch;
global using XTexture2D = Microsoft.Xna.Framework.Graphics.Texture2D;
global using XRectangle = Microsoft.Xna.Framework.Rectangle;
global using XVector2 = Microsoft.Xna.Framework.Vector2;
global using DefaultScaler = SpriteMaster.Resample.Scalers.xBRZ;
global using DrawingRectangle = System.Drawing.Rectangle;
global using DrawingSize = System.Drawing.Size;
global using half = System.Half;
using System;
using System.Runtime.CompilerServices;
using System.Security;
// ReSharper disable StringLiteralTypo

// https://stackoverflow.com/questions/24802222/performance-of-expression-trees#comment44537873_24802222
[assembly: CLSCompliant(false)]
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: InternalsVisibleTo("Preview")]
[assembly: InternalsVisibleTo("Benchmarks.BenchmarkBase")]
[assembly: InternalsVisibleTo("Hashing")]
[assembly: InternalsVisibleTo("Arrays")]
[assembly: InternalsVisibleTo("Sprites")]
[assembly: InternalsVisibleTo("Strings")]
[assembly: InternalsVisibleTo("Math")]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
[assembly: ChangeList("9ec3d23:0.15.0-beta.16-22-g9ec3d23")]
[assembly: BuildComputerName("Palatinate")]
[assembly: FullVersion("0.15.0.116.0-beta.16.0")]

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