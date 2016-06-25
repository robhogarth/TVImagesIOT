using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using TVImagesIot;
using System.Threading.Tasks;
using TVImagesIotSolution.Base;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.ApplicationInsights;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage.FileProperties;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TVImagesIoT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        const string settingsFilename = "settings.xml";
        DispatcherTimer dispatcherTimer;
        DispatcherTimer downloadTimer;

        int timerInt = 15;  //Time in Seconds
        int AsyncTimeout = 100;  //Timeout in Seconds eg 10

        int PicIndex = 0;
        bool imageReady = false;
        bool imageToggle = false;

        StorageFolder localfolder = ApplicationData.Current.LocalFolder;

        XMLTVDataIot TVImagesXML;
        AppSettings appSettings;

        AppServiceConnection _appServiceConnection;
        bool app2app = false;

        public MainPage()
        {
            this.InitializeComponent();

            LogEventSource.Log.Error("Initializing App");
            TVImagesXML = new XMLTVDataIot();     

            startLogText.Text += "Starting App" + System.Environment.NewLine;
            startLogText.Text += "Loading App Settings" + System.Environment.NewLine; 

            LoadAppSettings();
                        
            startLogText.Text += "Setting up Dispatcher Timers" + System.Environment.NewLine;
            DispatcherTimerSetup(false);
            DownloadTimerSetup(false);

#if DEBUG
            cmdbar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
#endif
            
            startLogText.Text += "Calling Startup method" + System.Environment.NewLine;

            startup();

            //Task task = Task.Run(() => startup());

            //TO DO:  Ensure main image has an image so screen is not black


        }
        private async void startup()
        {
            LogEventSource.Log.Debug("Starting up...");

            timerInt = appSettings.ImageRotateTimer;

            LogEventSource.Log.Debug("Deleting All Images");
            removeOldImages();

            LogEventSource.Log.Debug("Delete Data XML");
            removedataxml();
            
            startLogText.Text += "Loading Images from XML File" + System.Environment.NewLine;

            //Load XML File from the web on first load
            bool retval = await TVImagesXML.ReadObjectFromXMLWebAddress(appSettings.XMLWebAddress);

            LogEventSource.Log.Debug("Loaded " + TVImagesXML.Images.Count().ToString() + " images from XML file");
            startLogText.Text += "Images loaded - " + TVImagesXML.Images.Count + " images configured" + System.Environment.NewLine;

            startLogText.Visibility = Visibility.Collapsed;
            mainImage1.Visibility = Visibility.Visible;


            LogEventSource.Log.Debug("Starting DownloadImageListSync");
            //Load Images from Web on first load
            retval = await DownloadImageListSync(TVImagesXML, appSettings);
            LogEventSource.Log.Debug("Completed DownloadImageListSync");


            bool enableTimersDownload;
            bool enableTimersRotate;
            if (TVImagesXML != null)
            {
                enableTimersDownload = true;
                enableTimersRotate = true;
                loadImage(TVImagesXML.Images.ElementAt(PicIndex).URL);
            }
            else
            {
                LogEventSource.Log.Error("MainPage Initialized and TVImages is null.  Enabling download timer to attempt download at a later date to fix issues");
                
                enableTimersDownload = true;
                enableTimersRotate = false;
            }

            LogEventSource.Log.Debug("Starting dispatchers");
                    
            if (enableTimersDownload)
            {
                    downloadTimer.Start();
            }


            if (enableTimersRotate)
            {
                    dispatcherTimer.Start();
            }

            LogEventSource.Log.Debug("Startup Completed");
            //           InitAppSvc();
            //            SendAppMessage();

        }

        private async void removedataxml()
        {
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                
                IReadOnlyList<StorageFile> filesInFolder = await folder.GetFilesAsync();

                foreach (StorageFile file in filesInFolder)
                {
                    if ((file.FileType.Contains("xml")) & (!file.Name.Contains("settings")))
                    {
                        LogEventSource.Log.Debug("Attempting to delete - " + file.Name);
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }

                    if (file.FileType.Contains("log"))
                    {
                        BasicProperties bp = await file.GetBasicPropertiesAsync();
                        LogEventSource.Log.Debug("Log File - " + file.Name + " is this big: " + bp.Size);
                    }
                                       
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(string.Format("Error Removing old Images: {0}", ex.Message));
            }
        }

        private void LoadAppSettings()
        {
            LogEventSource.Log.Debug("Starting to read settings");

            // await Task.Run(() => AppSettings.ReadObjectFromXmlFileAsync<AppSettings>(settingsFilename);

            //AppSettings task;
                
               //await Task.Run(() => AppSettings.ReadObjectFromXmlFileAsync<AppSettings>(settingsFilename));
         //   while (task.IsCompleted != true)
           //     task.Wait(100);
           
            AppSettings newAppSettings = new AppSettings();
            
            newAppSettings.ReadObjectFromXmlFile(settingsFilename);


            //newAppSettings = task.Result;
            if (newAppSettings == null)
            {
                LogEventSource.Log.Debug("Read Settings failed, using object defaults");
            }
            else
            {
                LogEventSource.Log.Debug("Read Settings from file");
                appSettings = newAppSettings;
            }

            startLogText.Height = appSettings.ResolutionHeight;
            startLogText.Height = appSettings.ResolutionHeight;

            mainImage1.Height = appSettings.ResolutionHeight;
            mainImage2.Height = appSettings.ResolutionHeight;
            mainImage1.Width = appSettings.ResolutionWidth;
            mainImage2.Width = appSettings.ResolutionWidth;

            LogEventSource.Log.Debug("Read Settings Completed");
        }

        private async void InitAppSvc()
        {
            // Initialize the AppServiceConnection
            _appServiceConnection = new AppServiceConnection();
            _appServiceConnection.PackageFamilyName = "TVImagesWebApp_3zwxqbajh9e2y";
            _appServiceConnection.AppServiceName = "App2AppComService";

            // Send a initialize request 
            var res = await _appServiceConnection.OpenAsync();

            if (res == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                message.Add("Command", "Initialize");
                var response = await _appServiceConnection.SendMessageAsync(message);
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    throw new Exception("Failed to send message");
                }
                _appServiceConnection.RequestReceived += OnMessageReceived;
                app2app = true;
            }
        }

        private async void OnMessageReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;
            switch (command)
            {
                case "reloadSetting":
                {
                        LoadAppSettings();
                        break;
                }

                case "reloadxml":
                {
                        LogEventSource.Log.Debug("Clicking Refresh App Bar Button");
                        dispatcherTimer.Stop();
                        TVImagesXML = await DownloadImageList(TVImagesXML, appSettings, true);
                        dispatcherTimer.Start();
                        break;
                }

                case "updatelocation":
                {
                        appSettings.Location = message["Location"] as string;
                        await AppSettings.WriteObjectToXmlFileAsync(appSettings, settingsFilename);
                        LoadAppSettings();
                        break;
                }

            }

            var returnMessage = new ValueSet();
            returnMessage.Add("Status", "Success");
            var responseStatus = await args.Request.SendResponseAsync(returnMessage);

        }

        private async void SendAppMessage()
        {
            if (app2app)
            {
                string images = "";
                foreach (XMLImages TVImage in TVImagesXML.Images)
                {
                    images += "," + TVImage.Title;

                }

                string lastImage = TVImagesXML.Images[PicIndex - 1].Title;
                string nextImage = TVImagesXML.Images[PicIndex + 1].Title;

                var message = new ValueSet();
                message.Add("Command", "Data");
                message.Add("LastImage", lastImage);
                message.Add("NextImage", nextImage);
                message.Add("Location", appSettings.Location);
                message.Add("Images", images);

                var response = await _appServiceConnection.SendMessageAsync(message);
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    throw new Exception("Failed to send message");
                }
            }
        }

        public static async Task<XMLTVDataIot> DownloadImageList(XMLTVDataIot xmldata, AppSettings AP, bool ignoreTimeStamp = false)
        {
            try
            {
                
                XMLTVDataIot newxmldata = new XMLTVDataIot();
                Boolean doDownload = false;
                List<XMLImages> DeleteList = new List<XMLImages>();

                //deserialize object from web address

                bool ret = await newxmldata.ReadObjectFromXmlFile(AP.XMLWebAddress);                            

                //if timestamp is the same do nothing
                if (xmldata == null)
                {
                    doDownload = true;
                    LogEventSource.Log.Error("No Existing XML Data");                  
                }
                else
                {
                    if (DateTime.Compare(xmldata.Timestamp, newxmldata.Timestamp) == 0)
                    {
                        if (ignoreTimeStamp)
                            doDownload = true;
                        else
                            LogEventSource.Log.Debug("Timestamp matches current xml data no further downloads");
                    }
                    else
                        doDownload = true;
                }

                if (doDownload)
                {

                    LogEventSource.Log.Info("Starting Download");
                    
                    //if timestamp different then use new xml data
                    xmldata = newxmldata;

                    //for each image describe, download it                    
                    foreach (XMLImages image in xmldata.Images)
                    {

                        LogEventSource.Log.Info(string.Format("Starting Download on: {0}", image.Title));

                        //image.ReadObjectFromWebAddressAsync();
                        //as long as the location matches
                        if (image.LocationMatch(AP.Location))
                        {
                            //if location matches download file
                            LogEventSource.Log.Info(string.Format("{0}: Downloading File", image.Title));
                            string newFile = await XMLImagesIot.DownloadHTTPImage(image.URL, image.Title, AP.WebUsername, AP.WebPassword);

                            //check file exists locally (i.e. successful download).  If not remove image from the list
                            if (newFile != null)
                            {
                                image.URL = newFile;
                                LogEventSource.Log.Info(string.Format("{0}: Downloading File Completed", image.Title));
                            }
                            else
                                DeleteList.Add(image);

                        }
                        else
                        {
                            //if there is no location remove file from images list.  No download occurs
                            LogEventSource.Log.Info(string.Format("{0}: No Location match.  Removing file from images list", image.Title));
                            DeleteList.Add(image);
                        }
                    }


                    //remove files that dont match location or dont download
                    foreach (XMLImages dImage in DeleteList)
                    {
                        xmldata.Images.Remove(dImage);
                    }

                    removeOldImages(xmldata);

                    await XMLTVDataIot.SaveObjectToXml<XMLTVDataIot>(xmldata, AP.LocalXMLFileName);
                }
                return xmldata;

            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error("Error downloading from web address - " + ex.Source.ToString() + ": " + ex.Message);
                return xmldata;
            }
        }

        public async Task<bool> DownloadImageListSync(XMLTVDataIot xmldata, AppSettings AP, bool ignoreTimeStamp = false)
        {
            bool retval = false;

            try
            {        
                LogEventSource.Log.Debug("Starting DownloadImageListSync");

                LogEventSource.Log.Debug("Downloading xml file: " + appSettings.XMLWebAddress);

                Boolean doDownload = false;
                List<XMLImages> DeleteList = new List<XMLImages>();
                                
                //if xmldata is null or there are no images then dont download
                if ((xmldata == null) || (xmldata.Images.Count() == 0))
                {
                    doDownload = false;
                    LogEventSource.Log.Error("No Existing XML Data or no images");
                }
                else
                {
                        doDownload = true;
                }

                if (doDownload)
                {

                    LogEventSource.Log.Info("Starting Download");
                   
                    //for each image describe, download it                    
                    foreach (XMLImages image in xmldata.Images)
                    {

                        //LogEventSource.Log.Info(string.Format("Starting Download on: {0}", image.Title));

                        //image.ReadObjectFromWebAddressAsync();
                        //as long as the location matches
                        if (image.LocationMatch(AP.Location))
                        {
                            //if location matches download file
                            LogEventSource.Log.Info(string.Format("{0}: Downloading File", image.URL));
                            string newFile = await XMLImagesIot.DownloadHTTPImage(image.URL, image.Title, AP.WebUsername, AP.WebPassword);

                            //check file exists locally (i.e. successful download).  If not remove image from the list
                            if (newFile != null)
                            {
                                image.URL = newFile;
                                LogEventSource.Log.Info(string.Format("{0}: Downloading File Completed", image.Title));
                            }
                            else
                                DeleteList.Add(image);

                        }
                        else
                        {
                            //if there is no location remove file from images list.  No download occurs
                            LogEventSource.Log.Info(string.Format("{0}: No Location match.  Removing file from images list", image.Title));
                            DeleteList.Add(image);
                        }
                    }


                    //remove files that dont match location or dont download
                    foreach (XMLImages dImage in DeleteList)
                    {
                        xmldata.Images.Remove(dImage);
                    }

                    removeOldImages(xmldata);

                    await XMLTVDataIot.SaveObjectToXml<XMLTVDataIot>(xmldata, AP.LocalXMLFileName);
                }

                TVImagesXML.CopyProperties(xmldata);
                retval = true;

                //return xmldata;

            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error("Error downloading from web address - " + ex.Source.ToString() + ": " + ex.Message);
                TVImagesXML.CopyProperties(xmldata);
//                return xmldata;
            }

            return retval;
        }

        public static async void removeOldImages(XMLTVDataIot xmldata)
        {
            //Remove old images not in current xmldata
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder imagefolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("images", CreationCollisionOption.OpenIfExists);

                IReadOnlyList<StorageFile> filesInFolder = await imagefolder.GetFilesAsync();

                foreach (StorageFile file in filesInFolder)
                {
                    if (!xmldata.isFileinImages(file.Name))
                    {
                        LogEventSource.Log.Debug("Attempting to delete - " + file.Name);
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(string.Format("Error Removing old Images: {0}", ex.Message));
            }


        }

        public static async void removeOldImages()
        {
            //Remove all images mercilessly
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFolder imagefolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("images", CreationCollisionOption.OpenIfExists);

                IReadOnlyList<StorageFile> filesInFolder = await imagefolder.GetFilesAsync();

                foreach (StorageFile file in filesInFolder)
                {
                        LogEventSource.Log.Debug("Attempting to delete - " + file.Name);
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Error(string.Format("Error Removing old Images: {0}", ex.Message));
            }

        }


        public void DispatcherTimerSetup(bool enable)
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);

            if (enable)
                dispatcherTimer.Start();

            ImageRotateButton.IsChecked = dispatcherTimer.IsEnabled;
        }

        public void DownloadTimerSetup(bool enable)
        {
            downloadTimer = new DispatcherTimer();
            downloadTimer.Tick += downloadTimer_Tick;
            downloadTimer.Interval = new TimeSpan(0, 15, 0);

            if (enable)
                downloadTimer.Start();

            DownloadToggleButton.IsChecked = downloadTimer.IsEnabled;

        }

        void dispatcherTimer_Tick(object sender, object e)
        {
            try
            {

                while (!imageReady)
                {
                    LogEventSource.Log.Debug("Image not ready");
                }

                if (imageReady)
                {
                    if (imageToggle)
                    {
                        mainImage1.Visibility = Visibility.Visible;
                        mainImage2.Visibility = Visibility.Collapsed;
                        mainImage2.Source = null;
                        imageToggle = false;
                    }
                    else
                    {
                        mainImage2.Visibility = Visibility.Visible;
                        mainImage1.Visibility = Visibility.Collapsed;
                        mainImage1.Source = null;
                        imageToggle = true;
                    }

                    increasePicCount();

                    while (!isImageValidForLocation(TVImagesXML.Images.ElementAt(PicIndex)))
                    {
                        increasePicCount();
                    }

#if DEBUG
                pg1.Visibility = Visibility.Visible;
                pg1.Value = 0;
#endif
                    loadImage(TVImagesXML.Images.ElementAt(PicIndex).URL);
                    if (appSettings.IgnoreImageInterval)
                        dispatcherTimer.Interval = new TimeSpan(0, 0, timerInt);
                    else
                    {
                        if (TVImagesXML.Images.ElementAt(PicIndex).Interval > 0)
                            dispatcherTimer.Interval = new TimeSpan(0, 0, TVImagesXML.Images.ElementAt(PicIndex).Interval);
                        else
                            dispatcherTimer.Interval = new TimeSpan(0, 0, timerInt);
                    }
                }

                SendAppMessage();
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Critical("dispatchTimer_tick error - " + ex.Message);
            }

        }
        
        void increasePicCount()
        {
            try
            {
                PicIndex++;

                if (PicIndex > TVImagesXML.Images.Count - 1)
                    PicIndex = 0;
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Critical("IncreasePicCount error - " + ex.Message);
            }
        }

        bool isImageValidForLocation(XMLImages image)
        {
            
            bool retval = false;

            try
            {
                if (image.Target.Contains("All"))
                {
                    retval = true;
                }

                if (image.Target.Contains(appSettings.Location))
                {
                    retval = true;
                }
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Critical("isImageValidForLocation error - " + ex.Message);
            }

            return retval;

        }
        
        public async void downloadTimer_Tick(object sender, object e)
        {
            LogEventSource.Log.Debug("Starting downloadTimer_Tick.  Disabling dispatcherTimer while download proceeds");

            bool retval;

            dispatcherTimer.Stop();

            //LoadAppSettings();

            LogEventSource.Log.Debug("downloadTimer_Tick - Downloading XML from " + appSettings.XMLWebAddress);
            bool XMLretval = await TVImagesXML.ReadObjectFromXMLWebAddress(appSettings.XMLWebAddress);

            if (XMLretval)
            {
                LogEventSource.Log.Debug(String.Format("downloadTimer_Tick - XML downloaded successfully.  %1 images in definition", TVImagesXML.Images.Count().ToString()));
                LogEventSource.Log.Debug("downloadTimer_Tick - Starting DownloadImageListSync");
                retval = await DownloadImageListSync(TVImagesXML, appSettings);
            } 

            //TVImagesXML = await DownloadImageList(TVImagesXML, appSettings);

            LogEventSource.Log.Debug("Completed downloadTimer_Tick.  Enabling dispatcherTimer");
            dispatcherTimer.Start();
        }

        public void loadImage(string imageURI)
        {

            try
            {
                
                BitmapImage bitmapImage = new BitmapImage();
                
                bitmapImage.DecodePixelHeight = appSettings.ResolutionHeight;
                bitmapImage.DecodePixelWidth = appSettings.ResolutionWidth;

                bitmapImage.UriSource = new Uri(imageURI);

                imageReady = false;

                if (imageToggle)
                    mainImage1.Source = bitmapImage;
                else
                    mainImage2.Source = bitmapImage;
               
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Critical(string.Format("Failed in loadImage: {1}, {0} ", ex.Message, imageURI));
            }

        }

        private void mainImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            LogEventSource.Log.Critical(string.Format("Failed loading image: {0}, {1}", e.ErrorMessage, TVImagesXML.Images.ElementAt(PicIndex).Title));
            increasePicCount();
        }

        private async void AppBarButtonRefresh_Click(object sender, RoutedEventArgs e)
        {
            LogEventSource.Log.Debug("Clicking Refresh App Bar Button");

            downloadTimer_Tick(this, null);
            
        }

        private void AppBarButtonHelp_Click(object sender, RoutedEventArgs e)
        {
            LogEventSource.Log.Debug("Clicking Help App Bar Button");

        }

        private void AppBarToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (dispatcherTimer.IsEnabled)
                dispatcherTimer.Stop();
            else
                dispatcherTimer.Start();

            ImageRotateButton.IsChecked = dispatcherTimer.IsEnabled;
        }

        private void DownloadToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (downloadTimer.IsEnabled)
                downloadTimer.Stop();
            else
                downloadTimer.Start();

            DownloadToggleButton.IsChecked = downloadTimer.IsEnabled;
        }

        private void mainImage1_ImageOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                imageReady = true;
                pg1.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                LogEventSource.Log.Critical("ImageOpened error - " + ex.Message);
            }
        }
    }

}
