using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Synchronization;
using SyncFrameWork.Controllers;
using SyncFrameWork.TestClient;
using Common;

namespace TestClient
{
    /// <summary>
    /// This is a quick small demonstrate how to use this application to sync files and folders over WCF. 
    /// Set your local Folder in the GUI and your remote folder in the service.svc file. And you should be good to go.
    /// 
    /// In debugging you can see the traceoutput of whats happening when the sync is running.
    /// After the sync completed you can see metadata about the files that was synced.
    /// 
    /// Please read more in the readme.txt in Read me folder.
    /// </summary>
    public partial class MainWindow : Window
    {
        TextBoxTraceListener textBoxListener;
        LocalStore localStore;
        RemoteStore remoteStore;

        TestClientDemo localDemo;
        TestClientServiceDemo serviceDemo;

        string endPoint;

        string localDirectoryPath;
        public MainWindow()
        {
            InitializeComponent();

            ApplicationStartUp();
        }

        private void ApplicationStartUp()
        {
            localDirectoryPath = txtLocalStore.Text;
            endPoint = txtEndPoint.Text;

           localDemo = new Common.TestClientDemo();
           serviceDemo = new TestClientServiceDemo(endPoint);

            if (!Directory.Exists(localDirectoryPath))
                Directory.CreateDirectory(localDirectoryPath);

            LoadStores();
            
            lstConflictresolutionPolicy.ItemsSource = Enum.GetValues(typeof(ConflictResolutionPolicy)).Cast<ConflictResolutionPolicy>();
            lstSyncDirection.ItemsSource = Enum.GetValues(typeof(SyncDirectionOrder)).Cast<SyncDirectionOrder>();
            lstConflictresolutionPolicy.SelectedIndex = 0;
            lstSyncDirection.SelectedIndex = 0;

            textBoxListener = new TextBoxTraceListener(txtTraceListner);
            Trace.Listeners.Add(textBoxListener);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new SyncController().Synchronize(localStore, remoteStore, (ConflictResolutionPolicy)lstConflictresolutionPolicy.SelectedItem, (ConflictResolutionPolicy)lstConflictresolutionPolicy.SelectedItem, (SyncDirectionOrder)lstSyncDirection.SelectedItem, 100, "GENERIC_SCOPE");
            Reload();
        }

        private void btnLocalFile_Click(object sender, RoutedEventArgs e)
        {
            localDemo.CreateNewFile(localDirectoryPath);
            LoadDemoFiles();
        }
       

        private void btnRemoteFile_Click(object sender, RoutedEventArgs e)
        {
            serviceDemo.CreateRemoteFileDemo();
            LoadDemoFiles();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if ((bool)txtEdit.Tag && lstLocal.SelectedItem != null)
                localDemo.EditTextFile(Path.Combine(localDirectoryPath, lstLocal.SelectedItem.ToString()), txtEdit.Text);
            else if (lstRemote.SelectedItem != null)
                serviceDemo.EditServiceFile(lstRemote.SelectedItem.ToString(), txtEdit.Text);

            LoadDemoFiles();
        }

        private void btn_DeletelocalFile_Click(object sender, RoutedEventArgs e)
        {
            localDemo.DeleteFile(Path.Combine(localDirectoryPath, lstLocal.SelectedItem.ToString()));
            LoadDemoFiles();
        }

        private void btn_DeleteRemoteFile_Click(object sender, RoutedEventArgs e)
        {
            serviceDemo.Deletefileonserver(lstRemote.SelectedItem.ToString());
            LoadDemoFiles();
        }

        private void lstLocal_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstLocal.SelectedItem != null)
            txtEdit.Text = localDemo.ReadTextFile(Path.Combine(localDirectoryPath, lstLocal.SelectedItem.ToString()));

            txtEdit.Tag = true;
        }

        private void lstRemote_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lstRemote.SelectedItem != null)
            txtEdit.Text = serviceDemo.GetTextFileContent(lstRemote.SelectedItem.ToString());
        }

        #region LoadDemo
        /// <summary>
        /// This loads metadata for the sync same information that get stored in file.sync
        /// </summary>
        private void Reload()
        {
            dgLocal.ItemsSource = localStore.sync.MetaDataStore;
            dgRemote.ItemsSource = remoteStore.sync.MetaDataStore;
            LoadStores();
        }
        private void LoadStores()
        {
            localStore = new LocalStore(localDirectoryPath);
            remoteStore = new RemoteStore(endPoint);

            LoadDemoFiles();
        }

        private void LoadDemoFiles()
        {
            lstRemote.ItemsSource = serviceDemo.GetFiles();
            lstLocal.ItemsSource = localDemo.GetFilesInfo(localDirectoryPath);
        }

        #endregion

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            txtTraceListner.Text = string.Empty;
        }

    }
}
