// ImageUtil.cs created with MonoDevelop
// User: damien at 5:43 PM 10/8/2008
//
// Copyright Skull Squadron, All Rights Reserved.

/*
 * Ticket #683 : GIF thumb generation from CJ seems to be broken in Mono/GDI
 * Mono bug is https://bugzilla.novell.com/show_bug.cgi?id=510805 
 * 
 * Under mono, we use gdk.pixbuf to handle image manipulation when exceptions occur from trying to read the stream into a GDI bitmap.
 * In windows, unless you install mono (for windows), and somehow add the gdk-sharp and gtk-sharp refernces, you'll need to undefine GTKSHARP.
 * Unfortunately, there is no easy way to do conditional symbol definitions across the two operating systems.  The good news is that if you
 * are somehow able to do this, pixbuf seems to be more forgiving, and I am able to pass all three GIFs in marc's test file.
 */

#if (!WINDOWS)
#define GTKSHARP
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using System.Drawing.Imaging;

namespace EmergeTk
{
	public static class ImageUtil
	{
		private static readonly EmergeTkLog log = EmergeTkLogManager.GetLogger(typeof(ImageUtil));

//hint: look at top of file.
#if GTKSHARP		
		static ImageUtil()
		{
			string[] args = {};
			Gtk.Init.Check(ref args);
		}
#endif
		
		public static void saveJpeg(string path, Bitmap img, long quality)
		{
			// Encoder parameter for image quality
			EncoderParameter qualityParam =
				new EncoderParameter(Encoder.Quality, (long)quality);
			
			// Jpeg image codec
			ImageCodecInfo jpegCodec =
				getEncoderInfo("image/jpeg");
			
			if(jpegCodec == null)
				return;
			
			EncoderParameters encoderParams = new EncoderParameters(1);
			encoderParams.Param[0] = qualityParam;
			
			img.Save(path, jpegCodec, encoderParams);
		}
		
		public static void resizePng(string sourcePath, string destinationPath, Size size)
		{
			log.Debug("resizing png to", size);
			System.Drawing.Image fullSizeImg
                = System.Drawing.Image.FromFile(sourcePath);
			System.Drawing.Image.GetThumbnailImageAbort dummyCallBack 
				= new System.Drawing.Image.GetThumbnailImageAbort(ThumbnailCallback);
			System.Drawing.Image thumbNailImg 
				= fullSizeImg.GetThumbnailImage(size.Width, size.Height, 
				                                dummyCallBack, IntPtr.Zero);
			fullSizeImg.Dispose();
			
			//Save the thumbnail in PNG format. 		
            thumbNailImg.Save(destinationPath, ImageFormat.Png);
			thumbNailImg.Dispose();	
		}
		

		public static ImageCodecInfo getEncoderInfo(string mimeType)
		{
			// Get image codecs for all image formats
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
			
			// Find the correct image codec
			for (int i = 0; i <codecs.Length; i++)
				if (codecs[i].MimeType == mimeType)
					return codecs[i];
			return null;
		}
		
		private static Image cropImage(Image img, Rectangle cropArea)
		{
			Bitmap bmpImage = new Bitmap(img);
			Bitmap bmpCrop = bmpImage.Clone(cropArea,bmpImage.PixelFormat);
			bmpImage.Dispose();
			return (Image)(bmpCrop);
		}
		
		public static void resizeImageWidth(string sourcePath, string destinationPath, int width)
		{
			resizeImage(sourcePath, destinationPath, new Size(width,0),true);
		}

		public static void resizeImage(string sourcePath, string destinationPath, Size size)
		{
			resizeImage(sourcePath, destinationPath, size, false);
		}
		
		public static void resizeImage(string sourcePath, string destinationPath, Size size, bool onlyWidth)
		{
            log.DebugFormat("resizing image at {0} to {1}", sourcePath, destinationPath);

            if (sourcePath.ToLower().EndsWith(".png"))
			{
				resizePng(sourcePath, destinationPath, size);
			}
			else
			{
                Bitmap imageBitMap = GetBitmap(sourcePath);
				imageBitMap = (Bitmap)resizeImage(imageBitMap,size,onlyWidth);
                saveJpeg(destinationPath, imageBitMap, 80);
				imageBitMap.Dispose();
			}
			return;
		}
		
		public static void scaleAndCrop(string sourcePath, string destinationPath, Size size)
		{
            if (sourcePath.ToLower().EndsWith(".png"))
			{
                resizePng(sourcePath, destinationPath, size);
			}
			else
			{
                Bitmap imageBitMap = GetBitmap(sourcePath); //new Bitmap(sourcePath);
				Bitmap imageBitMap2 = (Bitmap)scaleAndCrop(imageBitMap,size);
                saveJpeg(destinationPath, imageBitMap2, 100);
				imageBitMap.Dispose();
				imageBitMap2.Dispose();
			}
			return;
		}
		
