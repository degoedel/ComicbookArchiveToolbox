using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicbookArchiveToolbox.CommonTools
{
	public class JpgConverter
	{
		private Logger _logger;
		EncoderParameters _encoderParameters;
		ImageCodecInfo _codecInfo;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public JpgConverter(Logger logger, long quality)
		{
			_logger = logger;
			_encoderParameters = new EncoderParameters(1);
			EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
			_encoderParameters.Param[0] = encoderParameter;
			_codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public void SaveJpeg(string inputPath, string outputPath)
		{
			try
			{
				using (Bitmap image = new Bitmap(inputPath))
				{
					image.Save(outputPath, _codecInfo, _encoderParameters);
				}
			}
			catch (Exception e)
			{
				_logger.Log($"Cannot reencode {inputPath} . {e.Message}");
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public void SaveJpeg(Bitmap inputPic, string outputPath)
		{
			try
			{
				inputPic.Save(outputPath, _codecInfo, _encoderParameters);
			}
			catch (Exception e)
			{
				_logger.Log($"Cannot reencode provided bitmap. {e.Message}");
			}
		}


		/// <summary>
		/// Resize the image to the specified width and height.
		/// </summary>
		/// <param name="inputPath">Path to the image to resize.</param>
		/// <param name="width">The width to resize to.</param>
		/// <param name="height">The height to resize to.</param>
		/// <returns>The resized image.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public Bitmap ResizeImageByPx(string inputPath, long height)
		{
			Bitmap destImage = null;
			using (Bitmap image = new Bitmap(inputPath))
			{
				int width = image.Width;
				if (height != image.Height)
				{
					width = (int)(image.Width * height / image.Height);
					destImage = ResizeImage(image, width, (int)height);
				}
				else
				{
					destImage = new Bitmap(inputPath);
				}
			}
			return destImage;
		}


		/// <summary>
		/// Resize the image to the specified ratio.
		/// </summary>
		/// <param name="inputPath">Path to the image to resize.</param>
		/// <param name="ratio">The ratio to resize to.</param>
		/// <returns>The resized image.</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		public Bitmap ResizeImageByRatio(string inputPath, long ratio)
		{
			Bitmap destImage = null;
			using (Bitmap image = new Bitmap(inputPath))
			{
				int width = (int) Math.Floor((double)(image.Width * ratio / 100));
				int height = (int)Math.Floor((double)(image.Height * ratio / 100));
				destImage = ResizeImage(image, width, height);
			}
			return destImage;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
		private Bitmap ResizeImage(Image image, int width, int height)
		{
			var destRect = new Rectangle(0, 0, width, height);
			var destImage = new Bitmap(width, height);
			destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
			using (var graphics = Graphics.FromImage(destImage))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.CompositingQuality = CompositingQuality.HighQuality;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.SmoothingMode = SmoothingMode.HighQuality;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

				using (var wrapMode = new ImageAttributes())
				{
					wrapMode.SetWrapMode(WrapMode.TileFlipXY);
					graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
				}
			}
			return destImage;
		}
	}
}
