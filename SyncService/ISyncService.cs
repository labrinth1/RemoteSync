using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Microsoft.Synchronization;
using System.IO;
using Common;
using Common.DTO;

namespace SyncService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ISyncService" in both code and config file together.
    [ServiceContract]
    public interface ISyncService
    {
        [OperationContract]
        LocalSyncDetails LoadSyncSession();

        [OperationContract]
        bool SaveSyncSession(LocalSyncDetails localSync);

        [OperationContract]
        SyncKnowledge GetCurrentSyncKnowledge();

        [OperationContract]
        byte[] GetChanges(uint batchSize, SyncKnowledge destinationKnowledge, LocalSyncDetails dfs);

        [OperationContract]
        string DownloadSingleFile(string filepath);

        [OperationContract]
        Stream DownloadFile(string filepath);

        [OperationContract]
        void UploadFile(RemoteFileInfo request);

        [OperationContract]
        void DeleteFile(SyncId itemID, string itemUri);

        [OperationContract]
        void StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgotten);


        #region TestClientDemo

        [OperationContract]
        List<FileInfo> GetServerFileInfo();

        [OperationContract]
        void CreateFileTest();

        [OperationContract]
        void DeleteTestFile(string file);

        [OperationContract]
        void EditServerTextFile(string file, string text);

        #endregion
    }
}
