using System;
using System.Collections.Generic;
using System.IO;

namespace Common
{
    /// <summary>
    /// This Class has nothing to do with the sync functionallity just for demo purpose.
    /// </summary>
    public class TestClientDemo
    {
        public List<System.IO.FileInfo> GetFilesInfo(string folderPath)
        {
            List<System.IO.FileInfo> currentFiles = new List<FileInfo>();

            FindFiles(folderPath, currentFiles);
            return currentFiles;
        }

        private void FindFiles(string path, List<System.IO.FileInfo> fileInfo)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                FindFiles(dir.FullName, fileInfo);

            foreach (FileInfo fi in directoryInfo.GetFiles())
                fileInfo.Add(fi);
        }
        public void CreateNewFile(string path)
        {
            string text = CreateRandomText();
            System.IO.File.WriteAllText(System.IO.Path.Combine(path, text), text);
        }

        private string CreateRandomText()
        {
            return new Random().Next(0, 9999).ToString();
        }
        public void EditTextFile(string path, string text)
        {
            System.IO.File.WriteAllText(path, text);
        }

        public void DeleteFile(string filepath)
        {
            File.Delete(filepath);
        }
        public string ReadTextFile(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                return sr.ReadToEnd();
            }
        }
    }
}
