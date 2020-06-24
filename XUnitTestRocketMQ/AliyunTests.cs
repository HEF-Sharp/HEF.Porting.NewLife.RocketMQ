using NewLife.Log;
using NewLife.RocketMQ;
using NewLife.RocketMQ.Client;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace XUnitTestRocketMQ
{
    public class AliyunTests
    {
        private static void SetConfig(MqBase mq)
        {
            mq.Server = "http://onsaddr-internet.aliyun.com/rocketmq/nsaddr4client-internet";
            mq.Configure(MqSetting.Current);

            mq.Log = XTrace.Log;
        }

        [Fact]
        public void CreateTopic()
        {
            var mq = new Producer
            {
                //Topic = "nx_test",
            };
            SetConfig(mq);

            mq.Start();

            // ����topicʱ��startǰ����ָ��topic������ʹ��Ĭ��TBW102
            Assert.Equal("TBW102", mq.Topic);

            mq.CreateTopic("nx_test", 2);
        }

        [Fact]
        static void ProduceTest()
        {
            using var mq = new Producer
            {
                Topic = "test1",
            };
            SetConfig(mq);

            mq.Start();

            for (var i = 0; i < 10; i++)
            {
                var str = "ѧ���Ⱥ����Ϊʦ" + i;
                //var str = Rand.NextString(1337);

                var sr = mq.Publish(str, "TagA");
            }
        }

        [Fact]
        static void ConsumeTest()
        {
            var mq = new Consumer
            {
                Topic = "test1",
                Group = "test",

                FromLastOffset = true,
                SkipOverStoredMsgCount = 0,
                BatchSize = 20,
            };
            SetConfig(mq);

            mq.OnConsume = (q, ms) =>
            {
                XTrace.WriteLine("[{0}@{1}]�յ���Ϣ[{2}]", q.BrokerName, q.QueueId, ms.Length);

                foreach (var item in ms.ToList())
                {
                    XTrace.WriteLine($"��Ϣ��������{item.Keys}��������ʱ�䡾{item.BornTimestamp.ToDateTime()}�������ݡ�{item.Body.ToStr()}��");
                }

                return true;
            };

            mq.Start();

            Thread.Sleep(3000);
        }
    }
}