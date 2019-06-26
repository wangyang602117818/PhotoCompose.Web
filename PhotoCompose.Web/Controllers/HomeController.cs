using MongoDB.Bson;
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
        public ActionResult RemoveBackground(HttpPostedFileBase file)
        {
            var client = new Baidu.Aip.BodyAnalysis.Body(apiKey, secretKey);
            var options = new Dictionary<string, object>
            {
                {"type", "foreground"}
            };
            var result = client.BodySeg(file.InputStream.ToBytes(), options);
            var imageBase64 = result["foreground"].ToString();
            byte[] imageBytes = imageBase64.Base64StrToBuffer();
            //剪切背景图
            MemoryStream imageBkStream = new MemoryStream();
            imageBkStream.Write(imageBytes, 0, imageBytes.Length);
            Bitmap img = new Bitmap(imageBkStream);

            Dictionary<int, List<int>> imgs = new Dictionary<int, List<int>>();
            for (var x = 0; x < img.Width; x++)
            {
                for (var y = 0; y < img.Height; y++)
                {
                    var p = img.GetPixel(x, y);
                    if (p.A > 0)
                    {
                        if (imgs.ContainsKey(x))
                        {
                            imgs[x].Add(y);
                        }
                        else
                        {
                            imgs.Add(x, new List<int>());
                        }
                    }
                }
            }
            int minX = img.Width, minY = img.Height;
            int maxX = 0, maxY = 0;
            foreach (var kv in imgs)
            {
                if (kv.Key < minX) minX = kv.Key;
                if (kv.Key > maxX) maxX = kv.Key;
                if (kv.Value.Min() < minY) minY = kv.Value.Min();
                if (kv.Value.Max() > maxY) maxY = kv.Value.Max();
            }
            int realWidth = maxX - minX;
            int realHeight = maxY - minY;
            Stream newImageBkStream = ImageExtention.GenerateThumbnail(imageBkStream, ImageModelEnum.cut, minX, minY, ref realWidth, ref realHeight);
            //保存磁盘
            string newpath = AppDomain.CurrentDomain.BaseDirectory + "images\\process_photo\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";
            string fileName = ObjectId.GenerateNewId().ToString() + ".png";
            if (!Directory.Exists(newpath)) Directory.CreateDirectory(newpath);
            using (FileStream fileStream = new FileStream(newpath + fileName, FileMode.Create, FileAccess.Write))
            {
                newImageBkStream.CopyTo(fileStream);
            }
            return Json(new { path = "process_photo-" + DateTime.Now.ToString("yyyyMMdd") + "-" + fileName, width = realWidth, height = realHeight });
        }

        public ActionResult ComposeImage(string photopath, string template, int photo_x = 0, int photo_y = 0, int photo_width = 0, int photo_height = 0)
        {
            //背景图
            Image imageBack = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + template);
            int Hw = Math.Max(imageBack.Width, imageBack.Height);
            //人物图
            string[] patharray = photopath.Split('-');
            string path = AppDomain.CurrentDomain.BaseDirectory + "images\\" + patharray[0] + "\\" + patharray[1] + "\\" + patharray[2];
            Bitmap img = new Bitmap(path);
            //组合图
            var newImage = CombinImage(imageBack, img, photo_x, photo_y, photo_width, photo_height);
            //保存磁盘
            string newpath = AppDomain.CurrentDomain.BaseDirectory + "images\\result\\" + DateTime.Now.ToString("yyyyMMdd") + "\\";
            string fileName = ObjectId.GenerateNewId().ToString() + ".png";
            if (!Directory.Exists(newpath)) Directory.CreateDirectory(newpath);
            newImage.Save(newpath + fileName);
            return Content("result-" + DateTime.Now.ToString("yyyyMMdd") + "-" + fileName);
        }
        public ActionResult Get(string path)
        {
            string[] array = path.Split('-');
            return File(AppDomain.CurrentDomain.BaseDirectory + "images\\" + array[0] + "\\" + array[1] + "\\" + array[2], "image/png");
        }
        /// <summary>
        /// 合并图片
        /// </summary>
        /// <param name="imgBack"></param>
        /// <param name="img"></param>
        /// <param name="xDeviation"></param>
        /// <param name="yDeviation"></param>
        /// <returns></returns>
        public static Bitmap CombinImage(Image imageBack, Bitmap img, int x, int y, int width, int height)
        {
            Bitmap bmp = new Bitmap(imageBack.Width, imageBack.Height);

            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.DrawImage(imageBack, 0, 0, imageBack.Width, imageBack.Height);
            g.DrawImage(img, x, y, width, height);
            GC.Collect();
            return bmp;
        }


    }
}