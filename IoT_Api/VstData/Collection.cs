using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BsonData
{
    class UpdatingState : Dictionary<string, int>
    {
        public const int Deleted = -1;
        public const int Changed = 0;
        public const int Inserted = 1;
        public bool Busy { get; private set; }
        public void Set(string key, int val)
        {
            while (Busy) { }
            int v;
            if (this.TryGetValue(key, out v))
            {
                if (v != val)
                {
                    base[key] = val;
                }
            }
            else
            {
                base.Add(key, val);
            }
        }
        public int Get(string key)
        {
            while (Busy) { }
            
            int v = int.MaxValue;
            this.TryGetValue(key, out v);
            return v;
        }

        public void Clear(Action<string, int> action)
        {
            if (this.Count > 0)
            {
                Busy = true;
                var ts = new ThreadStart(() =>
                {
                    try
                    {
                        foreach (var p in this)
                        {
                            action(p.Key, p.Value);
                        }
                        base.Clear();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Busy = false;
                });
                new Thread(ts).Start();
            }
        }
    }
    public class Collection
    {
        UpdatingState _updating = new UpdatingState();

        public Database Database { get; private set; }
        public string Name { get; private set; }
        public Collection(string name, Database db)
        {
            Database = db;
            Name = name;

            BeginRead();
        }

        #region LIST
        public bool IsBusy => (_stroreThread != null && _stroreThread.IsAlive);
        int _count;
        public int Count
        {
            get
            {
                Wait(null);
                return _count;
            }
        }
        class Node
        {
            public string Value { get; set; }
            public Node Next { get; set; }
            public Node Prev { get; set; }
        }
        Node _head, _tail;
        void _add(Document doc)
        {
            var node = new Node { Value = doc.ObjectId };
            if (_count++ == 0)
            {
                _head = node;
            }
            else
            {
                node.Prev = _tail;
                _tail.Next = node;
            }

            _tail = node;
        }
        void _remove(Node node)
        {
            var next = node.Next;
            var prev = node.Prev;

            if (next != null) next.Prev = prev;
            if (prev != null) prev.Next = next;

            if (node == _head) _head = next;
            if (node == _tail) _tail = prev;

            _count--;
        }
        void _load()
        {
            var s = Database.CollectionStorage.GetSubStorage(this.Name);
            foreach (var e in s.ReadAll())
            {
                _add(e);
                Database.Add(e);
            }
        }

        Thread _stroreThread;
        void _store()
        {
            if (_updating.Count == 0) return;

            var s = Database.CollectionStorage.GetSubStorage(this.Name);
            _updating.Clear((k, v) =>
            {
                if (v == UpdatingState.Deleted)
                {
                    s.Delete(k);
                    return;
                }

                s.Write(Database[k]);
            });
        }

        //bool _busy;
        //void _start_long_action(Action action)
        //{
        //    _busy = true;
        //    action.Invoke();
        //    _busy = false;
        //}

        public void BeginRead()
        {
            try
            {
                _stroreThread = new Thread(() => _load());
                _stroreThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void BeginWrite()
        {
            _store();
        }
        #endregion

        #region FINDING
        public Document Find(string objectId, Action<Document> callback)
        {
            Document doc = Database[objectId];
            if (doc != null)
            {
                callback?.Invoke(doc);
            }
            return doc;
        }
        public T Find<T>(string objectId)
            where T : Document, new()
        {
            var doc = Database[objectId];
            if (doc == null) { return null; }

            if (doc.GetType() == typeof(T)) { return (T)doc; }

            var context = new T();
            context.Copy(doc);

            Database[objectId] = context;

            return context;
        }
        public void FindAndDelete(string objectId, Action<Document> before)
        {
            Find(objectId, doc =>
            {
                before?.Invoke(doc);

                Database.Remove(objectId);

                _updating.Set(objectId, UpdatingState.Deleted);
            });
        }
        public void FindAndUpdate(string objectId, Action<Document> before)
        {
            Find(objectId, doc =>
            {
                before?.Invoke(doc);
                _updating.Set(objectId, UpdatingState.Changed);
            });
        }
        #endregion

        #region DB
        public void Wait(Action callback)
        {
            while (IsBusy) { }
            callback?.Invoke();
        }
        public IEnumerable<Document> Select(Func<Document, bool> where)
        {
            var lst = Select();
            if (where != null)
            {
                lst = lst.Where(where);
            }
            return lst;
        }
        public IEnumerable<Document> Select()
        {
            var lst = new List<Document>();
            Wait(() =>
            {
                var node = _head;
                while (node != null)
                {
                    var next = node.Next;
                    var documentId = node.Value;

                    if (_updating.Get(documentId) == UpdatingState.Deleted)
                    {
                        _remove(node);
                    }
                    else
                    {
                        var doc = Database[documentId];
                        if (doc == null)
                        {
                            _remove(node);
                        }
                        else
                        {
                            lst.Add(doc);
                        }
                    }
                    node = next;
                }
            });
            return lst;
        }
        public bool Insert(string id, Document doc)
        {
            if (id == null) id = new ObjectId();

            doc.ObjectId = id;
            if (Database.ContainsKey(id))
            {
                return false;
            }

            Database[id] = doc;

            _add(doc);
            _updating.Set(id, UpdatingState.Inserted);
            return true;
        }
        public bool Insert(Document doc)
        {
            return Insert(doc.ObjectId, doc);
        }
        public bool Update(string id, Document doc)
        {
            var res = false;
            FindAndUpdate(id, current =>
            {
                res = true;
                if (doc != current)
                {
                    foreach (var p in doc)
                    {
                        current.Push(p.Key, p.Value);
                    }
                }
                _updating.Set(id, UpdatingState.Changed);
            });

            return res;

        }
        public bool Update(Document doc)
        {
            return Update(doc.ObjectId, doc);
        }
        public bool Delete(Document doc)
        {
            return Delete(doc.ObjectId);
        }
        public bool Delete(string id)
        {
            var res = false;
            FindAndDelete(id, exist => res = true);

            return res;
        }
        public void InsertOrUpdate(Document doc)
        {
            var id = doc.ObjectId;
            Document old = Database[id];

            if (old == doc)
            {
                _updating.Set(id, UpdatingState.Changed);
                return;
            }
            if (old != null)
            {
                foreach (var p in doc)
                {
                    old.Push(p.Key, p.Value);
                }
            }
            else
            {
                Database.Add(id, doc);
                _add(doc);
            }
            _updating.Set(id, UpdatingState.Changed);
        }
        #endregion

        #region IMPORT
        public string ImportPrimaryKey { get; set; }
        public virtual bool Import(Document context)
        {
            if (ImportPrimaryKey == null)
            {
                context.ObjectId = new ObjectId().ToString();
                this.Insert(context);

                return true;
            }    
            var id = context.GetString(ImportPrimaryKey);
            if (id == null) return false;

            context.ObjectId = id;
            InsertOrUpdate(context);
            return true;
        }
        #endregion
    }
}
