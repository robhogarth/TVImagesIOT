using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.AppService;
using Windows.System.Threading;
using Windows.Networking.Sockets;
using System.IO;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace TVImagesWebApp
{
    public sealed class StartupTask : IBackgroundTask
    {

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // Associate a cancellation handler with the background task. 
            taskInstance.Canceled += OnCanceled;

            // Get the deferral object from the task instance
            _serviceDeferral = taskInstance.GetDeferral();

            /*
            HttpServer server = new HttpServer(80, _appServiceConnection);
            IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                (workItem) =>
                {
                    server.StartServer();
                });
*/
            
            var appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            if (appService != null &&
                appService.Name == "App2AppComService")
            {
                _appServiceConnection = appService.AppServiceConnection;
                _appServiceConnection.RequestReceived += OnRequestReceived;
            }

            
        }

        private async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;

            switch (command)
            {
                case "Initialize":
                    {
                        var messageDeferral = args.GetDeferral();
                        //Set a result to return to the caller
                        var returnMessage = new ValueSet();
                        HttpServer server = new HttpServer(8000, _appServiceConnection);
                        IAsyncAction asyncAction = Windows.System.Threading.ThreadPool.RunAsync(
                            (workItem) =>
                            {
                                server.StartServer();
                            });
                        returnMessage.Add("Status", "Success");
                        var responseStatus = await args.Request.SendResponseAsync(returnMessage);
                        messageDeferral.Complete();
                        break;
                    }

                case "Quit":
                    {
                        //Service was asked to quit. Give us service deferral
                        //so platform can terminate the background task
                        _serviceDeferral.Complete();
                        break;
                    }
            }
        }
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //Clean up and get ready to exit
        }

        BackgroundTaskDeferral _serviceDeferral;
        AppServiceConnection _appServiceConnection;
    }

    public sealed class HttpServer : IDisposable
    {
        string offHtmlString = "<html><head><title>Blinky App</title></head><body><form action=\"blinky.html\" method=\"GET\"><input type=\"radio\" name=\"state\" value=\"on\" onclick=\"this.form.submit()\"> On<br><input type=\"radio\" name=\"state\" value=\"off\" checked onclick=\"this.form.submit()\"> Off</form></body></html>";
        //string onHtmlString = "<html><head><title>Blinky App</title></head><body><form action=\"blinky.html\" method=\"GET\"><input type=\"radio\" name=\"state\" value=\"on\" checked onclick=\"this.form.submit()\"> On<br><input type=\"radio\" name=\"state\" value=\"off\" onclick=\"this.form.submit()\"> Off</form></body></html>";
        string onHtmlString = "<html><head><title>TVImages Web App</title></head><body>What up!</body></html>";

        private const uint BufferSize = 8192;
        private int port = 8000;
        private readonly StreamSocketListener listener;
        private AppServiceConnection appServiceConnection;
        private string lastImage = "";
        private string nextImage = "";
        private string location = "";

        public HttpServer(int serverPort, AppServiceConnection _appServiceConnection)
        {
            listener = new StreamSocketListener();
            port = serverPort;
            appServiceConnection = _appServiceConnection;
            appServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
        }

        private void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string command = message["Command"] as string;

            switch (command)
            {
                case "Initialize":
                    {
                        break;
                    }

                case "Data":
                    {
                        lastImage = message["lastImage"] as string;
                        nextImage = message["nextImage"] as string;
                        location = message["location"] as string;
                        break;
                    }
            }

        }
                    

        public void StartServer()
        {
#pragma warning disable CS4014
            listener.BindServiceNameAsync(port.ToString());
#pragma warning restore CS4014
        }

        public void Dispose()
        {
            listener.Dispose();
        }

        private async void ProcessRequestAsync(StreamSocket socket)
        {
            // this works for text only
            StringBuilder request = new StringBuilder();
            using (IInputStream input = socket.InputStream)
            {
                byte[] data = new byte[BufferSize];
                IBuffer buffer = data.AsBuffer();
                uint dataRead = BufferSize;
                while (dataRead == BufferSize)
                {
                    await input.ReadAsync(buffer, BufferSize, InputStreamOptions.Partial);
                    request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                    dataRead = buffer.Length;
                }
            }

            using (IOutputStream output = socket.OutputStream)
            {
                string requestMethod = request.ToString().Split('\n')[0];
                string[] requestParts = requestMethod.Split(' ');

                if (requestParts[0] == "GET")
                    //await WriteResponseAsync(requestMethod, output);
                    await WriteFileAsync(requestParts[1], output);
                else
                    throw new InvalidDataException("HTTP method not supported: "
                                                   + requestParts[0]);
            }
        }

        private async Task WriteFileAsync(string file, IOutputStream os)
        {
            file = file.Substring(1);
                        
            if (file == "")
            {
                file = "index.html";
            }

            byte[] bodyArray = null;
            byte[] headerArray = null;
            MemoryStream stream = null;
            
            using (Stream resp = os.AsStreamForWrite())
            {

                try
                {


                    Uri uri = new Uri("ms-appx:///Assets/" + file);
                    StorageFile sfile = await StorageFile.GetFileFromApplicationUriAsync(uri);

                    if (!sfile.IsAvailable)
                    {
                        string header = "HTTP/1.1 404 OK\r\nConnection: close\r\n\r\n";
                        headerArray = Encoding.UTF8.GetBytes(header);
                        await resp.WriteAsync(headerArray, 0, headerArray.Length);
                        await resp.FlushAsync();
                    }


                    Stream filestream = await sfile.OpenStreamForReadAsync();

                    string fileext = file.Substring(file.LastIndexOf(".") + 1);



                    // Show the html 

                    if (fileext == "html")
                    {
                        bodyArray = new byte[filestream.Length];
                        
                        filestream.Read(bodyArray, 0, (int)filestream.Length);
                        string htmlbody = bodyArray.ToString();
                        htmlbody = htmlbody.Replace("%location%", location);
                        htmlbody = htmlbody.Replace("%lastImage%", lastImage);
                        htmlbody = htmlbody.Replace("%nextImage%", nextImage);

                        bodyArray = new byte[htmlbody.Length];
                        System.Buffer.BlockCopy(htmlbody.ToCharArray(), 0, bodyArray, 0, htmlbody.Length);

                        //bodyArray = Encoding.UTF8.GetBytes(filebuf);
                        string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                                        "Content-Type: text/html; charset=utf-8" +
                                                        "Content-Length: {0}\r\n" +
                                                        "Connection: close\r\n\r\n",
                        filestream.Length);
                        headerArray = Encoding.UTF8.GetBytes(header);
                        stream = new MemoryStream(bodyArray);


                    }

                    if (fileext == "png")
                    {
                        byte[] filebuf = new byte[filestream.Length];
                        filestream.Read(filebuf, 0, (int)filestream.Length);

                        string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                                        "Content-Length: {0}\r\n" +
                                                        "Content-Type: image/png\r\n" +
                                                        "Content-Disposition: inline; filename=\"{1};\"\r\n" +
                                                        "Connection: close\r\n\r\n",
                        filestream.Length, file);
                        headerArray = Encoding.UTF8.GetBytes(header);
                        stream = new MemoryStream(filebuf);

                    }

                    if (headerArray.Length > 0)
                    {
                        await resp.WriteAsync(headerArray, 0, headerArray.Length);
                        await stream.CopyToAsync(resp);
                        await resp.FlushAsync();

                    }
                }
                catch
                {
                    string header = "HTTP/1.1 500 OK\r\nConnection: close\r\n\r\n";
                    headerArray = Encoding.UTF8.GetBytes(header);
                    await resp.WriteAsync(headerArray, 0, headerArray.Length);
                    await resp.FlushAsync();
                }
                
            }

        }

        private async Task WriteResponseAsync(string request, IOutputStream os)
        {
            string html = request;
                       
            // Show the html 
            using (Stream resp = os.AsStreamForWrite())
            {
                
                // Look in the Data subdirectory of the app package
                byte[] bodyArray = Encoding.UTF8.GetBytes(html);
                MemoryStream stream = new MemoryStream(bodyArray);
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                  "Content-Length: {0}\r\n" +
                                  "Connection: close\r\n\r\n",
                                  stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
            }

        }
    }

}
