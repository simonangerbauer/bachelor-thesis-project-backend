﻿using System;
using System.Threading;
using Data;
using Newtonsoft.Json.Linq;
using Primitives;
using Service;

namespace PubSubServer
{
    public class QueueWorker
    {
        private readonly TaskService _taskService;

        public QueueWorker(TaskService taskService)
        {
            _taskService = taskService;
        }

        public void Start()
        {
            System.Threading.Tasks.Task.Factory.StartNew(WorkQueueAsync);
        }

        private async System.Threading.Tasks.Task WorkQueueAsync()
        {
            while(true)
            {
                (var entity, var socketState) = Queue.Dequeue();
                if (entity is Task)
                {
                    (var state, var data) = await _taskService.PostAsync<Task>((Task)entity);
                    JObject json =
                        new JObject(
                            new JProperty(JsonTokens.State, state),
                            new JProperty(JsonTokens.Data, JObject.FromObject(data)));
                    switch (state)
                    {
                        case StateEnum.Unchanged:
                        {
                            PublisherService.ReplyToSender(json.ToString(), socketState);
                            break;
                        }
                        case StateEnum.Modified:
                        {
                            PublisherService.Publish(json.ToString(), typeof(Task).FullName);
                            break;
                        }
                    }

                }
            }
        }
    }
}