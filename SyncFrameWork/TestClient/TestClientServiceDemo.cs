using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using SyncFrameWork.SyncService;

namespace SyncFrameWork.TestClient
{
    /// <summary>
    /// This Class has nothing to do with functionallity just for demo purpose.
    /// </summary>
   public class TestClientServiceDemo
    {
       SyncServiceClient service;
       public TestClientServiceDemo(string endPoint)
       {
          service = new SyncServiceClient();
          ConfigureEndPoint(endPoint);
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
       public void Deletefileonserver(string filePath)
       {
           service.DeleteTestFile(filePath);
       }
       public void CreateRemoteFileDemo()
       {
           service.CreateFileTest();
       }
       public string GetTextFileContent(string filePath)
       {
           return service.DownloadSingleFile(filePath);
       }
       public List<System.IO.FileInfo> GetFiles()
       {
           return service.GetServerFileInfo().ToList();
       }
       public void EditServiceFile(string file, string text)
       {
            service.EditServerTextFile(file, text);
       }
    }
}
