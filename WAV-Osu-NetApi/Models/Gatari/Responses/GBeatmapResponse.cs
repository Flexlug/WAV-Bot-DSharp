﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WAV_Osu_NetApi.Gatari.Models.Responses
{
    public class GBeatmapResponse
    {
        public int code { get; set; }
        public List<GBeatmap> data { get; set; }
    }
}
