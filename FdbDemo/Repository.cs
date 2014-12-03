﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FoundationDB.Client;
using FoundationDB.Layers.Directories;

namespace FdbDemo
{
    public class Repository
    {
        private const string FeatureSubspace = "f";
        private readonly FdbDatabaseProvider _dbProvider;
        private readonly ConcurrentDictionary<string, FdbDirectorySubspace> _directoriesCache = 
            new ConcurrentDictionary<string, FdbDirectorySubspace>();

        public Repository(FdbDatabaseProvider dbProvider)
        {
            _dbProvider = dbProvider;
        }

        public void CreateFeature(string name, byte[] feature)
        {
            var featureSpace = GetOrCreateDir(FeatureSubspace);
            var fkey = featureSpace.Pack(name);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            _dbProvider.Db.WriteAsync(trans => trans.Set(fkey, Slice.FromStream(new MemoryStream(feature))), new CancellationToken()).Wait();
            sw.Stop();
            Console.WriteLine(string.Format("Created feature in {0} ms", sw.ElapsedMilliseconds));
        }

        public Slice GetFeature(string name)
        {
            var featureSpace = GetOrCreateDir(FeatureSubspace);
            var fkey = featureSpace.Pack(name);

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var featureSlice = _dbProvider.Db.ReadAsync(trans => trans.GetAsync(fkey), new CancellationToken()).Result;
            sw.Stop();
            Console.WriteLine(string.Format("Read feature in {0} ms", sw.ElapsedMilliseconds));
            return featureSlice;
        }

        private FdbDirectorySubspace GetOrCreateDir(string name)
        {
            return _directoriesCache.GetOrAdd(name, _dbProvider.Db.Directory.CreateOrOpenAsync(name, new CancellationToken()).Result);
        }
    }
}