using System;
using System.Collections.Generic;
using System.IO;
using Common;
using Common.DTO;
using Microsoft.Synchronization;

namespace SyncService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "SyncService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select SyncService.svc or SyncService.svc.cs at the Solution Explorer and start debugging.
    [System.ServiceModel.ServiceBehavior()]
    public class SyncService : ISyncService
    {
        private const string RemoteDirectoryPath = @"c:\sync\Remote";

        public SyncKnowledge GetCurrentSyncKnowledge()
        {
            using (SyncDetails sync = new LocalSyncDetails(RemoteDirectoryPath, true))
            {
                return sync.SyncKnowledge;
            }
        }
        public bool SaveSyncSession(LocalSyncDetails localSync)
        {
            localSync.Save(RemoteDirectoryPath);

            return true;
        }

        public void UploadFile(RemoteFileInfo request)
        {
            FileInfo fi = new FileInfo(Path.Combine(RemoteDirectoryPath, request.Metadata.Uri));

            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            fi.Delete();

            int chunkSize = 2048;
            byte[] buffer = new byte[chunkSize];

            using (FileStream writeStream = new FileStream(fi.FullName, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                do
                {
                    int bytesRead = request.FileByteStream.Read(buffer, 0, chunkSize);
                    if (bytesRead == 0) break;

                    writeStream.Write(buffer, 0, bytesRead);
                } while (true);
            }
        }

        public void DeleteFile(SyncId itemID, string itemUri)
        {
            File.Delete(Path.Combine(RemoteDirectoryPath, itemUri));
        }

        public void StoreKnowledgeForScope(SyncKnowledge knowledge, ForgottenKnowledge forgotten)
        {
            using (SyncDetails sync = new LocalSyncDetails(RemoteDirectoryPath, false))
            {
                sync.StoreKnowledgeForScope(knowledge, forgotten);
            }

        }

        public byte[] GetChanges(uint batchSize, SyncKnowledge destinationKnowledge, LocalSyncDetails syncDetails)
        {
            ChangeBatchTransfer changeBatch = new ChangeBatchTransfer();
            object dataRetriver = new object();
            changeBatch.ChangeBatch = syncDetails.GetChangeBatch(RemoteDirectoryPath, batchSize, destinationKnowledge, out dataRetriver);
            changeBatch.ChangeDataRetriever = dataRetriver;

            return changeBatch.ObjectToByteArray();
        }

        public LocalSyncDetails LoadSyncSession()
        {
            return new LocalSyncDetails(RemoteDirectoryPath, true);
        }


        public Stream DownloadFile(string file)
        {
            return new FileStream(Path.Combine(RemoteDirectoryPath, file), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        #region TestClientDemo

        public void CreateFileTest()
        {
            new Common.TestClientDemo().CreateNewFile(RemoteDirectoryPath);
        }
        public void DeleteTestFile(string file)
        {
            new Common.TestClientDemo().DeleteFile(Path.Combine(RemoteDirectoryPath, file));
        }
        public string DownloadSingleFile(string filepath)
        {
            using (StreamReader sr = new StreamReader(Path.Combine(RemoteDirectoryPath, filepath)))
            {
                return sr.ReadToEnd();
            }
        }

        public void EditServerTextFile(string file, string text)
        {
            new TestClientDemo().EditTextFile(Path.Combine(RemoteDirectoryPath, file), text);

        }

        public List<FileInfo> GetServerFileInfo()
        {
            return new Common.TestClientDemo().GetFilesInfo(RemoteDirectoryPath);
        }



        #endregion
    }

}
