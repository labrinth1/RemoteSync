using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Synchronization;

namespace Common
{
    [Serializable()]
    public class MetadataStore : IEnumerable<ItemMetadata>
    {
        private string path = null;

        public string Path
        {

            get
            {
                if (path == null)
                    throw new InvalidOperationException("Path is not set until metadata store is created / loaded.");

                return path;
            }

            private set { path = value; }
        }


        private List<ItemMetadata> data = new List<ItemMetadata>();

        private List<ItemMetadata> Data
        {
            get
            {
                if (data == null)
                    throw new InvalidOperationException("Metadata store contains no data (need to load or create).");

                return data;
            }

            set
            {
                data = value;
            }
        }
        public void Create(string path)
        {
            Path = path;
            Data = new List<ItemMetadata>();
            Save();
        }
        public void Load(string path)
        {
            Path = path;

            FileStream inputStream = new FileStream(Path, FileMode.Open);

            Load(inputStream);

            // Close the file
            inputStream.Close();
        }

        public void Load(FileStream stream)
        {
            BinaryFormatter bf = new BinaryFormatter();
            Data = bf.Deserialize(stream) as List<ItemMetadata>;

            foreach (var item in data)
            {
                System.Diagnostics.Debug.WriteLine("File: " + item.Uri + " Istombstone: " + item.IsTombstone.ToString());
            }
        }

        public void Save()
        {
            FileStream outputStream = new FileStream(Path, FileMode.OpenOrCreate);

            Save(outputStream);

            outputStream.Close();
        }

        public void Save(FileStream stream)
        {
            BinaryFormatter bf = new BinaryFormatter();

            foreach (var item in data)
            {
                if (item.LastWriteTimeUtc == null)
                {
                    item.LastWriteTimeUtc = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine("No Date for: " + stream.Name + " Date set");
                }
            }

            bf.Serialize(stream, Data);

            System.Diagnostics.Debug.WriteLine(" Start Saving Metadata: " + stream.Name);
            foreach (var item in data)
            {
                System.Diagnostics.Debug.WriteLine("Saved Metadata File:" + item.Uri);
                System.Diagnostics.Debug.WriteLine(" Istombstone : " + item.IsTombstone.ToString());
                System.Diagnostics.Debug.WriteLine("ChangeVersion: " + item.ChangeVersion.ToString());
                System.Diagnostics.Debug.WriteLine("Lasttimewrite: " + item.LastWriteTimeUtc.ToString());
            }

            System.Diagnostics.Debug.WriteLine("| End Saving Metadata |");
        }

        public bool TryGetItem(string uri, out ItemMetadata item)
        {
            ItemMetadata im = Data.Find(delegate(ItemMetadata compareItem) { return (compareItem.Uri == uri && compareItem.IsTombstone == false); });

            if (im == null)
            {
                item = null;
                return false;
            }

            item = im.Clone();

            return true;
        }

        public bool TryGetItem(SyncId itemId, out ItemMetadata item)
        {
            ItemMetadata im = Data.Find(delegate(ItemMetadata compareItem) { return (compareItem.ItemId == itemId); });

            if (im == null)
            {
                item = null;
                return false;
            }

            item = im.Clone();

            return true;
        }

        public ItemMetadata GetItem(int index)
        {
            return Data[index].Clone();
        }

        public bool Has(SyncId itemId)
        {
            return Data.Exists(delegate(ItemMetadata compareItem) { return (compareItem.ItemId == itemId); });
        }

        public int NumEntries
        {
            get
            {
                return Data.Count;
            }
        }
        public void SetItemInfo(ItemMetadata itemMetadata)
        {
            int index;

            index = Data.FindIndex(delegate(ItemMetadata compareItem) { return (compareItem.ItemId == itemMetadata.ItemId); });

            if (index >= 0)
            {
                Data[index] = itemMetadata.Clone();
            }
            else
            {
                Data.Add(itemMetadata.Clone());
            }
        }
        public void ReplaceItem(SyncId oldItemId, ItemMetadata item)
        {
            int index;

            index = Data.FindIndex(delegate(ItemMetadata compareItem) { return (compareItem.ItemId == oldItemId); });

            if (index < 0)
                throw new KeyNotFoundException("ItemID is not found.");

            Data[index] = item.Clone();
        }

        public IEnumerator<ItemMetadata> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)Data).GetEnumerator();
        }

        public void CleanTombstones()
        {
            for (int i = Data.Count; i > 0; i--)
            {
                if (Data[i - 1].IsTombstone)
                {
                    Data.RemoveAt(i - 1);
                }
            }
        }
    }

}
    