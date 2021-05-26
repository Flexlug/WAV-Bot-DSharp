﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WAV_Bot_DSharp.Database.Models;

using WAV_Osu_NetApi.Models;

namespace WAV_Bot_DSharp.Database.Interfaces
{
    public interface IWAVCompitProvider
    {
        /// <summary>
        /// Получить информацию об участии данного пользователя в конкурсах WAV
        /// </summary>
        /// <param name="uid">Discord id</param>
        /// <returns></returns>
        public WAVMemberCompitProfile GetCompitProfile(string uid);

        /// <summary>
        /// Добавить информацию о профиле 
        /// </summary>
        /// <param name="uid">ID участника</param>
        /// <param name="compitProfile">Конкурсный профиль участника</param>
        public void AddCompitProfile(string uid, WAVMemberCompitProfile compitProfile);

        /// <summary>
        /// Добавить в БД информацию о скоре участника
        /// </summary>
        /// <param name="score">Скор участника</param>
        public void SubmitScore(CompitScore score);

        /// <summary>
        /// Получить все скоры конкретного пользователя
        /// </summary>
        /// <param name="uid">Discord ID пользователя</param>
        /// <returns></returns>
        public List<CompitScore> GetUserScores(string uid);

        /// <summary>
        /// Получить лучшие скоры для заданной категории
        /// </summary>
        /// <param name="category">Категория, для которой необходимо получить лучшие скоры</param>
        /// <returns></returns>
        public List<CompitScore> GetCategoryBestScores(CompitCategories category);

        /// <summary>
        /// Удалить все скоры за прошедший конкурс
        /// </summary>
        public void ResetScores();

        /// <summary>
        /// Получить записанную в БД информацию о конкурсе
        /// </summary>
        /// <returns></returns>
        public CompitInfo GetCompitionInfo();

        /// <summary>
        /// Перезаписать информацию о конкурсе
        /// </summary>
        /// <param name="info">Информация о конкурсе</param>
        public void SetCompitionInfo(CompitInfo info);
    }
}
