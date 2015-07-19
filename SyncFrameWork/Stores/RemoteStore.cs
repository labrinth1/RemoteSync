using System;
using System.ServiceModel;
using Common;
using Common.DTO;
using Microsoft.Synchronization;
using SyncFrameWork.SyncService;

namespace SyncFrameWork.Controllers
{
    public class RemoteStore : KnowledgeSyncProvider, INotifyingChangeApplierTarget
    {
        private uint requestedBatchSize = 1;

        private SyncServiceClient service = new SyncServiceClient();
        private SyncKnowledge myKnowledge = null;
        private ForgottenKnowledge myForgottenKnowledge = null;
        private SyncSessionContext currentSessionContext;
        MemoryConflictLog _memConflictLog;

        public RemoteSyncDetails sync = null;

        public RemoteStore(string endpoint)
        {
            sync = new RemoteSyncDetails(null, false);
            ConfigureEndPoint(endpoint);

            var syncdetails = service.LoadSyncSession();

            sync = syncdetails.Cast<RemoteSyncDetails>();
            sync.Service = service;

        }

        private void ConfigureEndPoint(string endpoint)
        {
            service.Endpoint.Address = new EndpointAddress(endpoint);

            if (service.Endpoint.Binding is System.ServiceModel.WSHttpBinding)
            {
                int max = 2147483647;
                System.ServiceModel.WSHttpBinding binding = (System.ServiceModel.WSHttpBinding)service.Endpoint.Binding;
                binding.UseDefaultWebProxy = false;
                binding.MaxReceivedMessageSize = max;
                binding.MaxBufferPoolSize = max;
                binding.MaxBufferPoolSize = max;
                binding.ReaderQuotas.MaxArrayLength = max;
                binding.ReaderQuotas.MaxBytesPerRead = max;
                binding.ReaderQuotas.MaxStringContentLength = max;
            }
        }

        public uint RequestedBatchSize
        {
            get { return requestedBatchSize; }
            set { requestedBatchSize = value; }
        }

        public override void BeginSession(Microsoft.Synchronization.SyncProviderPosition providerPosition,
            SyncSessionContext syncSessionContext)
        {
            currentSessionContext = syncSessionContext;
            _memConflictLog = new MemoryConflictLog(IdFormats);
        }

        public override void EndSession(SyncSessionContext syncSessionContext)
        {
            service.SaveSyncSession(sync.Cast<LocalSyncDetails>());
            System.Diagnostics.Debug.WriteLine("_____   Ending Session On RemoteStore   ______");


        }
        public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge, out object changeDataRetriever)
        {
            System.Diagnostics.Debug.WriteLine("GetChangeBatch:" + destinationKnowledge.ToString());
            changeDataRetriever = this;

            ChangeBatchTransfer batch = (ChangeBatchTransfer)service.GetChanges(batchSize, destinationKnowledge, sync.Cast<LocalSyncDetails>()).ByteArrayToObject();

            ChangeBatch changeBatch = batch.ChangeBatch;
            changeDataRetriever = batch.ChangeDataRetriever;

            changeDataRetriever = changeDataRetriever.Cast<RemoteSyncDetails>();
            sync = changeDataRetriever.Cast<RemoteSyncDetails>();

            ((RemoteSyncDetails)(changeDataRetriever)).Service = service;
            return changeBatch;

        }

        public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval, out object changeDataRetriever)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
        {
            batchSize = RequestedBatchSize;
            myKnowledge = sync.SyncKnowledge;
            knowledge = myKnowledge.Clone();
        }

        public override SyncIdFormatGroup IdFormats
        {
            get { return RemoteSyncDetails.GetIdFormat(); }
        }

        /// <summary>
        /// Upload mechanism
        /// </summary>
        /// <param name="resolutionPolicy"></param>
        /// <param name="sourceChanges">Local File Changes</param>
        /// <param name="changeDataRetriever"></param>
        /// <param name="syncCallback"></param>
        /// <param name="sessionStatistics"></param>
        public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, object changeDataRetriever, SyncCallbacks syncCallback, SyncSessionStatistics sessionStatistics)
        {
            myForgottenKnowledge = new ForgottenKnowledge(IdFormats, myKnowledge);

            NotifyingChangeApplier changeApplier = new NotifyingChangeApplier(IdFormats);

            changeApplier.ApplyChanges(resolutionPolicy, sourceChanges, (IChangeDataRetriever)changeDataRetriever, myKnowledge.Clone(), myForgottenKnowledge,
                this, currentSessionContext, syncCallback);
        }

        public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy, FullEnumerationChangeBatch sourceChanges,
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


        public void SaveChangeWithChangeUnits(ItemChange change, SaveChangeWithChangeUnitsContext context)
        {
            throw new NotImplementedException("The method or operation is not implemented.");

        }

        public void SaveConflict(ItemChange conflictingChange, object conflictingChangeData, SyncKnowledge conflictingChangeKnowledge)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }
        public bool TryGetDestinationVersion(ItemChange sourceChange, out ItemChange destinationVersion)
        {
            destinationVersion = null;
            return false;

        }

        // Gets an object that can be used to retrieve item data from a replica. 
        public IChangeDataRetriever GetDataRetriever()
        {
            throw new NotImplementedException();
        }
        public void SaveItemChange(SaveChangeAction saveChangeAction, ItemChange change, SaveChangeContext context)
        {
            DataTransfer data = context.ChangeData as DataTransfer;

            switch (saveChangeAction)
            {
                case SaveChangeAction.Create:
                    {
                        ItemMetadata item = new ItemMetadata();
                        item.ItemId = change.ItemId;
                        item.ChangeVersion = change.ChangeVersion;
                        item.CreationVersion = change.CreationVersion;
                        item.Uri = data.Uri;

                        data.DataStream.Position = 0;
                        System.Diagnostics.Debug.WriteLine("Uploading File:" + item.Uri);
                        service.UploadFile(data.DataStream.Length, item, data.DataStream);

                        data.DataStream.Close();
                        sync.UpdateItemItem(item);

                        break;
                    }
                case SaveChangeAction.DeleteAndStoreTombstone:
                    {
                        ItemMetadata item = sync.GetItemMetaData(saveChangeAction, change, data);

                        sync.DeleteItem(change.ItemId);
                        service.DeleteFile(change.ItemId, item.Uri);
                        break;
                    }
                default:
                    {
                        throw new NotImplementedException(saveChangeAction + " ChangeAction is not implemented!");
                    }

            }

            context.GetUpdatedDestinationKnowledge(out myKnowledge, out myForgottenKnowledge);
        }
        public void StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgottenKnowledge)
        {

            sync.StoreKnowledgeForScope(knowledge, forgottenKnowledge);
            System.Diagnostics.Debug.WriteLine("    ### StoreKnowledgeForScope ###");
        }

        #endregion


    }
}
