﻿using System;
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
            var result = client.BodySeg(file.InputStream.ToBytes(), options);
            var imageBase64 = result["foreground"].ToString();
            byte[] imageBytes = imageBase64.Base64StrToBuffer();
            MemoryStream imageBkStream = new MemoryStream();
            imageBkStream.Write(imageBytes, 0, imageBytes.Length);
            Bitmap img = new Bitmap(imageBkStream);
            //背景图
            Image imageBack = Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + template);
            var newImage = CombinImage(imageBack, img);
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
        public static Bitmap CombinImage(Image imageBack, Bitmap img, int xDeviation = 0, int yDeviation = 0)
        {
            Bitmap bmp = new Bitmap(imageBack.Width, imageBack.Height);

            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            g.DrawImage(imageBack, 0, 0, imageBack.Width, imageBack.Height);
            Dictionary<int, List<int>> imgs = new Dictionary<int, List<int>>();
            for (var x = 0; x < img.Width; x++)
            {
                for (var y = 0; y < img.Height; y++)
                {
                    var p = img.GetPixel(x, y);
                    if (p.A > 0)
                    {
                        if (imgs.ContainsKey(x)) {
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
            int realWidth = maxX - minX;
            int realHeight = maxY - minY;
            foreach(var kv in imgs)
            {
                if (kv.Key < minX) minX = kv.Key;
                if (kv.Key > maxX) maxX = kv.Key;
                if (kv.Value.Min() < minY) minY = kv.Value.Min();
                if (kv.Value.Max() > maxY) maxY = kv.Value.Max();
            }
            int x = imageBack.Width< realWidth?0:
            g.DrawImage(img, 0, 0, img.Width / 2, img.Height / 2);
            GC.Collect();
            return bmp;
        }
        public ActionResult Get()
        {
            FileStream fileStream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "images\\background\\classical\\01.jpg", FileMode.Open, FileAccess.Read);
            return File(fileStream, "image/png");
        }


    }
}