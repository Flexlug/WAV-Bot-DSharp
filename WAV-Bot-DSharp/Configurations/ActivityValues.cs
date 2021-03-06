﻿using System;
using System.Collections.Generic;
using System.Text;

namespace WAV_Bot_DSharp.Configurations
{
    public class ActivityValues
    {
        public int VoiceStateUpdated { get; set; }
        public int TypingStarted { get; set; }
        public int MessageUpdated { get; set; }
        public int MessageReactionRemoved { get; set; }
        public int MessageReactionAdded { get; set; }
        public int MessageDeleted { get; set; }
        public int MessageCreated { get; set; }
    }
}
