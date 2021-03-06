﻿using LiteNetLib.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LibSample
{
    class SerializerBenchmark
    {
        [Serializable] //Just for test binary formatter
        private struct SampleNetSerializable : INetSerializable
        {
            public int Value;

            public void Serialize(NetDataWriter writer)
            {
                writer.Put(Value);
            }

            public void Deserialize(NetDataReader reader)
            {
                Value = reader.GetInt();
            }
        }

        [Serializable] //Just for test binary formatter
        private class SamplePacket
        {
            public string SomeString { get; set; }
            public float SomeFloat { get; set; }
            public int[] SomeIntArray { get; set; }
            public SomeVector2 SomeVector2 { get; set; }
            public SomeVector2[] SomeVectors { get; set; }
            public string EmptyString { get; set; }
            public SampleNetSerializable TestObj { get; set; }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("SomeString: " + SomeString);
                sb.AppendLine("SomeFloat: " + SomeFloat);
                sb.AppendLine("SomeIntArray: ");
                for (int i = 0; i < SomeIntArray.Length; i++)
                {
                    sb.AppendLine(" " + SomeIntArray[i]);
                }
                sb.AppendLine("SomeVector2 X: " + SomeVector2);
                sb.AppendLine("SomeVectors: ");
                for (int i = 0; i < SomeVectors.Length; i++)
                {
                    sb.AppendLine(" " + SomeVectors[i]);
                }
                sb.AppendLine("EmptyString: " + EmptyString);
                sb.AppendLine("TestObj value: " + TestObj.Value);
                return sb.ToString();
            }
        }

        [Serializable] //Just for test binary formatter
        private struct SomeVector2
        {
            public int X;
            public int Y;

            public SomeVector2(int x, int y)
            {
                X = x;
                Y = y;
            }

            public override string ToString()
            {
                return "X: " + X + ", Y: " + Y;
            }

            public static void Serialize(NetDataWriter writer, SomeVector2 vector)
            {
                writer.Put(vector.X);
                writer.Put(vector.Y);
            }

            public static SomeVector2 Deserialize(NetDataReader reader)
            {
                SomeVector2 res = new SomeVector2();
                res.X = reader.GetInt();
                res.Y = reader.GetInt();
                return res;
            }
        }

        public void Run()
        {
            Console.WriteLine("=== Serializer benchmark ===");
            
            const int LoopLength = 100000;
            //Test serializer performance
            Stopwatch stopwatch = new Stopwatch();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();
            NetDataWriter netDataWriter = new NetDataWriter();

            SamplePacket samplePacket = new SamplePacket
            {
                SomeFloat = 0.3f,
                SomeString = "TEST",
                SomeIntArray = new [] { 1, 2, 3 },
                SomeVector2 = new SomeVector2(1, 2),
                SomeVectors = new [] { new SomeVector2(3,4), new SomeVector2(5,6) }
            };

            NetSerializer netSerializer = new NetSerializer();
            netSerializer.RegisterNestedType<SampleNetSerializable>();
            netSerializer.RegisterNestedType( SomeVector2.Serialize, SomeVector2.Deserialize );

            //Prewarm cpu
            for (int i = 0; i < 10000000; i++)
            {
                double c = Math.Sin(i);
            }

            //Test binary formatter
            stopwatch.Start();
            for (int i = 0; i < LoopLength; i++)
            {
                binaryFormatter.Serialize(memoryStream, samplePacket);
            }
            stopwatch.Stop();
            Console.WriteLine("BinaryFormatter time: " + stopwatch.ElapsedMilliseconds + " ms");

            //Test NetSerializer
            stopwatch.Restart();
            for (int i = 0; i < LoopLength; i++)
            {
                netSerializer.Serialize(netDataWriter, samplePacket);
            }
            stopwatch.Stop();
            Console.WriteLine("NetSerializer first run time: " + stopwatch.ElapsedMilliseconds + " ms");

            //Test NetSerializer
            netDataWriter.Reset();
            stopwatch.Restart();
            for (int i = 0; i < LoopLength; i++)
            {
                netSerializer.Serialize(netDataWriter, samplePacket);
            }
            stopwatch.Stop();
            Console.WriteLine("NetSerializer second run time: " + stopwatch.ElapsedMilliseconds + " ms");

            //Test RAW
            netDataWriter.Reset();
            stopwatch.Restart();
            for (int i = 0; i < LoopLength; i++)
            {
                netDataWriter.Put(samplePacket.SomeFloat);
                netDataWriter.Put(samplePacket.SomeString);
                netDataWriter.PutArray(samplePacket.SomeIntArray);
                netDataWriter.Put(samplePacket.SomeVector2.X);
                netDataWriter.Put(samplePacket.SomeVector2.Y);
                netDataWriter.Put(samplePacket.SomeVectors.Length);
                for (int j = 0; j < samplePacket.SomeVectors.Length; j++)
                {
                    netDataWriter.Put(samplePacket.SomeVectors[j].X);
                    netDataWriter.Put(samplePacket.SomeVectors[j].Y);
                }
                netDataWriter.Put(samplePacket.EmptyString);
                netDataWriter.Put(samplePacket.TestObj.Value);
            }
            stopwatch.Stop();
            Console.WriteLine("DataWriter (raw put method calls) time: " + stopwatch.ElapsedMilliseconds + " ms");
        }
    }
}
