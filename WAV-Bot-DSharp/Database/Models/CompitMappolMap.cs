﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WAV_Osu_NetApi.Models.Bancho;

namespace WAV_Bot_DSharp.Database.Models
{
    public class CompitMappolMap
    {
        public CompitMappolMap()
        {
            Voted = new List<string>();
        }

        /// <summary>
        /// Предлагаемая карта
        /// </summary>
        public Beatmap Beatmap { get; set; }

        /// <summary>
        /// Категория, для которой предлагается карта
        /// </summary>
        public CompitCategories Category { get; set; }
        
        /// <summary>
        /// Список проголосовавших
        /// </summary>
        public List<string> Voted { get; set; }

        /// <summary>
        /// Discord ID предложившего карту
        /// </summary>
        public string SuggestedBy { get; set; }
    }
}
