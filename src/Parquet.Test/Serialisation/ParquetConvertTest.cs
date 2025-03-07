﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetBox.Extensions;
using Parquet.Attributes;
using Parquet.Data;
using Parquet.Serialization;
using Xunit;

namespace Parquet.Test.Serialisation
{
   public class ParquetConvertTest : TestBase
   {
      [Fact]
      public void Serialise_Should_Exclude_IgnoredProperties_while_serialized_to_parquetfile()
      {
         DateTime now = DateTime.Now;

         IEnumerable<StructureWithIgnoredProperties> structures = Enumerable
            .Range(0, 10)
            .Select(i => new StructureWithIgnoredProperties
            {
               Id = i,
               Name = $"row {i}",
               SSN = "000-00-0000",
               NonNullableDecimal = 100.534M,
               NullableDecimal = 99.99M,
               NonNullableDateTime = DateTime.Now,
               NullableDateTime = DateTime.Now,
               NullableInt = 111,
               NonNullableInt = 222
            }) ;

         using (var ms = new MemoryStream())
         {
            Schema schema = ParquetConvert.Serialize(structures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2);

            ms.Position = 0;

            StructureWithIgnoredProperties[] structures2 = ParquetConvert.Deserialize<StructureWithIgnoredProperties>(ms);

            StructureWithIgnoredProperties[] structuresArray = structures.ToArray();
            Func<Type, Object> GetDefaultValue = (type) => type.IsValueType ? Activator.CreateInstance(type) : null;

            for (int i = 0; i < 10; i++)
            {
               Assert.Equal(structuresArray[i].Id, structures2[i].Id);
               Assert.Equal(structuresArray[i].Name, structures2[i].Name);
               //As serialization ignored these below properties, deserilizing these should always be null(or type's default value).
               Assert.Equal(structures2[i].SSN, GetDefaultValue(typeof(string)));
               Assert.Equal(structures2[i].NonNullableInt, GetDefaultValue(typeof(int)));
               Assert.Equal(structures2[i].NullableInt, GetDefaultValue(typeof(int?)));
               Assert.Equal(structures2[i].NonNullableDecimal, GetDefaultValue(typeof(decimal)));
               Assert.Equal(structures2[i].NullableDecimal, GetDefaultValue(typeof(decimal?)));
               Assert.Equal(structures2[i].NonNullableDateTime, GetDefaultValue(typeof(DateTime)));
               Assert.Equal(structures2[i].NullableDateTime, GetDefaultValue(typeof(DateTime?)));
            }

         }
      }

      [Fact]
      public void Serialise_deserialise_all_types()
      {
         DateTime now = DateTime.Now;

         IEnumerable<SimpleStructure> structures = Enumerable
            .Range(0, 10)
            .Select(i => new SimpleStructure
            {
               Id = i,
               NullableId = (i % 2 == 0) ? new int?() : new int?(i),
               Name = $"row {i}",
               Date = now.AddDays(i).RoundToSecond().ToUniversalTime()
            });

         using (var ms = new MemoryStream())
         {
            Schema schema = ParquetConvert.Serialize(structures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2);

            ms.Position = 0;

            SimpleStructure[] structures2 = ParquetConvert.Deserialize<SimpleStructure>(ms);

            SimpleStructure[] structuresArray = structures.ToArray();
            for (int i = 0; i < 10; i++)
            {
               Assert.Equal(structuresArray[i].Id, structures2[i].Id);
               Assert.Equal(structuresArray[i].NullableId, structures2[i].NullableId);
               Assert.Equal(structuresArray[i].Name, structures2[i].Name);
               Assert.Equal(structuresArray[i].Date, structures2[i].Date);
            }
         }
      }

      [Fact]
      public void Serialize_append_deserialise()
      {
         DateTime now = DateTime.Now;

         IEnumerable<SimpleStructure> structures = Enumerable
            .Range(0, 5)
            .Select(i => new SimpleStructure
            {
               Id = i,
               NullableId = (i % 2 == 0) ? new int?() : new int?(i),
               Name = $"row {i}",
               Date = now.AddDays(i).RoundToSecond().ToUniversalTime()
            });

         IEnumerable<SimpleStructure> appendStructures = Enumerable
            .Range(5, 5)
            .Select(i => new SimpleStructure
            {
               Id = i,
               NullableId = (i % 2 == 0) ? new int?() : new int?(i),
               Name = $"row {i}",
               Date = now.AddDays(i).RoundToSecond().ToUniversalTime()
            });

         using (var ms = new MemoryStream())
         {
            ParquetConvert.Serialize(structures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2);

            ParquetConvert.Serialize(appendStructures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2, append: true);

            ms.Position = 0;

            SimpleStructure[] structures2 = ParquetConvert.Deserialize<SimpleStructure>(ms);

            SimpleStructure[] structuresArray = structures.Concat(appendStructures).ToArray();

            Assert.Equal(structuresArray.Length, structures2.Length);
            for (int i = 0; i < structuresArray.Length; i++)
            {
               Assert.Equal(structuresArray[i].Id, structures2[i].Id);
               Assert.Equal(structuresArray[i].NullableId, structures2[i].NullableId);
               Assert.Equal(structuresArray[i].Name, structures2[i].Name);
               Assert.Equal(structuresArray[i].Date, structures2[i].Date);
            }
         }
      }

