﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System.Configuration;


namespace Model
{
    class EventHubDataSender : IObserver<Payload>
    {
        private EventHubConfig _config;
        private EventHubClient _eventHubClient;


        public EventHubDataSender(EventHubConfig config)
        {
            try
            {
                _config = config;
                _eventHubClient = EventHubClient.CreateFromConnectionString(_config.ConnectionString, config.EventHubName);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        public void OnNext(Payload TwitterPayloadData)
        {
            try
            {

                var serialisedString = JsonConvert.SerializeObject(TwitterPayloadData);
                EventData data = new EventData(Encoding.UTF8.GetBytes(serialisedString)) { PartitionKey = TwitterPayloadData.Topic };
                _eventHubClient.Send(data);


                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Sending" + serialisedString + " at: " + TwitterPayloadData.CreatedAt.ToString());

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }

        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {

        }

    }
}
