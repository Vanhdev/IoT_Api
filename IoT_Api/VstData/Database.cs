using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace BsonData
{
    public class FileStorage
    {
        public DirectoryInfo Folder { get; private set; }
        public FileStorage(string path)
        {
            Folder = new DirectoryInfo(path);
            if (Folder.Exists == false)
            {
                Folder.Create();
            }
        }
        public FileStorage GetSubStorage(string name)
        {
            return new FileStorage(Folder.FullName + '/' + name);
        }
        public FileInfo GetFile(Document doc)
        {
            return GetFile(doc.ObjectId);
        }
        public FileInfo GetFile(string id)
        {
            return new FileInfo(Folder.FullName + '/' + id);
        }

        public void Write(Document doc)
        {
            WriteDocument(GetFile(doc), doc);
        }
        public void Delete(string id)
        {
            GetFile(id).Delete();
        }
        public List<Document> ReadAll()
        {
            return ReadFolderAsync(Folder);
        }

        //public static List<Document> ReadFolderAsync(DirectoryInfo src)
        //{
        //    var dst = new List<Document>();
        //    foreach (var fi in src.GetFiles())
        //    {
        //        var doc = ReadDocument(fi);
        //        if (doc.ObjectId == null)
        //        {
        //            doc.ObjectId = fi.Name;
        //        }
        //        dst.Add(doc);
        //    }
        //    return dst;
        //}
        public static List<Document> ReadFolderAsync(DirectoryInfo src)
        {
            var dst = new List<Document>();
            var map = new Dictionary<FileInfo, Document>();
            foreach (var fi in src.GetFiles())
            {
                try
                {
                    var doc = ReadDocument(fi);
                    if (doc.ObjectId == null)
                    {
                        doc.ObjectId = fi.Name;
                        map.Add(fi, doc);
                    }
                    dst.Add(doc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            foreach (var p in map)
            {
                WriteDocument(p.Key, p.Value);
            }
            return dst;
        }

        public static Document ReadDocument(FileInfo fi)
        {
            try
            {
                using (var br = new BsonDataReader(fi.OpenRead()))
                {
                    var serializer = new JsonSerializer();
                    var doc = serializer.Deserialize<Document>(br);

                    return doc;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return new Document();
        }
        public static void WriteDocument(FileInfo fi, Document doc)
        {
            try
            {
                using (var bw = new BsonDataWriter(fi.OpenWrite()))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(bw, doc);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
    public class Database : DocumentMap
    {
        #region STORAGE
        public FileStorage Storage { get; private set; }
        FileStorage _documentStorage;
        public FileStorage DocumentStorage
        {
            get
            {
                if (_documentStorage == null) { _documentStorage = Storage.GetSubStorage("Documents"); }
                return _documentStorage;
            }
        }
        FileStorage _collectionStorage;
        public FileStorage CollectionStorage
        {
            get
            {
                if (_collectionStorage == null) { _collectionStorage = Storage.GetSubStorage("Collections"); }
                return _collectionStorage;
            }
        }

        public bool IsBusy
        {
            get
            {
                foreach (var p in _collections)
                {
                    if (p.Value.IsBusy) { return true; }
                }
                return false;
            }
        }
        Thread _storageThread;
        public Database StartStorageThread(int interval)
        {
            _storageThread?.Abort();

            _storageThread = new Thread(() => {

                while (true)
                {
                    foreach (var p in _collections)
                    {
                        p.Value.BeginWrite();
                    }
                    Thread.Sleep(interval);
                }
            });
            _storageThread.Start();
            return this;
        }
        public Database StartStorageThread()
        {
            return this.StartStorageThread(1000);
        }

        #endregion

        public string ConnectionString { get; private set; }
        public string Name { get; private set; }
        public string PhysicalPath => ConnectionString + '/' + Name;
        public string DataPath(string name) => ConnectionString + '/' + name;
        public Database(string name)
        {
            Name = name;
        }

        public Database Connect(string connectionString)
        {
            this.ConnectionString = connectionString;
            Storage = new FileStorage(PhysicalPath);

            return this;
        }
        public void Disconnect()
        {
            Console.Write("Disconnecting ... ");

            _storageThread.Abort();

            while (IsBusy) { }
            Console.WriteLine("done");
        }

        #region COLLECTIONS
        BsonDataMap<Collection> _collections = new BsonDataMap<Collection>();
        public Collection GetCollection(string name)
        {
            Collection data = _collections[name];
            if (data == null)
            {
                _collections.Add(name, data = new Collection(name, this));
            }
            return data;
        }
        #endregion
        new public void Clear()
        {
            base.Clear();
            new DirectoryInfo(this.PhysicalPath).Delete(true);
        }
    }
}
