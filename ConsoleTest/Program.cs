using System;
using NewLife;
using NewLife.RocketMQ;
using NewLife.RocketMQ.Protocol;

namespace ConsoleTest
{
    class Program
    {
        static string[] MsgTags = new[] { "order_create", "order_commit", "order_refund" };

        static void Main(string[] args)
        {
            using var producerFactory = new ProducerFactory("172.17.20.6:9876");

            using var consumerContainer = new ConsumerContainer("172.17.20.6:9876");

            TestConsumeMsg(consumerContainer, MsgTags[0]);
            TestPublishMsg(producerFactory);

            Console.ReadKey();
        }

        static void TestPublishMsg(ProducerFactory producerFactory)
        {
            var producer = producerFactory.GetTopicProducer("delay_msg_test");

            for (var i = 0; i < 10; i++)
            {
                var str = $"order_test_{DateTime.UtcNow.Ticks}";

                var msg = new Message { Body = str.GetBytes(), Tags = MsgTags[i % 3], DelayTimeLevel = 2 };
                var sr = producer.Publish(msg);

                Console.WriteLine($"发送消息: {str}, Tag: {msg.Tags}, 发送结果: {sr.Status}, 队列: {sr.Queue}");
            }
        }

        static void TestConsumeMsg(ConsumerContainer consumerContainer, params string[] msgTags)
        {
            consumerContainer.AddTopicConsumer("delay_order_create", "delay_msg_test", (q, ms) =>
            {
                var receiveTime = DateTime.Now;
                Console.WriteLine($"队列: {q}收到消息{ms.Length}条");

                foreach (var msg in ms)
                {
                    Console.WriteLine($"接收消息: {msg.Body.ToStr()}, Tags: {msg.Tags}, 产生时间: {msg.BornTimestamp.ToDateTime()}, 接收时间: {receiveTime}");
                }
                return true;
            },  msgTags);
        }
    }
}