      [Fact]
      public void Serialise_deserialise_renamed_column()
      {
         IEnumerable<SimpleRenamed> structures = Enumerable
            .Range(0, 10)
            .Select(i => new SimpleRenamed
            {
               Id = i,
               PersonName = $"row {i}"
            });

         using (var ms = new MemoryStream())
         {
            Schema schema = ParquetConvert.Serialize(structures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2);

            ms.Position = 0;

            SimpleRenamed[] structures2 = ParquetConvert.Deserialize<SimpleRenamed>(ms);

            SimpleRenamed[] structuresArray = structures.ToArray();
            for (int i = 0; i < 10; i++)
            {
               Assert.Equal(structuresArray[i].Id, structures2[i].Id);
               Assert.Equal(structuresArray[i].PersonName, structures2[i].PersonName);
            }
         }
      }
      [Fact]
      public void Serialise_all_but_deserialise_only_few_properties()
      {
         DateTime now = DateTime.Now;

         IEnumerable<SimpleStructure> structures = Enumerable
            .Range(0, 10)
            .Select(i => new SimpleStructure
            {
               Id = i,
               NullableId = (i % 2 == 0) ? new int?() : new int?(i),
               Name = $"row {i}",
               Date = now.AddDays(i).RoundToSecond().ToUniversalTime()
            });

         using (var ms = new MemoryStream())
         {
            Schema schema = ParquetConvert.Serialize(structures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2);

            ms.Position = 0;

            SimpleStructure[] structuresArray = structures.ToArray();
            int rowGroupCount = 5; //based on our test input. 10 records with rowgroup size 2.
            for (int r = 0; r < rowGroupCount; r++)
            {
               SimpleStructureWithFewProperties[] rowGroupRecords = ParquetConvert.Deserialize<SimpleStructureWithFewProperties>(ms, rowGroupIndex: r);
               Assert.Equal(2, rowGroupRecords.Length);

               Assert.Equal(structuresArray[2 * r].Id, rowGroupRecords[0].Id);
               Assert.Equal(structuresArray[2 * r].Name, rowGroupRecords[0].Name);
               Assert.Equal(structuresArray[2 * r + 1].Id, rowGroupRecords[1].Id);
               Assert.Equal(structuresArray[2 * r + 1].Name, rowGroupRecords[1].Name);

            }
            Assert.Throws<ArgumentOutOfRangeException>("index", () => ParquetConvert.Deserialize<SimpleStructure>(ms, 5));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => ParquetConvert.Deserialize<SimpleStructure>(ms, 99999));
         }
      }
      [Fact]
      public void Serialise_read_and_deserialise_by_rowgroup()
      {
         DateTime now = DateTime.Now;

         IEnumerable<SimpleStructure> structures = Enumerable
            .Range(0, 10)
            .Select(i => new SimpleStructure
            {
               Id = i,
               NullableId = (i % 2 == 0) ? new int?() : new int?(i),
               Name = $"row {i}",
               Date = now.AddDays(i).RoundToSecond().ToUniversalTime()
            });

         using (var ms = new MemoryStream())
         {
            Schema schema = ParquetConvert.Serialize(structures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2);

            ms.Position = 0;

            SimpleStructure[] structuresArray = structures.ToArray();
            int rowGroupCount = 5; //based on our test input. 10 records with rowgroup size 2.
            for(int r = 0; r < rowGroupCount; r++)
            {
               SimpleStructure[] rowGroupRecords = ParquetConvert.Deserialize<SimpleStructure>(ms, rowGroupIndex: r);
               Assert.Equal(2, rowGroupRecords.Length);

               Assert.Equal(structuresArray[2*r].Id, rowGroupRecords[0].Id);
               Assert.Equal(structuresArray[2*r].NullableId, rowGroupRecords[0].NullableId);
               Assert.Equal(structuresArray[2*r].Name, rowGroupRecords[0].Name);
               Assert.Equal(structuresArray[2*r].Date, rowGroupRecords[0].Date);
               Assert.Equal(structuresArray[2*r+1].Id, rowGroupRecords[1].Id);
               Assert.Equal(structuresArray[2*r+1].NullableId, rowGroupRecords[1].NullableId);
               Assert.Equal(structuresArray[2*r+1].Name, rowGroupRecords[1].Name);
               Assert.Equal(structuresArray[2*r+1].Date, rowGroupRecords[1].Date);

            }
            Assert.Throws<ArgumentOutOfRangeException>("index",() => ParquetConvert.Deserialize<SimpleStructure>(ms, 5));
            Assert.Throws<ArgumentOutOfRangeException>("index",() => ParquetConvert.Deserialize<SimpleStructure>(ms, 99999));
         }
      }

