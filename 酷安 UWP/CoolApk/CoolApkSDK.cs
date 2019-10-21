﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Web.Http;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http.Filters;

namespace 酷安_UWP
{
    static class CoolApkSDK
    {
        //超级感谢！！！👉 https://github.com/ZCKun/CoolapkTokenCrack
        static string GetAppToken()
        {
            string DEVICE_ID = Guid.NewGuid().ToString();
            long UnixDate = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
            string t = UnixDate.ToString();
            string hex_t = "0x" + string.Format("{0:x}", UnixDate);
            // 时间戳加密
            string md5_t = GetMD5(t);
            string a = "token://com.coolapk.market/c67ef5943784d09750dcfbb31020f0ab?" + md5_t + "$" + DEVICE_ID + "&com.coolapk.market";
            string md5_a = GetMD5(Convert.ToBase64String(Encoding.UTF8.GetBytes(a)));
            string token = md5_a + DEVICE_ID + hex_t;
            return token;
        }

        //来源：https://blog.csdn.net/lindexi_gd/article/details/48951849
        static string GetMD5(string inputString)
        {
            CryptographicHash objHash = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash();
            objHash.Append(CryptographicBuffer.ConvertStringToBinary(inputString, BinaryStringEncoding.Utf8));
            IBuffer buffHash1 = objHash.GetValueAndReset();
            return CryptographicBuffer.EncodeToHexString(buffHash1);
        }

        public static async Task<string> GetCoolApkMessage(string url)
        {
            //这里感谢https://github.com/bjzhou/Coolapk-kotlin提供的 HTTP 头！！！！！！！！！！！！！

            try
            {
                var mClient = new HttpClient();

                mClient.DefaultRequestHeaders.UserAgent.ParseAdd(" +CoolMarket/9.2.2-1905301");
                //mClient.DefaultRequestHeaders.Add("User-Agent", "Dalvik/2.1.0 (Linux; U; Android 9; MI 8 SE MIUI/9.5.9) (#Build; Xiaomi; MI 8 SE; PKQ1.181121.001; 9) +CoolMarket/9.2.2-1905301");
                mClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                mClient.DefaultRequestHeaders.Add("X-Sdk-Int", "28");
                mClient.DefaultRequestHeaders.Add("X-Sdk-Locale", "zh-CN");
                mClient.DefaultRequestHeaders.Add("X-App-Id", "com.coolapk.market");
                mClient.DefaultRequestHeaders.Add("X-App-Token", GetAppToken());
                mClient.DefaultRequestHeaders.Add("X-App-Version", "9.2.2");
                mClient.DefaultRequestHeaders.Add("X-App-Code", "1905301");
                mClient.DefaultRequestHeaders.Add("X-Api-Version", "9");
                //mClient.DefaultRequestHeaders.Add("X-App-Device", "QRTBCOgkUTgsTat9WYphFI7kWbvFWaYByO1YjOCdjOxAjOxEkOFJjODlDI7ATNxMjM5MTOxcjMwAjN0AyOxEjNwgDNxITM2kDMzcTOgsTZzkTZlJ2MwUDNhJ2MyYzM");
                //mClient.DefaultRequestHeaders.Add("X-Dark-Mode", "0");
                mClient.DefaultRequestHeaders.Add("Host", "api.coolapk.com");
                return await mClient.GetStringAsync(new Uri("https://api.coolapk.com/v6" + url));
            }
            catch
            {
                throw;
            }
        }

