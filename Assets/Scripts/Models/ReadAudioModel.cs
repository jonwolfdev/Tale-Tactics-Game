﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public class ReadAudioModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsBgm { get; set; }
        public uint DurationSeconds { get; set; }
        public FileFormatEnum Format { get; set; }
        public string AbsoluteUrl { get; set; }
        public bool IsScanned { get; set; }
        public long Size { get; set; }
    }
}
