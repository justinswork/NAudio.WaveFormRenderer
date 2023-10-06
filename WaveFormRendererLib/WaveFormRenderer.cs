using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using NAudio.Wave;

namespace NAudio.WaveFormRenderer
{
	public class WaveFormRenderer
	{
		public Image Render(WaveStream waveStream, WaveFormRendererSettings settings)
		{
			return Render(waveStream, new MaxPeakProvider(), settings);
		}

		public Image Render(WaveStream waveStream, IPeakProvider peakProvider, WaveFormRendererSettings settings)
		{
			int bytesPerSample = (waveStream.WaveFormat.BitsPerSample / 8);
			var samples = waveStream.Length / (bytesPerSample);
			var samplesPerPixel = (int)(samples / settings.Width);
			var stepSize = settings.PixelsPerPeak + settings.SpacerPixels;
			peakProvider.Init(waveStream.ToSampleProvider(), samplesPerPixel * stepSize);
			if (settings.ImageType == RenderImageType.Bitmap)
				return RenderBitmap(peakProvider, settings);
			else
				return RenderVector(peakProvider, settings);
		}

		private static Image RenderBitmap(IPeakProvider peakProvider, WaveFormRendererSettings settings)
		{
			if (settings.DecibelScale)
				peakProvider = new DecibelPeakProvider(peakProvider, 48);

			var b = new Bitmap(settings.Width, settings.TopHeight + settings.BottomHeight);
			if (settings.BackgroundColor == Color.Transparent)
			{
				b.MakeTransparent();
			}
			using (var g = Graphics.FromImage(b))
			{
				g.FillRectangle(settings.BackgroundBrush, 0, 0, b.Width, b.Height);
				var midPoint = settings.TopHeight;

				int x = 0;
				var currentPeak = peakProvider.GetNextPeak();
				while (x < settings.Width)
				{
					var nextPeak = peakProvider.GetNextPeak();

					for (int n = 0; n < settings.PixelsPerPeak; n++)
					{
						var lineHeight = settings.TopHeight * currentPeak.Max;
						g.DrawLine(settings.TopPeakPen, x, midPoint, x, midPoint - lineHeight);
						lineHeight = settings.BottomHeight * currentPeak.Min;
						g.DrawLine(settings.BottomPeakPen, x, midPoint, x, midPoint - lineHeight);
						x++;
					}

					for (int n = 0; n < settings.SpacerPixels; n++)
					{
						// spacer bars are always the lower of the 
						var max = Math.Min(currentPeak.Max, nextPeak.Max);
						var min = Math.Max(currentPeak.Min, nextPeak.Min);

						var lineHeight = settings.TopHeight * max;
						g.DrawLine(settings.TopSpacerPen, x, midPoint, x, midPoint - lineHeight);
						lineHeight = settings.BottomHeight * min;
						g.DrawLine(settings.BottomSpacerPen, x, midPoint, x, midPoint - lineHeight);
						x++;
					}
					currentPeak = nextPeak;
				}
			}
			return b;
		}

		private static Image RenderVector(IPeakProvider peakProvider, WaveFormRendererSettings settings)
		{
			var imageStream = CreateMemoryEMF(peakProvider, settings);
			return Image.FromStream(imageStream);
		}

		static void SetGraphicsQuality(Graphics graphics)
		{
			graphics.CompositingQuality = CompositingQuality.HighQuality;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.CompositingMode = CompositingMode.SourceOver;
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		}

		static Stream CreateMemoryEMF(IPeakProvider peakProvider, WaveFormRendererSettings settings)
		{
			var result = new MemoryStream();
			Metafile metafile;
			using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
			{
				SetGraphicsQuality(graphics);
				var context = graphics.GetHdc();
				metafile = new Metafile(result, context, EmfType.EmfPlusDual);
				graphics.ReleaseHdc(context);
			}
			DrawGraphics(metafile, peakProvider, settings);
			result.Flush();
			result.Seek(0, SeekOrigin.Begin);
			return result;
		}

		static void DrawGraphics(Metafile metafile, IPeakProvider peakProvider, WaveFormRendererSettings settings)
		{
			using (var graphics = Graphics.FromImage(metafile))
			{
				var midPoint = settings.TopHeight;

				int x = 0;
				var currentPeak = peakProvider.GetNextPeak();
				while (x < settings.Width)
				{
					var nextPeak = peakProvider.GetNextPeak();

					for (int n = 0; n < settings.PixelsPerPeak; n++)
					{
						var lineHeight = settings.TopHeight * currentPeak.Max;
						graphics.DrawLine(settings.TopPeakPen, x, midPoint, x, midPoint - lineHeight);
						lineHeight = settings.BottomHeight * currentPeak.Min;
						graphics.DrawLine(settings.BottomPeakPen, x, midPoint, x, midPoint - lineHeight);
						x++;
					}

					for (int n = 0; n < settings.SpacerPixels; n++)
					{
						// spacer bars are always the lower of the 
						var max = Math.Min(currentPeak.Max, nextPeak.Max);
						var min = Math.Max(currentPeak.Min, nextPeak.Min);

						var lineHeight = settings.TopHeight * max;
						graphics.DrawLine(settings.TopSpacerPen, x, midPoint, x, midPoint - lineHeight);
						lineHeight = settings.BottomHeight * min;
						graphics.DrawLine(settings.BottomSpacerPen, x, midPoint, x, midPoint - lineHeight);
						x++;
					}
					currentPeak = nextPeak;
				}
			}
		}

		static void CreateDiskEMF(string path, Stream metaStream)
		{
			using (var fileStream = File.Create(path))
				metaStream.CopyTo(fileStream);
			metaStream.Seek(0, SeekOrigin.Begin);
		}
	}
}
