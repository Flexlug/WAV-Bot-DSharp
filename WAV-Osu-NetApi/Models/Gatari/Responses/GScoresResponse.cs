﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WAV_Osu_NetApi.Gatari.Models.Responses
{
    public class GScoresResponse
    {
        public int code { get; set; }
        public int count { get; set; }
        public List<GScore> scores { get; set; }
    }
}
