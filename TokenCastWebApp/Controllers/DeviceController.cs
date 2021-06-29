using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using TokenCast.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TokenCast.Controllers
{
    public class DeviceController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        // GET: Device/Content?deviceId=test
        public DeviceModel DisplayContent(string deviceId)
        {
            if (deviceId == null)
            {
                return null;
            }

            return Database.GetDeviceContent(deviceId).Result;
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

        public ActionResult GetQRCode(string url)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20, Color.Black, Color.Transparent, drawQuietZones: true);
            using (var stream = new MemoryStream())
            {
                qrCodeImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                Byte[] imageOut = stream.ToArray();
                return File(imageOut, "image/png");
            }
        }
    }
}