		private static Image scaleAndCrop( Image imgToResize, Size newSize )
		{
			int sourceWidth = imgToResize.Width;
			int sourceHeight = imgToResize.Height;	
			
			float nPercentW = 0;
			float nPercentH = 0;
			
			nPercentW = ((float)newSize.Width / (float)sourceWidth);
			nPercentH = ((float)newSize.Height / (float)sourceHeight);
			
			int scaleW = 0, scaleH = 0;
			
			Rectangle cropRect = new Rectangle( new Point(0,0), newSize );
			
			if( nPercentW > nPercentH )
			{
				scaleW = Convert.ToInt32( (float)sourceWidth * nPercentW );
				scaleH = Convert.ToInt32( (float)sourceHeight * nPercentW );
				
				cropRect.Y = (scaleH-newSize.Height)/2;
			}
			else
			{
				scaleW = Convert.ToInt32( (float)sourceWidth * nPercentH );
				scaleH = Convert.ToInt32( (float)sourceHeight * nPercentH );
				
				cropRect.X = (scaleW-newSize.Width)/2;
			}
			
			Size scaleSize = new Size( scaleW, scaleH );
			
			log.Debug( "scaleSize: ", scaleSize ); 
			
			//imgToResize = resizeImage( imgToResize, new Size( scaleW, scaleH ) );
			
			Bitmap b = new Bitmap(scaleW, scaleH);
			Graphics g = Graphics.FromImage((Image)b);
			//g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			
			g.DrawImage(imgToResize, 0, 0, scaleW, scaleH);
			g.Dispose();
			
			log.Debug( "cropRect: ", cropRect  ); 
			
			log.Debug( "imgToResize: ", imgToResize.Width, imgToResize.Height  );

			Image i = cropImage(b, cropRect);
			b.Dispose();
			return i;
		}
			
		public static Image resizeImage(Image imgToResize, Size size)
		{
			return resizeImage(imgToResize,size,false);
		}
		
		private static Image resizeImage(Image imgToResize, Size size, bool onlyWidth)
		{
			int sourceWidth = imgToResize.Width;
			int sourceHeight = imgToResize.Height;
			
			float nPercent = 0;
			float nPercentW = 0;
			float nPercentH = 0;
			
			nPercentW = ((float)size.Width / (float)sourceWidth);
			nPercentH = ((float)size.Height / (float)sourceHeight);
			
			if (!onlyWidth && nPercentH <nPercentW)
				nPercent = nPercentH;
			else
				nPercent = nPercentW;
			
			int destWidth = (int)(sourceWidth * nPercent);
			int destHeight = (int)(sourceHeight * nPercent);
			
			Bitmap b = new Bitmap(destWidth, destHeight);
			Graphics g = Graphics.FromImage((Image)b);
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			
			g.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
			g.Dispose();
			
			return (Image)b;
		}				
		
		//this function is reqd for thumbnail creation
		public static bool ThumbnailCallback()
		{
			return false;
		}


		public static string WriteStreamToFile(Stream stream)
		{
			string path = Path.GetTempFileName();
			return WriteStreamToFile(stream, path);
		}

		public static string WriteStreamToFile(Stream stream, string path)
		{
			FileStream f = new FileStream(path, FileMode.Create, FileAccess.Write );
			byte[] buffer = new byte[32*1024];
		    int read;			
			
		    while ( (read=stream.Read(buffer, 0, buffer.Length)) > 0)
		    {				
		        f.Write(buffer, 0, read);
		    }		
			f.Close();
			log.Debug("temp path: ", path );
			return path;
		}
		
		public static Image GetImage(Stream stream)
		{
			string file = WriteStreamToFile(stream);
			Image img = GetImage(file);
#if (!WINDOWS)
			File.Delete(file);
#endif
			return img;
		}
		
		public static Bitmap GetBitmap(string file)
		{
			return new Bitmap(GetImage(file));
		}
		
		public static Image GetImage(string file)
		{
			try
			{
				Image i = Bitmap.FromFile(file);	
				return i;
			}
			catch(Exception e )
			{
				log.Error("Error getting image from stream,", e );
//hint: look at top of file.
#if GTKSHARP
				if( e.Message.Contains("GDI+") )
				{
					string path = Path.GetTempFileName() + ".png";
					log.Info("Attempting to load via pixbuf", path);
					Gdk.Pixbuf p = new Gdk.Pixbuf(file);	
					p.Save(path, "png");
					Image i = Bitmap.FromFile(path);
					log.Debug("got image ", i.Size);
					File.Delete(path);
					return i;
				}
#endif
				throw new Exception("EXCEPTION FROM STREAM", e ); 
			}
		}
	}
}