        public static async Task<string> GetUserIDByName(string name)
        {
            try
            {
                string uid = await Web.GetHttp("https://www.coolapk.com/n/" + name);
                uid = uid.Split(new string[] { "coolmarket://www.coolapk.com/u/" },StringSplitOptions.RemoveEmptyEntries)[1];
                uid = uid.Split(new string[] { @"""" },StringSplitOptions.RemoveEmptyEntries)[0];
                return uid;
            }
            catch
            {
                throw;
            }
        }


        public static async Task<JObject> GetUserProfileByID(string uid)
        {
            string result = await GetCoolApkMessage("/user/space?uid=" + uid);
            return (JObject)((JObject)JsonConvert.DeserializeObject(result))["data"];
        }

        /**
            * 根據用戶名獲得用戶信息，失敗返回null
            *
            * @param name 用戶名
            * @return 用戶信息
            *
        public static async Task<JObject> GetUserProfileByName(string name)
        {
            try
            {
                string uid = await GetUserIDByName(name);
                return await GetUserProfileByID(uid);
            }
            catch (Exception)
            {
                return null;
            }
        }*/

        public static async Task<JArray> GetFeedListByID(string uid, string page, string firstItem, string lastItem)
        {
            try
            {
                string str = await GetCoolApkMessage($"/user/feedList?uid={uid}&page={page}&firstItem={firstItem}&lastItem={lastItem}");
                JObject jo = (JObject)JsonConvert.DeserializeObject(str);
                return jo.HasValues ? (JArray)jo["data"] : new JArray();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<JArray> GetIndexList(string page)
        {
            try
            {
                string str = await GetCoolApkMessage("/main/indexV8?page=" + page);
                JObject jo = (JObject)JsonConvert.DeserializeObject(str);
                return (JArray)jo["data"];
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<JObject> GetFeedDetailById(string feedId)
        {
            try
            {
                string result = await GetCoolApkMessage("/feed/detail?id=" + feedId);
                JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                return (JObject)jo["data"];
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<JArray> GetAnswerListById(string feedId, string sortType, string page, string firstItem, string lastItem)
        {
            try
            {
                string result = await GetCoolApkMessage($"/question/answerList?id={feedId}&sort={sortType}&page={page}&firstItem={firstItem}&lastItem={lastItem}");
                JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                return (JArray)jo["data"];
            }
            catch (Exception)
            {
                throw;
            }
        }
        //回复
        public static async Task<JArray> GetFeedReplyListById(string feedId, string listType, string page, string fromFeedAuthor, string firstItem, string lastItem)
        {
            try
            {
                string result = await GetCoolApkMessage($"/feed/replyList?id={feedId}&listType={listType}&page={page}&firstItem={firstItem}&lastItem={lastItem}&discussMode=1&feedType=feed&blockStatus=0&fromFeedAuthor={fromFeedAuthor}");
                JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                return (JArray)jo["data"];
            }
            catch (Exception)
            {
                throw;
            }
        }
        //回复的回复
        public static async Task<JArray> GetReplyListById(string feedId, string page, string lastItem)
        {
            try
            {
                string result = await GetCoolApkMessage($"/feed/replyList?id={feedId}&listType=&page={page}&lastItem={lastItem}&discussMode=0&feedType=feed_reply&blockStatus=0&fromFeedAuthor=0");
                JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                return (JArray)jo["data"];
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<JArray> GetFeedLikeUsersListById(string feedId, string page, string firstItem, string lastItem)
        {
            try
            {
                string result = await GetCoolApkMessage($"/feed/likeList?id={feedId}&listType=lastupdate_desc&page={page}&firstItem={firstItem}&lastItem={lastItem}");
                JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                return (JArray)jo["data"];
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static async Task<JArray> GetForwardListById(string feedId, string page)
        {
            try
            {
                string result = await GetCoolApkMessage($"/feed/forwardList?id={feedId}&type=feed&page={page}");
                JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                return (JArray)jo["data"];
            }
            catch (Exception)
            {
                throw;
            }
        }

        /*
        public static async Task<string> GetCoolApkUserFaceUri(string NameOrID)
        {
            String body = await Web.GetHttp("https://www.coolapk.com/u/" + NameOrID);
            body = Regex.Split(body, @"<div class=""msg_box"">")[1];
            body = Regex.Split(body, @"src=""")[1];
            return Regex.Split(body, @"""")[0];
        }
        */
    }
}