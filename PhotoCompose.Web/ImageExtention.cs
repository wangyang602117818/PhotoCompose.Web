using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace PhotoCompose.Web
{
    public static class ImageExtention
    {
        private static object o = new object();
        public static ImageFormat GetImageFormat(string ext)
        {
            switch (ext.ToLower())
            {
                case ".jpg":
                    return ImageFormat.Jpeg;
                case ".png":
                    return ImageFormat.Png;
                case ".gif":
                    return ImageFormat.Gif;
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".ico":
                    return ImageFormat.Icon;
                case ".tif":
                    return ImageFormat.Tiff;
            }
            return ImageFormat.Jpeg;
        }
        public static Stream GenerateThumbnail(Stream stream, ImageModelEnum model, int x, int y, ref int width, ref int height)
        {
            string type = GetImageType2(stream);
            bool cut = false;
            if (type == "XML")
            {
                return GenerateSvg(stream, model, ref width, ref height);
            }
            else
            {
                using (Image image = Image.FromStream(stream))
                {
                    switch (model)
                    {
                        case ImageModelEnum.scale:
                            if (width == 0 && height == 0)
                            {
                                width = image.Width;
                                height = image.Height;
                            }
                            else if (width == 0 && height > 0)
                            {
                                width = image.Width * height / image.Height;
                            }
                            else if (width > 0 && height == 0)
                            {
                                height = image.Height * width / image.Width;
                            }
                            break;
                        case ImageModelEnum.height:
                            width = image.Width * height / image.Height;
                            break;
                        case ImageModelEnum.width:
                            height = image.Height * width / image.Width;
                            break;
                        case ImageModelEnum.cut:
                            cut = true;
                            break;
                    }
                    if (width > image.Width) width = image.Width;
                    if (height > image.Height) height = image.Height;
                    return ConvertImage(image, image.RawFormat, x, y, width, height, cut);
                }
            }
        }
        public static Stream GenerateFilePreview(int fileHW, Stream stream, ImageModelEnum model, ref int width, ref int height)
        {
            string type = GetImageType2(stream);
            bool isGif = type == "GIF";
            if (type == "XML")
            {
                width = fileHW;
                height = fileHW;
                return GenerateSvg(stream, model, ref width, ref height);
            }
            else
            {
                using (Image image = Image.FromStream(stream))
                {
                    //原图比较宽
                    if (image.Width >= image.Height)
                    {
                        width = image.Width > fileHW ? fileHW : image.Width;  //原图比指定的宽度要宽，就是用指定的宽度，否则使用原图宽
                        height = image.Height * width / image.Width;
                    }
                    //原图比较高
                    if (image.Width < image.Height)
                    {
                        height = image.Height > fileHW ? fileHW : image.Height; //原图比指定的高度要高，就是用指定的高度，否则使用原图高
                        width = image.Width * height / image.Height;
                    }
                    return ConvertImage(image, image.RawFormat, 0, 0, width, height, false);
                }
            }
        }
        private static Stream GenerateSvg(Stream stream, ImageModelEnum model, ref int width, ref int height)
        {
            string text = stream.ToStr();
            string pattern = @"<svg\s*(.|\n)+?>";
            string widthPattern = @"width=""(\d+)(px)?""";
            string heightPattern = @"height=""(\d+)(px)?""";
            string svgTag = Regex.Match(text, pattern).Groups[0].Value;
            string newSvgTag = svgTag;
            var widthMath = Regex.Match(svgTag, widthPattern);
            int swidth = widthMath.Success ? int.Parse(widthMath.Groups[1].Value) : 1024;
            var heightMath = Regex.Match(svgTag, heightPattern);
            int sheight = heightMath.Success ? int.Parse(widthMath.Groups[1].Value) : 1024;
            switch (model)
            {
                case ImageModelEnum.scale:
                    if (width == 0 && height == 0)
                    {
                        width = swidth;
                        height = sheight;
                    }
                    else if (width == 0 && height > 0)
                    {
                        width = swidth * height / sheight;
                    }
                    else if (width > 0 && height == 0)
                    {
                        height = sheight * width / swidth;
                    }
                    break;
                case ImageModelEnum.height:
                    if (sheight != 0 && swidth != 0)
                    {
                        width = swidth * height / sheight;
                    }
                    else
                    {
                        width = height;
                    }
                    break;
                case ImageModelEnum.width:
                    if (sheight != 0 && swidth != 0)
                    {
                        height = sheight * width / swidth;
                    }
                    else
                    {
                        height = width;
                    }
                    break;
            }
            if (newSvgTag.Contains("width"))
            {
                newSvgTag = Regex.Replace(newSvgTag, widthPattern, "width=\"" + width + "\"");
            }
            else
            {
                newSvgTag = newSvgTag.TrimEnd('>') + " width=\"" + width + "\"" + ">";
            }
            if (newSvgTag.Contains("height"))
            {
                newSvgTag = Regex.Replace(newSvgTag, heightPattern, "height=\"" + height + "\"");
            }
            else
            {
                newSvgTag = newSvgTag.TrimEnd('>') + " height=\"" + height + "\"" + ">";
            }
            text = text.Replace(svgTag, newSvgTag);
            return text.ToStream();

        }
        private static Stream ConvertImage(Image image, ImageFormat outputFormat, int x, int y, int width, int height, bool cut)
        {
            Stream stream = new MemoryStream();
            using (Bitmap bmp = new Bitmap(width, height))  //新建一个图片
            {
                using (Graphics g = Graphics.FromImage(bmp)) //画板
                {
                    g.InterpolationMode = InterpolationMode.Low;
                    g.SmoothingMode = SmoothingMode.Default;
                    g.Clear(Color.Transparent);
                    if (cut)
                    {
                        g.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
                    }
                    else
                    {
                        g.DrawImage(image, new Rectangle(0, 0, width, height), new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                    }
                }
                bmp.Save(stream, outputFormat);
            }
            stream.Position = 0;
            return stream;
        }
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        private static void bindProperty(Image a, Image b)
        {
            for (int i = 0; i < a.PropertyItems.Length; i++)
            {
                b.SetPropertyItem(a.PropertyItems[i]);
            }
        }
        public static string GetImageType2(Stream stream)
        {
            string headerCode = GetHeaderInfo(stream).ToUpper();
            if (headerCode.StartsWith("FFD8FF"))
            {
                return "JPG";
            }
            else if (headerCode.StartsWith("49492A"))
            {
                return "TIFF";
            }
            else if (headerCode.StartsWith("424D"))
            {
                return "BMP";
            }
            else if (headerCode.StartsWith("474946"))
            {
                return "GIF";
            }
            else if (headerCode.StartsWith("89504E470D0A1A0A"))
            {
                return "PNG";
            }
            else if (headerCode.StartsWith("3C3F786D6C"))
            {
                return "XML";
            }
            else
            {
                return "";
            }
        }
        public static string GetHeaderInfo(Stream stream)
        {
            byte[] buffer = new byte[8];
            BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
            reader.Read(buffer, 0, buffer.Length);
            reader.Close();
            StringBuilder sb = new StringBuilder();
            foreach (byte b in buffer)
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
    public enum ImageModelEnum
    {
        /// <summary>
        /// 缩放
        /// </summary>
        scale = 0,
        /// <summary>
        /// 剪切
        /// </summary>
        cut = 1,
        /// <summary>
        /// 按宽度
        /// </summary>
        width = 2,
        /// <summary>
        /// 按高度
        /// </summary>
        height = 3
    }
}