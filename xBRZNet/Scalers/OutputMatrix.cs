﻿using System.Runtime.CompilerServices;
using xBRZNet2.Common;

namespace xBRZNet2.Scalers {
	//access matrix area, top-left at position "out" for image with given width
	internal struct OutputMatrix {
		private readonly int[] _out;
		private readonly int _outWidth;
		private readonly int _n;
		private int _outi;
		private int _nr;

		private const int MaxScale = 6; // Highest possible scale
		private const int MaxScaleSquared = MaxScale * MaxScale;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public OutputMatrix (int scale, int[] outPtr, int outWidth) {
			_n = (scale - 2) * (Rotator.MaxRotations * MaxScaleSquared);
			_out = outPtr;
			_outWidth = outWidth;
			_outi = 0;
			_nr = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Move (int rotDeg, int outi) {
			_nr = _n + rotDeg * MaxScaleSquared;
			_outi = outi;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetIndex (int i, int j) {
			var rot = MatrixRotation[_nr + i * MaxScale + j];
			return (_outi + rot.J + rot.I * _outWidth);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set (int i, int j, int value) {
			_out[GetIndex(i, j)] = value;
		}

		// TODO : I _really_ don't like this but I don't want to fully refactor ScalerImplementations.cs right now.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref int Ref (int i, int j) {
			return ref _out[GetIndex(i, j)];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int Get (int i, int j) {
			return _out[GetIndex(i, j)];
		}

		//calculate input matrix coordinates after rotation at program startup
		private static readonly IntPair[] MatrixRotation = new IntPair[(MaxScale - 1) * MaxScaleSquared * Rotator.MaxRotations];

		static OutputMatrix () {
			for (var n = 2; n < MaxScale + 1; n++) {
				for (var r = 0; r < Rotator.MaxRotations; r++) {
					var nr = (n - 2) * (Rotator.MaxRotations * MaxScaleSquared) + r * MaxScaleSquared;
					for (var i = 0; i < MaxScale; i++) {
						for (var j = 0; j < MaxScale; j++) {
							MatrixRotation[nr + i * MaxScale + j] = BuildMatrixRotation(r, i, j, n);
						}
					}
				}
			}
		}

		private static IntPair BuildMatrixRotation (int rotDeg, int i, int j, int n) {
			int iOld, jOld;

			if (rotDeg == 0) {
				iOld = i;
				jOld = j;
			}
			else {
				//old coordinates before rotation!
				var old = BuildMatrixRotation(rotDeg - 1, i, j, n);
				iOld = n - 1 - old.J;
				jOld = old.I;
			}

			return new IntPair(iOld, jOld);
		}
	}
}
