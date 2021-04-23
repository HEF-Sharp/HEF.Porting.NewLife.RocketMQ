using System;
using System.Collections.Concurrent;
using NewLife.RocketMQ.Protocol;

namespace NewLife.RocketMQ
{
    /// <summary>
    /// 消费者Container
    /// </summary>
    public class ConsumerContainer : IDisposable
    {
        private readonly string _nameServerAddress;

        private readonly BlockingCollection<Consumer> _consumerCache = new();

        /// <summary>
        /// Constructor
        /// </summary>
        public ConsumerContainer(string nameServerAddress)
        {
            if (nameServerAddress.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(nameServerAddress));

            _nameServerAddress = nameServerAddress;
        }

        /// <summary>
        /// 添加Topic消费者
        /// </summary>
        public void AddTopicConsumer(string group, string topic,
            Func<MessageQueue, MessageExt[], Boolean> consumeFunction, params string[] tags)
        {
            AddTopicConsumer(group, topic, null, consumeFunction, tags);
        }

        /// <summary>
        /// 添加Topic消费者
        /// </summary>
        public void AddTopicConsumer(string group, string topic, Action<Consumer> consumerConfigure,
            Func<MessageQueue, MessageExt[], Boolean> consumeFunction, params string[] tags)
        {
            if (group.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(group));

            if (topic.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(topic));

            if (consumeFunction == null)
                throw new ArgumentNullException(nameof(consumeFunction));

            var consumer = new Consumer
            {
                Group = group,
                NameServerAddress = _nameServerAddress,

                FromLastOffset = true,
                SkipOverStoredMsgCount = 0
            };
            consumer.Passively().Subscribe(topic, tags);

            consumerConfigure?.Invoke(consumer);
            consumer.OnConsume = consumeFunction;

            consumer.Start();

            _consumerCache.Add(consumer);
        }

        /// <summary>
        /// IDisposable
        /// </summary>
        public void Dispose()
        {
            _consumerCache.CompleteAdding();

            while(_consumerCache.TryTake(out var consumer))
            {
                consumer.Dispose();
            }

            _consumerCache.Dispose();
        }
    }
}
