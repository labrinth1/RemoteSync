using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Synchronization;

namespace Common
{
    [Serializable()]
    public abstract class SyncDetails : IDisposable
    {
        private string folderPath;
        private SyncId replicaId;
        private SyncIdFormatGroup idFormats = null;
        public ulong tickCount = 1; 
        private SyncKnowledge myKnowledge;
        private ForgottenKnowledge myForgottenKnowledge;
        public MetadataStore metadataStore = new MetadataStore();
        public bool localChangedDetected = false;

        public SyncDetails(string folderPath, bool load)
        {
            if (load)
            {
                this.idFormats = GetIdFormat();
                this.folderPath = folderPath;
                Load();
            }
        }

        public MetadataStore MetaDataStore
        {
            get { return metadataStore; }
            set { metadataStore = value; }
        }

        public ulong TickCount
        {
            get { return tickCount; }
            set { tickCount = value; }
        }

        public string FolderPath
        {
            get { return folderPath; }
        }

        public SyncId ReplicaId
        {
            get { return replicaId; }
            set { replicaId = value; }
        }

        public SyncKnowledge SyncKnowledge
        {
            get { return myKnowledge; }
            set { myKnowledge = value; }
        }

        public SyncIdFormatGroup IdFormats
        {
            get { return idFormats; }
            set { idFormats = value; }
        }

        public ForgottenKnowledge ForgottenKnowledge
        {
            get { return myForgottenKnowledge; }
            set { myForgottenKnowledge = value; }
        }

        public ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge, out object changeDataRetriever)
        {
            GetNextTickCount();

            List<ItemChange> changes = DetectChanges(destinationKnowledge, batchSize);

            myKnowledge.SetLocalTickCount(tickCount);

            ChangeBatch changeBatchBuilder = new ChangeBatch(IdFormats,
                destinationKnowledge, myForgottenKnowledge );

            changeBatchBuilder.BeginUnorderedGroup();

            changeBatchBuilder.AddChanges(changes);

            changeBatchBuilder.EndUnorderedGroup(myKnowledge, true);

            if ((changes.Count < batchSize) || (changes.Count == 0))
            {
                changeBatchBuilder.SetLastBatch();
            }

            changeDataRetriever = this;
            return changeBatchBuilder; 
        }

        public ChangeBatch GetChangeBatch(string path, uint batchSize, SyncKnowledge destinationKnowledge, out object changeDataRetriever)
        {
            folderPath = path;
            GetNextTickCount();

            List<ItemChange> changes = DetectChanges(destinationKnowledge, batchSize);

            myKnowledge.SetLocalTickCount(tickCount);

            ChangeBatch changeBatchBuilder = new ChangeBatch(IdFormats,destinationKnowledge,  myForgottenKnowledge);

            changeBatchBuilder.BeginUnorderedGroup();

            changeBatchBuilder.AddChanges(changes); 

            changeBatchBuilder.EndUnorderedGroup(myKnowledge, true);

            if ((changes.Count < batchSize) || (changes.Count == 0))
            {
                changeBatchBuilder.SetLastBatch();
            }


            changeDataRetriever = this;

            return changeBatchBuilder; 
        }


        public ChangeBatch GetChanges(ChangeBatch sourceChanges)
        {

            GetNextTickCount(); 
            myKnowledge.SetLocalTickCount(tickCount);

            List<ItemChange> changes = new List<ItemChange>();

            foreach (ItemChange ic in sourceChanges)
            {
                ItemMetadata item;
                ItemChange change;


                if (metadataStore.TryGetItem(ic.ItemId, out item))
                {
                    System.Diagnostics.Debug.WriteLine("Remote item has   local counterpart::" + item.Uri);
                    change = new ItemChange(IdFormats, ReplicaId, item.ItemId,
                        (item.IsTombstone ? ChangeKind.Deleted : ChangeKind.Update),
                        item.CreationVersion, item.ChangeVersion);
                   
                }
                else
                {
                    if (item == null)
                        System.Diagnostics.Debug.WriteLine("Remote item has no local counterpart: item.uri is null");
                    else
                        System.Diagnostics.Debug.WriteLine("Remote item has no local counterpart:" + item.Uri);


                    change = new ItemChange(IdFormats, replicaId, ic.ItemId, ChangeKind.UnknownItem,
                        SyncVersion.UnknownVersion, SyncVersion.UnknownVersion);

                }
                changes.Add(change);
            }

 
            ChangeBatch changeBatchBuilder = new ChangeBatch(IdFormats, myKnowledge , myForgottenKnowledge);

            changeBatchBuilder.BeginUnorderedGroup();

            changeBatchBuilder.AddChanges(changes); 

            changeBatchBuilder.EndUnorderedGroup(myKnowledge, true);

            return changeBatchBuilder; 
        }