      [Fact]
      public void Serialize_deserialize_repeated_field()
      {
         IEnumerable<SimpleRepeated> structures = Enumerable
            .Range(0, 10)
            .Select(i => new SimpleRepeated
            {
               Id = i,
               Areas = new int[] { i, 2, 3}
            });

         SimpleRepeated[] s = ConvertSerialiseDeserialise(structures);

         Assert.Equal(10, s.Length);

         Assert.Equal(0, s[0].Id);
         Assert.Equal(1, s[1].Id);

         Assert.Equal(new[] { 0, 2, 3 }, s[0].Areas);
         Assert.Equal(new[] { 1, 2, 3 }, s[1].Areas);
      }

      [Fact]
      public void Serialize_deserialize_empty_enumerable()
      {
         IEnumerable<SimpleRepeated> structures = Enumerable.Empty<SimpleRepeated>();

         SimpleRepeated[] s = ConvertSerialiseDeserialise(structures);
   
         Assert.Equal(0, s.Length);
      }

      [Fact]
      public void Serialize_structure_with_DateTime()
      {
         TestRoundTripSerialization<DateTime>(DateTime.UtcNow.RoundToSecond());
      }

      [Fact]
      public void Serialize_structure_with_nullable_DateTime()
      {
         TestRoundTripSerialization<DateTime?>(DateTime.UtcNow.RoundToSecond());
         TestRoundTripSerialization<DateTime?>(null);
      }

      [Fact]
      public void Serialise_groups()
      {
         DateTime now = DateTime.Now;

         IEnumerable<SimpleStructure> structures = Enumerable
            .Range(start: 0, count: 10)
            .Select(i => new SimpleStructure
            {
               Id = i,
               NullableId = (i % 2 == 0) ? new int?() : new int?(i),
               Name = $"row {i}",
               Date = now.AddDays(i).RoundToSecond().ToUniversalTime()
            });

         using (var ms = new MemoryStream())
         {
            Schema schema = ParquetConvert.Serialize(structures, ms, compressionMethod: CompressionMethod.Snappy, rowGroupSize: 2);

            ms.Position = 0;

            SimpleStructure[/*Groups*/][] groups2 = ParquetConvert.DeserializeGroups<SimpleStructure>(ms).ToArray();
            Assert.Equal(10/2, groups2.Length); //groups = count/rowGroupSize

            SimpleStructure[] structuresArray = structures.ToArray();

            SimpleStructure[] structures2 = (
               from g in groups2
               from s in g
               select s
            ).ToArray();

            for (int i = 0; i < 10; i++)
            {
               Assert.Equal(structuresArray[i].Id, structures2[i].Id);
               Assert.Equal(structuresArray[i].NullableId, structures2[i].NullableId);
               Assert.Equal(structuresArray[i].Name, structures2[i].Name);
               Assert.Equal(structuresArray[i].Date, structures2[i].Date);
            }
         }
      }

      void TestRoundTripSerialization<T>(T value)
      {
         StructureWithTestType<T> input = new StructureWithTestType<T>
         {
            Id = "1",
            TestValue = value,
         };

         Schema schema = SchemaReflector.Reflect<StructureWithTestType<T>>();

         using (MemoryStream stream = new MemoryStream())
         {
            ParquetConvert.Serialize<StructureWithTestType<T>>(new StructureWithTestType<T>[] { input }, stream, schema);

            stream.Position = 0;
            StructureWithTestType<T>[] output = ParquetConvert.Deserialize<StructureWithTestType<T>>(stream);
            Assert.Single(output);
            Assert.Equal("1", output[0].Id);
            Assert.Equal(value, output[0].TestValue);
         }
      }

      public class SimpleRepeated
      {
         public int Id { get; set; }

         public int[] Areas { get; set; }
      }

      public class SimpleStructure
      {
         public int Id { get; set; }

         public int? NullableId { get; set; }

         public string Name { get; set; }

         public DateTimeOffset Date { get; set; }
      }
      public class SimpleStructureWithFewProperties
      {
         public int Id { get; set; }
         public string Name { get; set; }
      }
      public class StructureWithIgnoredProperties
      {
         public int Id { get; set; }
         public string Name { get; set; }

         [ParquetIgnore]
         public string SSN { get; set; }

         [ParquetIgnore]
         public DateTime NonNullableDateTime { get; set; }
         [ParquetIgnore]
         public DateTime? NullableDateTime { get; set; }

         [ParquetIgnore]
         public int NonNullableInt { get; set; }

         [ParquetIgnore]
         public int? NullableInt { get; set; }

         [ParquetIgnore]
         public decimal NonNullableDecimal { get; set; }
         [ParquetIgnore]
         public decimal? NullableDecimal { get; set; }
      }

      public class SimpleRenamed
      {
         public int Id { get; set; }

         [ParquetColumn("Name")]
         public string PersonName { get; set; }
      }

      public class StructureWithTestType<T>
      {
         T testValue;

         public string Id { get; set; }

         // public T TestValue { get; set; }
         public T TestValue { get { return testValue; } set { testValue = value; } }
      }
   }
}
