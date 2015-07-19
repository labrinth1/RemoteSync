using System;
using Microsoft.Synchronization;

namespace Common
{
    [Serializable()]
   public class ItemMetadata
    {
        private SyncId itemId = null;
        private SyncVersion creationVersion = null;
        private SyncVersion changeVersion = null;
        private string uri = null;
        private bool isTombstone = false;
        private DateTime? lastWriteTimeUtc;

        public SyncId ItemId
        {
            get
            {
                if (itemId == null)
                    throw new InvalidOperationException("ItemId not yet set.");

                return itemId;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value", "ItemId cannot be null.");

                itemId = value;
            }
        }
        public SyncVersion CreationVersion
        {
            get { return creationVersion; }
            set { creationVersion = value; }
        }
        public SyncVersion ChangeVersion
        {
            get { return changeVersion; }
            set { changeVersion = value; }
        }
        public string Uri
        {
            get
            {
                if (uri == null)
                    throw new InvalidOperationException("Uri not yet set.");

                return uri;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value", "Uri cannot be null.");

                uri = value;
            }
        }
        public bool IsTombstone
        {
            get { return isTombstone; }
            set { isTombstone = value; }
        }
        public DateTime? LastWriteTimeUtc
        {
            get
            {
                if (!lastWriteTimeUtc.HasValue)
                {
                    return null;
                }
                else
                {
                    return lastWriteTimeUtc.Value;
                }
            }
            set
            {
                lastWriteTimeUtc = value;
            }
        }
        public ItemMetadata Clone()
        {
            return (ItemMetadata)this.MemberwiseClone();
        }
    }
}
