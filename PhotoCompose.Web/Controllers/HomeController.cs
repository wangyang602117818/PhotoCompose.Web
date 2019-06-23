using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PhotoCompose.Web.Controllers
{
    public class HomeController : Controller
    {
        string apiKey = ConfigurationManager.AppSettings["apiKey"];
        string secretKey = ConfigurationManager.AppSettings["secretKey"];

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ComposeImage(HttpPostedFileBase file, string template)
        {
            var client = new Baidu.Aip.BodyAnalysis.Body(apiKey, secretKey);
            var options = new Dictionary<string, object>
            {
                {"type", "foreground"}
            };
            var result = client.BodySeg(file.InputStream.ToBytes(),options);
            var imageBase64 = result["foreground"].ToString();
            byte[] imageBytes = imageBase64.Base64StrToBuffer();
            MemoryStream imageBkStream = new MemoryStream();
            imageBkStream.Write(imageBytes, 0, imageBytes.Length);
            Image imageBack = Image.FromStream(imageBkStream);
            //背景图
            Image img = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + template);
            var newImage = CombinImage(img, imageBack);
            MemoryStream memoryStream = new MemoryStream();
            newImage.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Position = 0;
            return File(memoryStream, "image/png");
        }
        /// <summary>
        /// 合并图片
        /// </summary>
        /// <param name="imgBack"></param>
        /// <param name="img"></param>
        /// <param name="xDeviation"></param>
        /// <param name="yDeviation"></param>
        /// <returns></returns>
        public static Bitmap CombinImage(Image imgBack, Image img, int xDeviation = 0, int yDeviation = 0)
        {
            Bitmap bmp = new Bitmap(imgBack.Width, imgBack.Height);

            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.DrawImage(imgBack, 0, 0, imgBack.Width, imgBack.Height);

            g.DrawImage(img, imgBack.Width / 2 - img.Width / 2 + xDeviation, imgBack.Height / 2 - img.Height / 2 + yDeviation, img.Width, img.Height);
            GC.Collect();
            return bmp;
        }
        public ActionResult Get()
        {
            FileStream fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "images\\background\\classical\\01.jpg",FileMode.Open,FileAccess.Read);
            return File(fileStream, "image/png");
        }


    }
}