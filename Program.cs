﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Model;

namespace CredexoTwitterAnalytics
{
    class Program
    {
        static void Main(string[] args)
        {
            //Configure Twitter OAuth
            var oauthToken = ConfigurationManager.AppSettings["oauth_token"];
            var oauthTokenSecret = ConfigurationManager.AppSettings["oauth_token_secret"];
            var oauthCustomerKey = ConfigurationManager.AppSettings["oauth_consumer_key"];
            var oauthConsumerSecret = ConfigurationManager.AppSettings["oauth_consumer_secret"];
            var keywords = ConfigurationManager.AppSettings["twitter_keywords"];

            //Configure EventHub
            var config = new EventHubConfig();
            config.ConnectionString = ConfigurationManager.AppSettings["EventHubConnectionString"];
            config.EventHubName = ConfigurationManager.AppSettings["EventHubName"];
            var myEventHubDataSender = new EventHubDataSender(config);

            var tweeterData = Tweet.StreamStatuses(new TwitterConfig(oauthToken, oauthTokenSecret, oauthCustomerKey, oauthConsumerSecret,
                keywords)).Select(tweet => Sentiment.ComputeScore(tweet, keywords)).Select(tweet => new Payload { CreatedAt = tweet.CreatedAt, Text = tweet.Text, Topic = tweet.Topic, SentimentScore = tweet.SentimentScore });

            tweeterData.ToObservable().Subscribe(myEventHubDataSender);
        }
    }
}
