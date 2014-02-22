﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KafkaNet;
using KafkaNet.Common;
using KafkaNet.Model;
using NUnit.Framework;

namespace kafka_tests.Unit
{
    [TestFixture]
    public class DefaultPartitionSelectorTests
    {
        private List<Partition> _twoPartitions;

        [SetUp]
        public void Setup()
        {
            _twoPartitions = new List<Partition>(new[]
                {
                    new Partition
                        {
                            LeaderId = 0,
                            PartitionId = 0
                        },
                    new Partition
                        {
                            LeaderId = 1,
                            PartitionId = 1
                        }
                });
        }

        [Test]
        public void RoundRobinShouldRollOver()
        {
            var selector = new DefaultPartitionSelector();

            var first = selector.Select("test", null, _twoPartitions);
            var second = selector.Select("test", null, _twoPartitions);
            var third = selector.Select("test", null, _twoPartitions);

            Assert.That(first.PartitionId, Is.EqualTo(0));
            Assert.That(second.PartitionId, Is.EqualTo(1));
            Assert.That(third.PartitionId, Is.EqualTo(0));
        }

        [Test]
        public void RoundRobinShouldHandleMultiThreadedRollOver()
        {
            var selector = new DefaultPartitionSelector();
            var bag = new ConcurrentBag<Partition>();

            Parallel.For(0, 100, x => bag.Add(selector.Select("test", null, _twoPartitions)));

            Assert.That(bag.Count(x => x.PartitionId == 0), Is.EqualTo(50));
            Assert.That(bag.Count(x => x.PartitionId == 1), Is.EqualTo(50));
        }

        [Test]
        public void RoundRobinShouldTrackEachTopicSeparately()
        {
            var selector = new DefaultPartitionSelector();

            var a1 = selector.Select("a", null, _twoPartitions);
            var b1 = selector.Select("b", null, _twoPartitions);
            var a2 = selector.Select("a", null, _twoPartitions);
            var b2 = selector.Select("b", null, _twoPartitions);

            Assert.That(a1.PartitionId, Is.EqualTo(0));
            Assert.That(a2.PartitionId, Is.EqualTo(1));

            Assert.That(b1.PartitionId, Is.EqualTo(0));
            Assert.That(b2.PartitionId, Is.EqualTo(1));
        }

        [Test]
        public void KeyHashShouldSelectEachPartitionType()
        {
            var selector = new DefaultPartitionSelector();

            var first = selector.Select("test", "0", _twoPartitions);
            var second = selector.Select("test", "1", _twoPartitions);

            Assert.That(first.PartitionId, Is.EqualTo(0));
            Assert.That(second.PartitionId, Is.EqualTo(1));
        }

        [Test]
        [ExpectedException(typeof(InvalidPartitionIdSelectedException))]
        public void KeyHashShouldThrowExceptionWhenChoosesAPartitionIdThatDoesNotExist()
        {
            var selector = new DefaultPartitionSelector();
            var list = new List<Partition>(_twoPartitions);
            list[1].PartitionId = 999;
            
            selector.Select("test", "1", _twoPartitions);
        }
    }
}
