﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TokenCast.Models
{
    public class DeviceModel
    {
        public string id { get; set; }

        public Display currentDisplay { get; set; }

        public List<Display> castedTokens { get; set; }

        public string whiteLabeler { get; set; }

        public bool isCanviaDevice { get; set; }
        
        public int frequencyOfRotation { get; set; }

        public DeviceModel()
        {

        }
        public DeviceModel(string id, Display currentDisplay)
        {
            this.id = id;
            this.currentDisplay = currentDisplay;
        }
    }

    public class Display
    {
        public string tokenName { get; set; }
        
        public Uri tokenOwnershipUrl { get; set; }

        public string tokenMetadata { get; set; }

        public Uri tokenImageUrl { get; set; }

        public int borderWidthPercent { get; set; }

        public bool fitScreen { get; set; }

        public string backgroundColor { get; set; }

        public bool orientationVertical { get; set; }
     
        public string currentPrice { get; set; }
    }
}
