using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Hadoop.Avro;
using MessagePack;
using MsgPack.Serialization;

namespace Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(1000);
            Console.WriteLine("Benchmarking...");
            RunMsgPack();
            RunLz4MessagePack();
            RunMessagePack();
            RunMessagePack();
            RunMsgPack();
            RunLz4MessagePack();

            Console.ReadLine();
        }
        public static void RunLz4MessagePack()
        {
            var message = new Message();
            var sw = Stopwatch.StartNew();
            var memStream3 = new MemoryStream();
            //MessagePack.MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            MessagePack.MessagePackSerializer.Serialize(memStream3, message);
            Console.WriteLine("Message Size: " + memStream3.Length);
            memStream3.Position = 0;
            var obj1 = MessagePack.MessagePackSerializer.Deserialize<Message>(memStream3);
            //Console.WriteLine(obj1.Title);
            Console.WriteLine("Elapsed: " + sw.ElapsedTicks);
        }

        public static void RunMsgPack()
        {
            var message = new Message();
            var sw = Stopwatch.StartNew();
            var memStream = new MemoryStream();
            var serializer = SerializationContext.Default.GetSerializer<Message>();
            serializer.Pack(memStream, message);
            Console.WriteLine("Message Size: " + memStream.Length);
            memStream.Position = 0;
            var unpackedObject = serializer.Unpack(memStream);
            Console.WriteLine("Elapsed: " + sw.ElapsedTicks);
        }

        public static void RunMessagePack()
        {
            var message = new Message();
            var sw = Stopwatch.StartNew();
            var memStream2 = new MemoryStream();
            //MessagePack.MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            MessagePack.MessagePackSerializer.Serialize(memStream2, message);
            Console.WriteLine("Message Size: " + memStream2.Length);
            memStream2.Position = 0;
            var obj = MessagePack.MessagePackSerializer.Deserialize<Message>(memStream2);
            Console.WriteLine("Elapsed: " + sw.ElapsedTicks);
        }

    }
}
