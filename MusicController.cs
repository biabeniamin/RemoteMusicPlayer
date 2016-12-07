using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicAlarm
{
    public class MusicController
    {
        private MediaElement _mediaElement;
        private List<string> _files;
        private Random _random;
        private MediaPlaybackList _list;
        private string _serverAddress = "http://windphn.hi2.ro/Music/";
        private HttpServer _httpServer;
        private DispatcherTimer _timer;
        private int count = 0;
        public MusicController(MediaElement mediaElement)
        {
            Debug.WriteLine("app start");
            _mediaElement = mediaElement;
            _mediaElement.Volume = 0.35;
            _mediaElement.MediaOpened += _mediaElement_MediaOpened;
            _mediaElement.BufferingProgressChanged += _mediaElement_BufferingProgressChanged;
            _random = new Random();
            _httpServer = new HttpServer(80, RemoteCommand, CurrentTrack, NextExecution);
            _httpServer.StartServer();
            LoadList();
        }

        private void _mediaElement_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource != null)
                _httpServer.SetCurrentTrack(e.OriginalSource.ToString());
            else
                _httpServer.SetCurrentTrack(/*_mediaElement.Source.ToString() + */count.ToString());
            count++;
        }
        private void _mediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            /*if(e.OriginalSource!=null)
                _httpServer.SetCurrentTrack(e.OriginalSource.ToString());*/
        }

        private async Task<string> GetFilesIndex()
        {
            string filesIndex = await Helpers.ReadAFileOnServer(string.Format("{0}{1}",_serverAddress, "Files.info"));
            _files= Helpers.Split(filesIndex, ";-!-;");
            return "";
        }
        private void RemoteCommand(RemoteCommandAction remoteAction,int id)
        {
            switch(remoteAction)
            {
                case RemoteCommandAction.Play:
                    _mediaElement.Play();
                    _mediaElement.AutoPlay = true;
                    break;
                case RemoteCommandAction.Pause:
                    case RemoteCommandAction.Close:
                    _mediaElement.Pause();
                    break;
                case RemoteCommandAction.Next:
                    _list.MoveNext();
                    break;
                case RemoteCommandAction.Previous:
                    _list.MovePrevious();
                    break;
                case RemoteCommandAction.VolumeUp:
                    _mediaElement.Volume += 0.1;
                    break;
                case RemoteCommandAction.VolumeDown:
                    _mediaElement.Volume -= 0.1;
                    break;
                case RemoteCommandAction.PlaySelectedItem:
                    LoadSelectedFile(id);
                    break;
                case RemoteCommandAction.ReloadList:
                    LoadList();
                    break;
            }
        }
        private async void LoadSelectedFile(int id)
        {
            //string filesIndex = await Helpers.ReadAFileOnServer(string.Format("{0}{1}", _serverAddress, "CurentFile.info"));
            //_files = new List<string>();
            _list.ShuffleEnabled = false;
            _list.MoveTo((uint)id);
            Play();
        }
        private string GetRandomTrack()
        {
            string file = _files[_random.Next(0, _files.Count)];
            _httpServer.SetCurrentTrack(file);
            return file;
        }
        public string CurrentTrack()
        {
            string currentTrack = "";
            try
            {
                _mediaElement.Source.ToString();
            }
            catch
            {
                //
            }
            return currentTrack;
        }
        public string NextExecution()
        {
            string nextExecution = "";
            try
            {
                nextExecution = _timer.Interval.ToString("dd days hh:mm:ss");
            }
            catch
            {
                //
            }
            return nextExecution;
        }
        public void Play()
        {
            
            _mediaElement.Play();
            _mediaElement.AutoPlay = true;
            Debug.WriteLine("Play");
        }
        public void Execute()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += _timer_Tick;
            _timer.Interval = Helpers.GetNextDay(8,0,0);
            System.Diagnostics.Debug.WriteLine(_timer.Interval.TotalSeconds);
            //_timer.Start();
        }
        private void _timer_Tick(object sender, object e)
        {
            Debug.WriteLine("Timer Tick");
            Play();
            _timer.Interval = Helpers.GetNextDay();
        }
        private async void LoadList()
        {
            await GetFilesIndex();
            _list = new MediaPlaybackList();
            _httpServer.LoadList(_files);
            foreach (string file in _files)
            {
                Uri trackUri = new Uri(string.Format("{0}{1}", _serverAddress, file));
                
                _list.Items.Add(new MediaPlaybackItem(MediaSource.CreateFromUri(trackUri)));
            }
            Debug.WriteLine($"Tracks:{_list.Items.Count}");
            _list.ShuffleEnabled = true;
            _mediaElement.SetPlaybackSource(_list);
            
        }
    }
}
