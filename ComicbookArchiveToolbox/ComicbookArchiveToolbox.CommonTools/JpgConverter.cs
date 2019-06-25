using System;
using System.Collections.Generic;
using System.Drawing;
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

		public JpgConverter(Logger logger, long quality)
		{
			_logger = logger;
			_encoderParameters = new EncoderParameters(1);
			EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
			_encoderParameters.Param[0] = encoderParameter;
			_codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
		}

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
	}
}
