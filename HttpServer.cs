using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace MusicAlarm
{
    public sealed class HttpServer : IDisposable
    {
        string _controlsHtmlString = @"<html><head><title>Blinky App</title></head><body>
        <a href='command=next'><input type='button' Value='Next'></a>
        <a href='command=previous'><input type='button' Value='Previous'></a>
        <a href='command=volumeUp'><input type='button' Value='Volume Up'></a>
        <a href='command=volumeDown'><input type='button' Value='Volume Down'></a>
        <a href='command=close'><input type='button' Value='Close'></a>
        <a href='command=play'><input type='button' Value='Play'></a>
        <a href='command=pause'><input type='button' Value='Pause'></a>
        <a href='command=playselected'><input type='button' Value='Play Selected'></a>
        <a href='command=reload'><input type='button' Value='Reload list'></a><br>";
        string _htmlString = "";
        private const uint _bufferSize = 8192;
        private int _port = 80;
        private Action<RemoteCommandAction,int> _action;
        private readonly StreamSocketListener listener;
        private Func<string> _getTrackFunction, _getNextExecutionFunction;
        private string _currentTrack;
        private List<string> _tracks = new List<string>();
        public HttpServer(int serverPort, Action<RemoteCommandAction,int> action, Func<string> getTrackFunction, Func<string> getNextExecutionFunction)
        {
            listener = new StreamSocketListener();
            _port = serverPort;
            _action = action;
            listener.ConnectionReceived += (s, e) => ProcessRequestAsync(e.Socket);
            _getTrackFunction = getTrackFunction;
            _getNextExecutionFunction = getNextExecutionFunction;

        }
        public void LoadList(List<string> tracks)
        {
            System.Diagnostics.Debug.WriteLine("Track uploaded in server.");
            _tracks = tracks;
            _htmlString = _controlsHtmlString;
            for (int i = 0; i < _tracks.Count; ++i)
            {
                _htmlString += $" < a href='command=playselected&id={i}'>{i}.{_tracks[i]}</a><br>";
            }
        }
        public void SetCurrentTrack(string currentTrack)
        {
            _currentTrack = currentTrack;
        }
        public void StartServer()
        {
            listener.BindServiceNameAsync(_port.ToString());
        }

        public void Dispose()
        {
            listener.Dispose();
        }
        private string GetHttpResponse()
        {
            StringBuilder httpResponse = new StringBuilder();
            httpResponse.AppendLine(string.Format("{0}<br>Current track:{1}<br>", _htmlString, _currentTrack));
            httpResponse.AppendLine(string.Format("Next execution:{0}<br>", _getNextExecutionFunction()));
            httpResponse.AppendLine(string.Format("Current time:{0}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")));
            return httpResponse.ToString();
        }
        private async void ProcessRequestAsync(StreamSocket socket)
        {
            try
            {
                // this works for text only
                StringBuilder request = new StringBuilder();
                using (IInputStream input = socket.InputStream)
                {
                    byte[] data = new byte[_bufferSize];
                    IBuffer buffer = data.AsBuffer();
                    uint dataRead = _bufferSize;
                    while (dataRead == _bufferSize)
                    {
                        await input.ReadAsync(buffer, _bufferSize, InputStreamOptions.Partial);
                        request.Append(Encoding.UTF8.GetString(data, 0, data.Length));
                        dataRead = buffer.Length;
                    }
                }

                using (IOutputStream output = socket.OutputStream)
                {
                    string requestMethod = request.ToString().Split('\n')[0];
                    string[] requestParts = requestMethod.Split(' ');

                    if (requestParts[0] == "GET")
                        await WriteResponseAsync(requestParts[1], output);
                    else
                        throw new InvalidDataException("HTTP method not supported: "
                                                       + requestParts[0]);
                }
            }
            catch(Exception ee)
            {
                System.Diagnostics.Debug.WriteLine($"ProcessRequestAsync -error:{ee.Message}");
            }
        }

        private async Task WriteResponseAsync(string request, IOutputStream os)
        {
            int parameter=0;
            System.Diagnostics.Debug.WriteLine("Processing request");
            RemoteCommandAction remoteAction = RemoteCommandAction.None;
            if (request.Contains("command=next"))
            {
                remoteAction = RemoteCommandAction.Next;
            }
            else if (request.Contains("command=previous"))
            {
                remoteAction = RemoteCommandAction.Previous;
            }
            else if (request.Contains("command=volumeUp"))
            {
                remoteAction = RemoteCommandAction.VolumeUp;
            }
            else if (request.Contains("command=volumeDown"))
            {
                remoteAction = RemoteCommandAction.VolumeDown;
            }
            else if (request.Contains("command=volumeDown"))
            {
                remoteAction = RemoteCommandAction.VolumeDown;
            }
            else if (request.Contains("command=reload"))
            {
                remoteAction = RemoteCommandAction.ReloadList;
            }
            else if (request.Contains("command=close"))
            {
                remoteAction = RemoteCommandAction.Close;
            }
            else if (request.Contains("command=playselected"))
            {
                string secv = "command=playselected&id=";
                for (int i = 0; i < request.Length; i++)
                {
                    bool isOk = true;
                    for (int j = i; j < request.Length && j < secv.Length; j++)
                    {
                        if(request[j]!=secv[j-i])
                        {
                            isOk = false;
                            break;
                        }
                    }
                    if(isOk)
                    {
                        string IdAsString = request.Substring(i + secv.Length);
                        parameter = Convert.ToInt32(IdAsString);
                        break;
                    }
                }
                remoteAction = RemoteCommandAction.PlaySelectedItem;
            }
            else if (request.Contains("command=play"))
            {
                remoteAction = RemoteCommandAction.Play;
            }
            else if (request.Contains("command=pause"))
            {
                remoteAction = RemoteCommandAction.Pause;
            }
            

            // Show the html 
            using (Stream resp = os.AsStreamForWrite())
            {
                // Look in the Data subdirectory of the app package
                byte[] bodyArray = Encoding.UTF8.GetBytes(GetHttpResponse());
                MemoryStream stream = new MemoryStream(bodyArray);
                string header = String.Format("HTTP/1.1 200 OK\r\n" +
                                  "Content-Length: {0}\r\n" +
                                  "Connection: close\r\n\r\n",
                                  stream.Length);
                byte[] headerArray = Encoding.UTF8.GetBytes(header);
                await resp.WriteAsync(headerArray, 0, headerArray.Length);
                await stream.CopyToAsync(resp);
                await resp.FlushAsync();
                if (remoteAction != RemoteCommandAction.None)
                {

                    SendCommand(remoteAction,parameter);
                }
            }
            

        }
        private async void SendCommand(RemoteCommandAction remoteAction,int id)
        {
            System.Diagnostics.Debug.WriteLine("Sending remote command");
            var dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                _action(remoteAction,id);
            });
        }
    }
}
