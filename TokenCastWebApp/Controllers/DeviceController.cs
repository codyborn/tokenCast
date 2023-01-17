﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Utilities.Zlib;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using TokenCast.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TokenCast.Controllers
{
    [Route("device")]
    public class DeviceController : Controller
    {
        // GET: /<controller>/

        private readonly IDatabase Database;

        public DeviceController(IDatabase database)
        {
            Database = database;
        }

        public IActionResult Index()
        {
            return View();
        }

        // GET: Device/Content?deviceId=test
        [HttpGet("content")]
        public async Task<IActionResult> DisplayContent([FromQuery] string deviceId, [FromQuery] int width, [FromQuery] int height, [FromQuery] int skip = 0, [FromQuery] int take = 10)
        {
            if (deviceId == null)
            {
                return BadRequest();
            }

            var content = await Database.GetDeviceContent(deviceId);
            using (var webClient = new WebClient())
            {
                byte[] imageBytes = webClient.DownloadData(content.currentDisplay.tokenImageUrl);
                using(var stream = new MemoryStream(imageBytes))
                {
                    var image = Image.Load(stream);
                    image.Mutate(x =>
                    {
                        x.Resize(new ResizeOptions
                        {
                            Size = new Size
                            {
                                Height = height,
                                Width = width
                            },
                            Mode = ResizeMode.Max
                        });
                        
                    });
                    
                    using (var resizedStream = new MemoryStream())
                    {
                        await image.SaveAsJpegAsync(resizedStream);

                        var inputAsString = Convert.ToBase64String(resizedStream.ToArray());

                        return Ok(new
                        {
                            c = new string(inputAsString.Skip(skip).Take(take).ToArray()),
                            t = inputAsString.Length
                        });
                    }
                }
            }
        }

        // GET: LastUpdateTime
        // Used to force refresh of client
        public long LastUpdateTime()
        {
            LastUpdateModel lastUpdated = Database.GetLastUpdateTime().Result;
            if (lastUpdated == null)
            {
                return 0;
            }

            return lastUpdated.time.Ticks;
        }

        //public ActionResult GetQRCode(string url, string color = "black")
        //{
        //    Color qrColor = Color.Black;
        //    if (color == "white")
        //    {
        //        qrColor = Color.White;
        //    }
        //    QRCodeGenerator qrGenerator = new QRCodeGenerator();
        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        //    QRCode qrCode = new QRCode(qrCodeData);
        //    Bitmap qrCodeImage = qrCode.GetGraphic(20, qrColor, Color.Transparent, drawQuietZones: true);
        //    using (var stream = new MemoryStream())
        //    {
        //        qrCodeImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        //        Byte[] imageOut = stream.ToArray();
        //        return File(imageOut, "image/png");
        //    }
        //}

        /// <summary>
        /// Sets white labeler field for the device
        /// </summary>
        [HttpPost]
        public void SetDeviceWhitelabeler(string deviceId, string whitelabel)
        {
            Database.SetDeviceWhiteLabeler(deviceId, whitelabel).Wait();
        }

        /// <summary>
        /// Get the device count by whitelabeler
        /// </summary>
        [HttpGet]
        public Dictionary<string, Dictionary<string, int>> GetDeviceCount()
        {
            List<DeviceModel> devices = Database.GetAllDevices().Result;
            var whiteLabelerActiveVsTotal = new Dictionary<string, Dictionary<string, int>>();
            foreach (var device in devices)
            {
                if (device.whiteLabeler == null)
                {
                    device.whiteLabeler = "null";
                }

                if (!whiteLabelerActiveVsTotal.ContainsKey(device.whiteLabeler))
                {
                    whiteLabelerActiveVsTotal[device.whiteLabeler] = new Dictionary<string, int>();
                    whiteLabelerActiveVsTotal[device.whiteLabeler]["active"] = 0;
                    whiteLabelerActiveVsTotal[device.whiteLabeler]["total"] = 0;
                }
                whiteLabelerActiveVsTotal[device.whiteLabeler]["total"]++;
                whiteLabelerActiveVsTotal[device.whiteLabeler]["active"] += device.currentDisplay == null ? 0 : 1;
            }

            return whiteLabelerActiveVsTotal;
        }
    }
}
