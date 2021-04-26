using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NewLife.RocketMQ
{
    /// <summary>
    /// 生产者factory
    /// </summary>
    public class ProducerFactory : IDisposable
    {
        internal const int defaultTopicQueueNum = 4;

        private readonly Producer _masterProducer;
        private readonly int _topicQueueNum = 0;

        private readonly ConcurrentDictionary<string, Producer> _topicProducerCache = new();

        /// <summary>
        /// Constructor
        /// </summary>
        public ProducerFactory(string nameServerAddress, int topicQueueNum = 0)
        {
            if (nameServerAddress.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(nameServerAddress));

            _topicQueueNum = topicQueueNum < 1 ? defaultTopicQueueNum : topicQueueNum;

            _masterProducer = new Producer { NameServerAddress = nameServerAddress };
            _masterProducer.Start();
        }

        /// <summary>
        /// 获取Topic对应Producer
        /// </summary>        
        public Producer GetTopicProducer(string topic, Action<Producer> producerConfigure = null)
        {
            if (topic.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(topic));

            return _topicProducerCache.GetOrAdd(topic, (key) => CreateTopicProducer(key, producerConfigure));
        }

        private Producer CreateTopicProducer(string topic, Action<Producer> producerConfigure)
        {
            _masterProducer.CreateTopic(topic, _topicQueueNum);

            var topicProducer = new Producer
            {
                Topic = topic,
                NameServerAddress = _masterProducer.NameServerAddress
            };

            producerConfigure?.Invoke(topicProducer);

            topicProducer.Start();

            return topicProducer;
        }

        /// <summary>
        /// IDisposable
        /// </summary>
        public void Dispose()
        {
            _masterProducer?.Dispose();

            var topicProducers = _topicProducerCache.Select(m => m.Value).ToArray();
            _topicProducerCache.Clear();

            foreach (var topicProducer in topicProducers)
            {
                topicProducer.Dispose();
            }
        }
    }
}
