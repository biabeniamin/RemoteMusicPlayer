using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MusicAlarm
{
    public static class Helpers
    {
        public static async Task<string> ReadAFileOnServer(string path)
        {
            string text = "";
            HttpClient client = new HttpClient();
            try
            {
                text = await client.GetStringAsync(path);
            }
            catch(Exception ee)
            {
                System.Diagnostics.Debug.WriteLine(ee.Message);
            }
            return text;
        }
        public static TimeSpan GetNextDay(int hour, int minute, int second)
        {
            return DateTime.Now.AddSeconds(10)-DateTime.Now;
            DateTime today = DateTime.Now;
            if (hour >= today.Hour)
            {
                if (hour == today.Hour)
                {
                    if (minute <= today.Minute)
                    {
                        today = today.AddDays(1);
                    }
                }
            }
            else
            {
                today = today.AddDays(1);
            }
            DateTime tommorow = new DateTime(today.Year, today.Month, today.Day, hour, minute, second);
            TimeSpan interval = tommorow - DateTime.Now;
            return interval;
        }
        public static TimeSpan GetNextDay()
        {
            return TimeSpan.FromDays(1);
        }
        public static char[] ConvertStringToCharArray(string text)
        {
            char[] array = new char[text.Length];
            for (int i = 0; i < text.Length; ++i)
                array[i] = text[i];
            return array;
        }
        public static List<string> Split(string text,string separator)
        {
            List<string> items = new List<string>();
            for(int i=0;i<=text.Length-separator.Length;++i)
            {
                if(text.Substring(i,separator.Length)==separator)
                {
                    items.Add(text.Substring(0, i));
                    text = text.Substring(i + separator.Length);
                    i = -1;
                }
            }
            return items;
        }
    }
}
