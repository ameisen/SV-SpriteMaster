global using XNA = Microsoft.Xna.Framework;
global using DrawingColor = System.Drawing.Color;
global using DrawingPoint = System.Drawing.Point;
global using DrawingRectangle = System.Drawing.Rectangle;
global using DrawingSize = System.Drawing.Size;
global using XTilePoint = xTile.Dimensions.Location;
global using XTileRectangle = xTile.Dimensions.Rectangle;
global using XTileSize = xTile.Dimensions.Size;
global using half = System.Half;
global using XTexture2D = Microsoft.Xna.Framework.Graphics.Texture2D;

#nullable enable

using System.Runtime.CompilerServices;
using System.Security;
using System;

[assembly: CompilationRelaxations(CompilationRelaxations.NoStringInterning)]

// https://stackoverflow.com/questions/24802222/performance-of-expression-trees#comment44537873_24802222
[assembly: CLSCompliant(false)]
[assembly: AllowPartiallyTrustedCallers]
[assembly: SecurityTransparent]
[assembly: SecurityRules(SecurityRuleSet.Level2, SkipVerificationInFullTrust = true)]
[assembly: InternalsVisibleToAttribute("xBRZ")]
[assembly: ChangeList("5f9c398:0.13.0-alpha.3-1-g5f9c398")]
[assembly: BuildComputerName("Palatinate")]
[assembly: FullVersion("0.13.0.3-alpha.3")]
// [assembly: SuppressUnmanagedCodeSecurity]

[module: CompilationRelaxations(CompilationRelaxations.NoStringInterning)]
[module: CLSCompliant(false)]
[module: SkipLocalsInit]

[AttributeUsage(validOn: AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
sealed class ChangeListAttribute : Attribute {
	internal readonly string Value;
	internal ChangeListAttribute(string value) => Value = value;
}

[AttributeUsage(validOn: AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
sealed class BuildComputerNameAttribute : Attribute {
	internal readonly string Value;
	internal BuildComputerNameAttribute(string value) => Value = value;
}

[AttributeUsage(validOn: AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
sealed class FullVersionAttribute : Attribute {
	internal readonly string Value;
	internal FullVersionAttribute(string value) => Value = value;
}