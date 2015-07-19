using System;
using System.IO;
using Common;
using Microsoft.Synchronization;
using SyncFrameWork.SyncService;

namespace SyncFrameWork
{
    public class RemoteSyncDetails : Common.SyncDetails, IChangeDataRetriever
    {
        public RemoteSyncDetails(string folderPath, bool load)
            : base(folderPath, load)
        {
        }
        private SyncServiceClient service;
        
        public SyncServiceClient Service
        {
            get
            {  
                return service;
            }
            set
            {
                if (service == null)
                {
                    service = new SyncServiceClient();
                }
                service = value;
            }
        }
        public override object LoadChangeData(Microsoft.Synchronization.LoadChangeContext loadChangeContext)
        {
            ItemMetadata item;

            metadataStore.TryGetItem(loadChangeContext.ItemChange.ItemId, out item);

            if (item.IsTombstone)
            {
                throw new InvalidOperationException("Cannot load change data for a deleted item.");
            }

            System.Diagnostics.Debug.WriteLine("Downloading File:" + item.Uri);
            Stream dataStream = service.DownloadFile(item.Uri);

            DataTransfer transferMechanism = new DataTransfer(dataStream, item.Uri);

            return transferMechanism;
        }
    }
}
