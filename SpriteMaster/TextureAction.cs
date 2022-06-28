using System.Runtime.InteropServices;

namespace SpriteMaster;

[StructLayout(LayoutKind.Auto)]
internal record struct TextureAction(string Name, int Size);
