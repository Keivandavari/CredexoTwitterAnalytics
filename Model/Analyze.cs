//********************************************************* 
// 
//    Copyright (c) Microsoft. All rights reserved. 
//    This code is licensed under the Microsoft Public License. 
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF 
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY 
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR 
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT. 
// 
//*********************************************************

using Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Model
{
    public static class Sentiment
    {
        public static TwitterPayload ComputeScore(Tweet tweet, string twitterKeywords)
        {

            return new TwitterPayload
            {
                ID = tweet.Id,
                CreatedAt = ParseTwitterDateTime(tweet.CreatedAt),
                UserName = tweet.User != null ? tweet.User.Name : null,
                TimeZone = tweet.User != null ? (tweet.User.TimeZone != null ? tweet.User.TimeZone : "(unknown)") : "(unknown)",
                ProfileImageUrl = tweet.User != null ? (tweet.User.ProfileImageUrl != null ? tweet.User.ProfileImageUrl : "(unknown)") : "(unknown)",
                Text = tweet.Text,
                Language = tweet.Language != null ? tweet.Language : "(unknown)",
                RawJson = tweet.RawJson,
                SentimentScore = AnalyzeText(tweet.Text).Result,
                Topic = DetermineTopc(tweet.Text, twitterKeywords),
            };
        }
        static DateTime ParseTwitterDateTime(string p)
        {
            if (p == null)
                return DateTime.Now;
            p = p.Replace("+0000 ", "");
            DateTimeOffset result;

            if (DateTimeOffset.TryParseExact(p, "ddd MMM dd HH:mm:ss yyyy", CultureInfo.GetCultureInfo("en-us").DateTimeFormat, DateTimeStyles.AssumeUniversal, out result))
                return result.DateTime;
            else
                return DateTime.Now;
        }
        static async Task<string> AnalyzeText(string textToAnalyze)
        {
            var sentiment = await InvokeRequestResponseService(textToAnalyze);
            return sentiment;
        }
        static async Task<string> InvokeRequestResponseService(string textToAnalyze)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {

                    Inputs = new Dictionary<string, StringTable>() { 
                        { 
                            "input1", 
                            new StringTable() 
                            {
                                ColumnNames = new string[] {"sentiment_label", "tweet_text"},
                                Values = new string[,] {  { "0", textToAnalyze } }
                            }
                        },
                                        },
                    GlobalParameters = new Dictionary<string, string>()
                    {

                    }
                };
                const string apiKey = "tZx29MJUi9rBspBb7pTmaYlE6HC4x5sWAAD/V9hpFiZUItAoEu0EJbqv50p1WtGi3s/g5KJzBvkfzFpjJUxRQ=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/761ef8d9ce194d1abc0798d50ae771d1/services/a82c5926d3394da9b6e62448758a51fb/execute?api-version=2.0&details=true");

                // WARNING: The 'await' statement below can result in a deadlock if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false) so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)


                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    dynamic jsonResult = JObject.Parse(result);
                    return jsonResult.Results.output1.value.Values[0][0];
                }
                else
                {
                   return string.Format("The request failed with status code: {0}", response.StatusCode);

                    //// Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    //Console.WriteLine(response.Headers.ToString());

                    //string responseContent = await response.Content.ReadAsStringAsync();
                    //Console.WriteLine(responseContent);
                }
            }
        }
        static string DetermineTopc(string tweetText, string keywordFilters)
        {
            if (string.IsNullOrEmpty(tweetText))
                return string.Empty;

            string subject = string.Empty;

            //keyPhrases are specified in app.config separated by commas.  Can have no leading or trailing spaces.  Example of key phrases in app.config
            //	<add key="twitter_keywords" value="Microsoft, Office, Surface,Windows Phone,Windows 8,Windows Server,SQL Server,SharePoint,Bing,Skype,XBox,System Center"/><!--comma to spit multiple keywords-->
            string[] keyPhrases = keywordFilters.Split(',');

            foreach (string keyPhrase in keyPhrases)
            {
                subject = keyPhrase;

                //a key phrase may have multiple key words, like: Windows Phone.  If this is the case we will only assign it a subject if both words are 
                //included and in the correct order. For example, a tweet will match if "Windows 8" is found within the tweet but will not match if
                // the tweet is "There were 8 broken Windows".  This is not case sensitive

                //Creates one array that breaks the tweet into individual words and one array that breaks the key phrase into individual words.  Within 
                //This for loop another array is created from the tweet that includes the same number of words as the keyphrase.  These are compared.  For example,
                // KeyPhrase = "Microsoft Office" Tweet= "I Love Microsoft Office"  "Microsoft Office" will be compared to "I Love" then "Love Microsoft" and 
                //Finally "Microsoft Office" which will be returned as the subject.  if no match is found "Do Not Include" is returned. 
                string[] KeyChunk = keyPhrase.Trim().Split(' ');
                string[] tweetTextChunk = tweetText.Split(' ');
                string Y;
                for (int i = 0; i <= (tweetTextChunk.Length - KeyChunk.Length); i++)
                {
                    Y = null;
                    for (int j = 0; j <= (KeyChunk.Length - 1); j++)
                    {
                        Y += tweetTextChunk[(i + j)] + " ";
                    }
                    if (Y != null) Y = Y.Trim();
                    if (Y.ToUpper().Contains(keyPhrase.ToUpper()))
                    {
                        return subject;
                    }
                }
            }

            return "Unknown";
        }
    }
    public class StringTable
    {
        public string[] ColumnNames { get; set; }
        public string[,] Values { get; set; }
    }
}
