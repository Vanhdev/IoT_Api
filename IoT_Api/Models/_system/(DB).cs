using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using BsonData;

namespace System
{
    public static class DataExtension
    {
        public static string GetDataKey(this string s)
        {
            return s.ToUpper();
        }
    }
    public partial class DB
    {
        public const int DeleteAll = -2;
        public const int Delete = -1;
        public const int Update = 0;
        public const int Insert = 1;

        static public Database Main { get; set; }
        static public Collection GetCollection<T>() 
        { 
            return AsyncGetCollection<T>(0); 
        }
        static public Database Register(string path)
        {
            Main = new Database("MainDB")
                .Connect(path)
                .StartStorageThread();
            return Main;
        }
        static public Collection AsyncGetCollection<T>(int wait)
        {
            if (wait != 0)
            {
                System.Threading.Thread.Sleep(wait);
            }
            var data = Main.GetCollection(typeof(T).Name);
            if (wait == 0)
            {
                data.Wait(null);
            }
            return data;
        }
    }
}