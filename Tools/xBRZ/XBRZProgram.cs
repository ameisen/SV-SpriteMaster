using SpriteMaster.Extensions;
using SpriteMaster.Types;
using SpriteMaster.Types.Fixed;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Tools;

public static class XBRZProgram {
	private static readonly Colors.ColorSpace ColorSpace = Colors.ColorSpace.sRGB_Precise;

	private record struct Job(Uri Path, int Scale);

	public static int Main(string[] args) {
		bool info = true;
		
		var jobs = new HashSet<Job>();
		foreach (var arg in args) {
			jobs.Add(
				new(new Uri(arg), 6)
			);
		}

		if (info) {
			Console.WriteLine("Settings:");
			var settings = new[] {
				("LuminanceWeight", Config.Resample.xBRZ.LuminanceWeight),
				("EqualColorTolerance", Config.Resample.xBRZ.EqualColorTolerance),
				("DominantDirectionThreshold", Config.Resample.xBRZ.DominantDirectionThreshold),
				("SteepDirectionThreshold", Config.Resample.xBRZ.SteepDirectionThreshold),
				("CenterDirectionBias", Config.Resample.xBRZ.CenterDirectionBias)
			};

			var maxKeyKength = settings.Select(s => s.Item1.Length).Max();
			foreach (var setting in settings) {
				var key = setting.Item1;
				var value = setting.Item2;

				key = key.PadRight(maxKeyKength);
				Console.WriteLine($"  {key} : {value}");
			}
			Console.WriteLine();
		}

		if (jobs.Count == 0) {
			Console.Error.WriteLine("No files provided!");
			return -1;
		}

		foreach (var job in jobs) {
			try {
				ProcessJob(job);
			}
			catch (Exception ex) {
				Console.Error.WriteLine(ex.ToString());
			}
		}

		return 0;
	}

	private static unsafe Span<Color8> ReadFile(Uri path, out Vector2I size) {
		using var rawImage = Image.FromFile(path.LocalPath);
		using var image = new Bitmap(rawImage.Width, rawImage.Height, PixelFormat.Format32bppArgb);
		using (Graphics g = Graphics.FromImage(image)) {
			g.DrawImage(rawImage, 0, 0);
		}

		if (image is null) {
			throw new NullReferenceException(nameof(image));
		}

		var imageData = image.LockBits(new Rectangle(Point.Empty, image.Size), ImageLockMode.ReadOnly, image.PixelFormat);

		var imageSpan = SpanExt.MakeUninitialized<Color8>(image.Width * image.Height);
		var sourceSize = imageData.Height * imageData.Stride;
		var sourceData = new ReadOnlySpan<Color8>(imageData.Scan0.ToPointer(), sourceSize);
		int destOffset = 0;
		int sourceOffset = 0;
		for (int y = 0; y < imageData.Height; ++y) {
			sourceData.Slice(sourceOffset, imageData.Width).CopyTo(
				imageSpan.Slice(destOffset, imageData.Width)
			);

			destOffset += imageData.Width;
			sourceOffset += imageData.Stride;
		}

		image.UnlockBits(imageData);

		size = image.Size;
		return imageSpan;
	}

	private static unsafe void ProcessJob(in Job job) {
		var imageDataNarrow = ReadFile(job.Path, out var imageSize);

		// Widen
		var imageData = Color16.Convert(imageDataNarrow);
		var originalImageData = imageData;

		// Reverse Alpha-Premultiplication
		Resample.Passes.PremultipliedAlpha.Reverse(imageData, imageSize);

		// Linearize
		Resample.Passes.GammaCorrection.Linearize(imageData, imageSize);

		// TODO : padding?
		// Padding?

		var scalerConfig = new xBRZ.Config(
			wrapped: Vector2B.False,
			luminanceWeight: Config.Resample.xBRZ.LuminanceWeight,
			equalColorTolerance: Config.Resample.xBRZ.EqualColorTolerance,
			dominantDirectionThreshold: Config.Resample.xBRZ.DominantDirectionThreshold,
			steepDirectionThreshold: Config.Resample.xBRZ.SteepDirectionThreshold,
			centerDirectionBias: Config.Resample.xBRZ.CenterDirectionBias
		);
		imageData = xBRZ.Scaler.

		// Delinearize
		Resample.Passes.GammaCorrection.Delinearize(imageData, imageSize);

		// Alpha-Premultiplication
		Resample.Passes.PremultipliedAlpha.Apply(imageData, imageSize);

		// Narrow

	}
}
