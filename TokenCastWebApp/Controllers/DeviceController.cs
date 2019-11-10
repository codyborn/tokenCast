using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    }
}
