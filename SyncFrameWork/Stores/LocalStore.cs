using System;
using System.IO;
using Common;
using Microsoft.Synchronization;


namespace SyncFrameWork.Controllers
{
    public class LocalStore : KnowledgeSyncProvider, INotifyingChangeApplierTarget
    {
        private string folderPath;
        private SyncSessionContext currentSessionContext;
        private uint requestedBatchSize = 1;
        public LocalSyncDetails sync = null;
        MemoryConflictLog _memConflictLog;

        public LocalStore(string folderPath)
        {
            sync = new LocalSyncDetails(folderPath, true);
            this.folderPath = folderPath;
        }

        private string FolderPath
        {
            get { return folderPath; }
        }
        public uint RequestedBatchSize
        {
            get { return requestedBatchSize; }
            set { requestedBatchSize = value; }
        }

        public override void BeginSession(Microsoft.Synchronization.SyncProviderPosition providerPosition, SyncSessionContext syncSessionContext)
        {
            currentSessionContext = syncSessionContext;
            _memConflictLog = new MemoryConflictLog(IdFormats);
        }

        public override void EndSession(SyncSessionContext syncSessionContext)
        {
            sync.Save();
            System.Diagnostics.Debug.WriteLine("_____   Ending Session On LocalStore   ______" );
           
        }

        public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge, out object changeDataRetriever)
        {
            return sync.GetChangeBatch(batchSize, destinationKnowledge, out changeDataRetriever);
        }

        public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval,out object changeDataRetriever)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
        {
            if (sync == null)
                throw new InvalidOperationException("Knowledge not yet loaded.");

            sync.SetLocalTickCount();

            batchSize = RequestedBatchSize;

            knowledge = sync.SyncKnowledge.Clone();
        }

        public override SyncIdFormatGroup IdFormats
        {
            get { return sync.IdFormats; }
        }
        /// <summary>
        /// Download Mechanism 
        /// </summary>
        /// <param name="resolutionPolicy"></param>
        /// <param name="sourceChanges"></param>
        /// <param name="changeDataRetriever"></param>
        /// <param name="syncCallback"></param>
        /// <param name="sessionStatistics"></param>
        public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, 
            object changeDataRetriever, SyncCallbacks syncCallback, SyncSessionStatistics sessionStatistics)
        {
            
            ChangeBatch localVersions = sync.GetChanges(sourceChanges);

            ForgottenKnowledge destinationForgottenKnowledge = new ForgottenKnowledge(sync.IdFormats, sync.SyncKnowledge);

            NotifyingChangeApplier changeApplier = new NotifyingChangeApplier(sync.IdFormats);


            changeApplier.ApplyChanges(resolutionPolicy, CollisionConflictResolutionPolicy.Merge, sourceChanges,
        (IChangeDataRetriever)changeDataRetriever, localVersions, sync.SyncKnowledge.Clone(),
        destinationForgottenKnowledge, this, _memConflictLog, currentSessionContext, syncCallback);
            
        }

        public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy,
            FullEnumerationChangeBatch sourceChanges,
            object changeDataRetriever, SyncCallbacks syncCallback, SyncSessionStatistics sessionStatistics)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public ulong GetNextTickCount()
        {
            return sync.GetNextTickCount();
        }

        #region IChangeDataRetriever Members

        public object LoadChangeData(LoadChangeContext loadChangeContext)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region INotifyingChangeApplierTarget Members

        public void SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
        {
    
          return sync.TryGetDestinationVersion(sourceChange, out destinationVersion);
        }
 
        public IChangeDataRetriever GetDataRetriever()
        {
            throw new NotImplementedException();
        }

        public void StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgottenKnowledge)
        {
  
            sync.SyncKnowledge = knowledge;
            sync.ForgottenKnowledge = forgottenKnowledge;
            System.Diagnostics.Debug.WriteLine("Local.StoreKnowledgeForScope:" + knowledge + "ForgottenKnowledge:" + forgottenKnowledge);
        }

        #endregion

        public void SaveConstraintConflict(ItemChange conflictingChange, SyncId conflictingItemId, ConstraintConflictReason reason, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge, bool temporary)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Download Mechanism
        /// </summary>
        /// <param name="saveChangeAction"></param>
        /// <param name="change"></param>
        /// <param name="context"></param>
        public void SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
        {
            
            DataTransfer data = context.ChangeData as DataTransfer;

            ItemMetadata item = sync.GetItemMetaData(saveChangeAction, change, data);
            switch (saveChangeAction)
            {
                case SaveChangeAction.Create:
                    {
                        System.Diagnostics.Debug.WriteLine("Create File: " + item.Uri);
                        UpdateOrCreateFile(data, item);

                        break;
                    }
                case SaveChangeAction.UpdateVersionAndData:
                        {
                            System.Diagnostics.Debug.WriteLine("UpdateVersion And Data File: " + item.Uri);
                            UpdateOrCreateFile(data, item);

                        break;
                        }
                case SaveChangeAction.DeleteAndStoreTombstone:
                    {
                        System.Diagnostics.Debug.WriteLine("   Delete File: " + item.Uri);
                        File.Delete(Path.Combine(folderPath, item.Uri));
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException(saveChangeAction + " ChangeAction is not implemented!");
                    }

            }
            sync.GetUpdatedKnowledge(context);
            
        }

        private void UpdateOrCreateFile(DataTransfer data, ItemMetadata item)
        {
            FileInfo fileInfo = new FileInfo(Path.Combine(folderPath, item.Uri));

            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();


            using (FileStream outputStream = new FileStream(Path.Combine(folderPath, item.Uri), FileMode.OpenOrCreate))
            {
                const int copyBlockSize = 4096;
                byte[] buffer = new byte[copyBlockSize];

                int bytesRead;
                while ((bytesRead = data.DataStream.Read(buffer, 0, copyBlockSize)) > 0)
                    outputStream.Write(buffer, 0, bytesRead);

                outputStream.SetLength(outputStream.Position);
            }
            item.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;

            data.DataStream.Close();
        }
        public void SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
        {
            throw new NotImplementedException();
        }
    }
}
