using System.Runtime.Intrinsics.X86;

namespace SpriteMaster.Extensions.Simd;
internal static class Support {
	internal static bool UseAVX2 = true;



	static Support() {
		if (X86Base.IsSupported) {
			var (Eax, Ebx, Ecx, Edx) = X86Base.CpuId(0, 0);

			var (Eax2, Ebx2, Ecx2, Edx2) = X86Base.CpuId(1, 0);
			int stepping = Eax2 & 0b1111;
			int model = (Eax2 >> 4) & 0b1111;
			int familyId = (Eax2 >> 8) & 0b1111;
			int procType = (Eax2 >> 12) & 0b11;
			int extendedModelId = (Eax2 >> 16) & 0b1111;
			int extendedFamilyId = (Eax2 >> 20) & 0b1111_1111;

			// AMD
			if (Ebx == 0x68747541 && Ecx == 0x444d4163 && Edx == 0x69746e65) {
				// AMD can always use AVX2, if it is supported
				UseAVX2 = true;
			}
			// Intel
			else if (Ebx == 0x756e6547 && Ecx == 0x6c65746e && Edx == 0x49656e69) {
				// Haswell
				//	0x6 0x4 0x6
				//	0x6 0x4 0x5
				//	0x6 0x3 0xC
				//	0x6 0x3 0xF
				// Broadwell
				//  0x6 0x4 0x7
				//  0x6 0x3 0xD
				//  0x6 0x4 0xF
				//  0x6 0x5 0x6 
				// Skylake
				//	0x6 0x5 0xE
				//	0x6 0x4 0xE
				//  0x6 0x5 0x5 
				// Kaby Lake
				//	0x6 0x9 0xE 
				//	0x6 0x8 0xE 
				// Coffee Lake
				//	0x6 0x9 0xE 
				//	0x6 0x8 0xE 
				// Cannon Lake
				//	0x6 0x6 0x6 

				UseAVX2 = true;
				if (familyId == 0x6 && extendedFamilyId == 0x0) {
					switch (extendedModelId) {
						case 0x3:
							switch (model) {
								case 0xC: // Haswell
									UseAVX2 = false;
									break;
								case 0xD: // Broadwell
									UseAVX2 = false;
									break;
								case 0xF: // Haswell
									UseAVX2 = false;
									break;
							}
							break;
						case 0x4:
							switch (model) {
								case 0x5: // Haswell
									UseAVX2 = false;
									break;
								case 0x6: // Haswell
									UseAVX2 = false;
									break;
								case 0x7: // Broadwell
									UseAVX2 = false;
									break;
								case 0xE: // Skylake
									UseAVX2 = false;
									break;
								case 0xF: // Broadwell
									UseAVX2 = false;
									break;
							}
							break;
						case 0x5:
							switch (model) {
								case 0x5: // Skylake
									UseAVX2 = false;
									break;
								case 0x6: // Broadwell
									UseAVX2 = false;
									break;
								case 0xE: // Skylake
									UseAVX2 = false;
									break;
							}
							break;
						case 0x6:
							switch (model) {
								case 0x6: // Cannon Lake
									UseAVX2 = false;
									break;
							}
							break;
						case 0x8:
							switch (model) {
								case 0xE: // Cannon Lake / Kaby Lake
									UseAVX2 = false;
									break;
							}
							break;
						case 0x9:
							switch (model) {
								case 0xE: // Cannon Lake / Kaby Lake
									UseAVX2 = false;
									break;
							}
							break;
					}
				}
			}
			else {
				// For now, just don't allow AVX2
				UseAVX2 = false;
			}
		}
	}
}
