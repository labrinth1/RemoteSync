
namespace Common
{
    public class DataTransfer
    {
        private System.IO.Stream dataStream;
        private string uri;

        public DataTransfer(System.IO.Stream dataStream)
        {
            this.dataStream = dataStream;
        }

        public DataTransfer(System.IO.Stream dataStream, string uri)
        {
            this.dataStream = dataStream;
            this.uri = uri;
        }

        public System.IO.Stream DataStream
        {
            get { return dataStream; }
        }

        public string Uri
        {
            get { return uri; }
        }
    }
}
