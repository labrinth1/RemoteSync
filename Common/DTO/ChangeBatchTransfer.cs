using System;
using Microsoft.Synchronization;

namespace Common.DTO
{
    [Serializable()]
    public class ChangeBatchTransfer
    {
        public ChangeBatch ChangeBatch { get; set; }
        public object ChangeDataRetriever { get; set; }
    }
}
