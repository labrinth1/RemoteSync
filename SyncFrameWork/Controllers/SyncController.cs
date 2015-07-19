using Microsoft.Synchronization;

namespace SyncFrameWork.Controllers
{
   public class SyncController
    {
       public void Synchronize(KnowledgeSyncProvider destinationProvider, KnowledgeSyncProvider sourceProvider, ConflictResolutionPolicy destinationPol, ConflictResolutionPolicy sourcePol, SyncDirectionOrder SyncOrder, uint batchSize, string scopeName)
       {
           ((LocalStore)destinationProvider).RequestedBatchSize = batchSize;
           ((RemoteStore)sourceProvider).RequestedBatchSize = batchSize;

           destinationProvider.Configuration.ConflictResolutionPolicy = destinationPol;
           sourceProvider.Configuration.ConflictResolutionPolicy = sourcePol;

           SyncOrchestrator syncAgent = new SyncOrchestrator();

           syncAgent.LocalProvider = destinationProvider;
           syncAgent.RemoteProvider = sourceProvider;
           syncAgent.Direction = SyncOrder;

           syncAgent.Synchronize();
       }
    }
}