        public void StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgottenKnowledge)
        {
            myKnowledge = knowledge;
            myForgottenKnowledge = forgottenKnowledge;

            Save();

        }

        public ItemMetadata GetItemMetaData(SaveChangeAction saveChangeAction, ItemChange change, DataTransfer data)
        {
            ItemMetadata item;
            if (saveChangeAction == SaveChangeAction.UpdateVersionOnly || ((change.ChangeKind & ChangeKind.Deleted) == ChangeKind.Deleted))
            {
                if (!metadataStore.TryGetItem(change.ItemId, out item))
                {
                    item = new ItemMetadata();
                    item.Uri = String.Empty;
                }
            }
            else
            {
                item = new ItemMetadata(); 
                item.Uri = data.Uri;
            }

            item.ItemId = change.ItemId;
            item.CreationVersion = change.CreationVersion;
            item.ChangeVersion = change.ChangeVersion;

            if ((change.ChangeKind & ChangeKind.Deleted) == ChangeKind.Deleted)
                item.IsTombstone = true;

            if (!metadataStore.Has(item.ItemId))
            {
                ItemMetadata oldItem;

                if (metadataStore.TryGetItem(item.Uri, out oldItem))
                {

                    if (item.ItemId.CompareTo(oldItem.ItemId) > 0)
                    {
                        oldItem.IsTombstone = true;
                        oldItem.Uri = String.Empty;
                        oldItem.ChangeVersion = new SyncVersion(0, tickCount);
                    }
                    else
                    {
                        item.IsTombstone = true;
                        item.Uri = String.Empty;
                        item.ChangeVersion = new SyncVersion(0, tickCount);
                    }
                    metadataStore.SetItemInfo(item);
                    metadataStore.SetItemInfo(oldItem);
                }
            }
            metadataStore.SetItemInfo(item);

            return item;
        }

        public void GetUpdatedKnowledge(SaveChangeContext context)
        {
            context.GetUpdatedDestinationKnowledge(out myKnowledge, out myForgottenKnowledge);
        }

        public void DeleteItem(SyncId itemID)
        {
            ItemMetadata item;
            if (metadataStore.TryGetItem(itemID, out item))
            {
                item.IsTombstone = true;
            }
        }

        public ulong GetNextTickCount()
        {
            return ++tickCount;
        }

        public void SetLocalTickCount()
        {
            myKnowledge.SetLocalTickCount(tickCount);
        }

        public void UpdateItemItem(ItemMetadata item)
        {
            metadataStore.SetItemInfo(item);
        }
        public MetadataStore UpdateItemWithReturn(ItemMetadata item)
        {
            metadataStore.SetItemInfo(item);

            return metadataStore;
        }

        public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
        {
            ItemMetadata metadata;

            if (!metadataStore.TryGetItem(sourceChange.ItemId, out metadata))
            {
                destinationVersion = null;
                return false;
            }

            if (metadata == null)
            {
                destinationVersion = null;
                return false;
            }
            else
            {
                destinationVersion = new ItemChange(idFormats, replicaId, sourceChange.ItemId,
                        metadata.IsTombstone ? ChangeKind.Deleted : ChangeKind.Update,
                        metadata.CreationVersion, metadata.ChangeVersion);
                return true;
            }
        }
        public void Save()
        {
            if (!string.IsNullOrEmpty(folderPath))
            {
                string syncFile = Path.Combine(folderPath, "file.sync");

                File.Delete(syncFile);

                using (FileStream stream = new FileStream(syncFile, FileMode.OpenOrCreate))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, replicaId);
                    bf.Serialize(stream, tickCount);
                    bf.Serialize(stream, myKnowledge);
                    bf.Serialize(stream, myForgottenKnowledge);

                    // Serialize metadatastore
                    metadataStore.Save(stream);
                }

                FileInfo fi = new FileInfo(syncFile);
                fi.Attributes = FileAttributes.Hidden | FileAttributes.System;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(" ¤Error Save() Without Path ");
            }
        }
        public void Save(string path)
        {

            string syncFile = Path.Combine(path, "file.sync");

            File.Delete(syncFile);

            using (FileStream stream = new FileStream(syncFile, FileMode.OpenOrCreate))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(stream, replicaId);
                bf.Serialize(stream, tickCount);
                bf.Serialize(stream, myKnowledge);
                bf.Serialize(stream, myForgottenKnowledge);

                metadataStore.Save(stream);

                System.Diagnostics.Debug.WriteLine("Saved with tickCount: " + tickCount.ToString());
            }

            FileInfo fi = new FileInfo(syncFile);
            fi.Attributes = FileAttributes.Hidden | FileAttributes.System;
        }
        public void Load()
        {
            string syncFile = Path.Combine(folderPath, "file.sync");

            if (File.Exists(syncFile))
            {
                using (FileStream stream = new FileStream(syncFile, FileMode.Open))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    replicaId = (SyncId)bf.Deserialize(stream);
                    tickCount = (ulong)bf.Deserialize(stream);
                    myKnowledge = (SyncKnowledge)bf.Deserialize(stream);
                    if (myKnowledge.ReplicaId != ReplicaId)
                        throw new Exception("Replica id of loaded knowledge doesn't match replica id provided in constructor.");
                    myForgottenKnowledge = (ForgottenKnowledge)bf.Deserialize(stream);
                    if (myForgottenKnowledge.ReplicaId != ReplicaId)
                        throw new Exception("Replica id of loaded forgotten knowledge doesn't match replica id provided in constructor.");

                    System.Diagnostics.Debug.WriteLine("    ### Loaded For: " + folderPath + "  # Now Load metadatastore");
                    metadataStore.Load(stream);

                    System.Diagnostics.Debug.WriteLine("    ### End load Knowledge ###");
                }
                FindLocalFileChanges(folderPath);
            }
            else
            {
                replicaId = new SyncId(Guid.NewGuid());
                myKnowledge = new SyncKnowledge(IdFormats, ReplicaId, tickCount);
                myForgottenKnowledge = new ForgottenKnowledge(IdFormats, myKnowledge);
            }
        }

        public static SyncIdFormatGroup GetIdFormat()
        {
            SyncIdFormatGroup idFormats = new SyncIdFormatGroup();

            idFormats.ChangeUnitIdFormat.IsVariableLength = false;
            idFormats.ChangeUnitIdFormat.Length = 1;

            idFormats.ReplicaIdFormat.IsVariableLength = false;
            idFormats.ReplicaIdFormat.Length = 16;

            idFormats.ItemIdFormat.IsVariableLength = false;
            idFormats.ItemIdFormat.Length = 24;

            return idFormats;
        }
        public abstract object LoadChangeData(LoadChangeContext loadChangeContext);


        public void Dispose()
        {
            
        }

        private List<ItemChange> DetectChanges(SyncKnowledge destinationKnowledge, uint batchSize)
        {
            System.Diagnostics.Debug.WriteLine(" Start DetectChanges in:" + folderPath);

            List<ItemChange> changeBatch = new List<ItemChange>();

            if (destinationKnowledge == null)
                throw new ArgumentNullException("destinationKnowledge");

            if (batchSize < 0)
                throw new ArgumentOutOfRangeException("batchSize");

            if (!localChangedDetected)
            {
                FindLocalFileChanges(folderPath);
                localChangedDetected = true;
            }

            SyncKnowledge mappedKnowledge = myKnowledge.MapRemoteKnowledgeToLocal(destinationKnowledge);


            System.Diagnostics.Debug.WriteLine("  Is the current version of the item is known to the destination ?");
            foreach (ItemMetadata item in metadataStore)
            {
                if (!mappedKnowledge.Contains(replicaId, item.ItemId, item.ChangeVersion))// Create
                {
                    System.Diagnostics.Debug.WriteLine("Not Known:" + item.Uri + " IsTombstone:" + item.IsTombstone.ToString());
                    ItemChange itemChange = new ItemChange(IdFormats, replicaId, item.ItemId,
                        (item.IsTombstone) ? ChangeKind.Deleted : ChangeKind.Update,
                        item.CreationVersion, item.ChangeVersion);


                    changeBatch.Add(itemChange);
                }
                else if (item.IsTombstone)//Delete
                {
                    System.Diagnostics.Debug.WriteLine("Item is Known:" + item.Uri + " Its a Tombstone so add it:");

                    ItemChange itemChange = new ItemChange(IdFormats, replicaId, item.ItemId, ChangeKind.Deleted, item.CreationVersion, item.ChangeVersion);
                    changeBatch.Add(itemChange);
                }
                else//Update
                {
                    System.Diagnostics.Debug.WriteLine("mappedKnowledge is know item " + item.Uri + " And was no tombstone");
                    ItemChange itemChange = new ItemChange(IdFormats, replicaId, item.ItemId, ChangeKind.Update,
                        item.CreationVersion, item.ChangeVersion);

                    changeBatch.Add(itemChange);
                }

                if (changeBatch.Count == batchSize)
                    break;
            }
            System.Diagnostics.Debug.WriteLine("        #### End DetectChanges ####");
            return changeBatch;
        }

  
        private void FindLocalFileChanges(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
            {
                FindLocalFileChanges(dir.FullName);
            }

            foreach (FileInfo fi in directoryInfo.GetFiles())
            {
                string uri = fi.FullName.Substring(folderPath.Length + 1);
                if (Path.GetExtension(fi.Name).ToUpperInvariant() == ".SYNC")
                {
                    continue;
                }
                else
                {
                    ItemMetadata existingItem;
                    if (!metadataStore.TryGetItem(uri, out existingItem)) //Create
                    {
                        ItemMetadata item = new ItemMetadata();

                        item.Uri = uri;
                        item.IsTombstone = false; 

                        item.ItemId = new SyncId(new SyncGlobalId(0, Guid.NewGuid()));
                        System.Diagnostics.Debug.WriteLine("FindLocalFileChanges: Create found for uri:" + uri + " ItemID: " + item.ItemId);
                        item.CreationVersion = new SyncVersion(0, tickCount);

                        item.ChangeVersion = item.CreationVersion;

                        item.LastWriteTimeUtc = fi.LastWriteTimeUtc;

                        metadataStore.SetItemInfo(item);
                    }
                    else
                    {
                        if (existingItem.LastWriteTimeUtc == null || fi.LastWriteTimeUtc > existingItem.LastWriteTimeUtc)
                        {
                            System.Diagnostics.Debug.WriteLine("FindLocalFileChanges: File has been changed:  " + uri);
  
                            existingItem.ChangeVersion = new SyncVersion(0, tickCount);
                            existingItem.LastWriteTimeUtc = fi.LastWriteTimeUtc;

                            metadataStore.SetItemInfo(existingItem);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("No change found for file : " + uri);
                        }
                    }
                }
            }
            List<ItemMetadata> toUpdate = new List<ItemMetadata>();

            foreach (ItemMetadata item in metadataStore)
            {
                if (!File.Exists(Path.Combine(FolderPath, item.Uri)))
                {
                    if (!item.IsTombstone)
                    {
                        item.IsTombstone = true;
                        item.ChangeVersion = new SyncVersion(0, tickCount);
                        toUpdate.Add(item);
                    }
                }
            }
            foreach (ItemMetadata item in toUpdate)
            {
                metadataStore.SetItemInfo(item);
            }
        }
    }
}
