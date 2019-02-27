using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;
using System.Drawing;
using WaveFormRendererLib;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using System.Threading;
using System.IO;

namespace Player.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private string inputPath;
        private string defaultDecompressionFormat;
        private IWavePlayer wavePlayer;
        private WaveStream reader;
        private readonly WaveFormRenderer waveFormRenderer;
        private string songLength;
        private string songPosition;

        public RelayCommand LoadCommand { get; }
        public RelayCommand PlayCommand { get; }
        public RelayCommand PauseCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand AddToPlaylistCommand { get; }
        public RelayCommand RemoveFromPlaylistCommand { get; }
        
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private double sliderPosition;
        private float currentVolume;
        private readonly ObservableCollection<string> inputPathHistory;
        private string lastPlayed;
        private ObservableCollection<PlaylistViewModel> playlist = new ObservableCollection<PlaylistViewModel>();
        private PlaylistViewModel selectedSongInPlaylist;
        
        private string imagePath;
        public MainWindowViewModel()
        {
            waveFormRenderer = new WaveFormRenderer();
            inputPathHistory = new ObservableCollection<string>();
            LoadCommand = new RelayCommand(Load, () => IsStopped);
            PlayCommand = new RelayCommand(Play, () => !IsPlaying);
            PauseCommand = new RelayCommand(Pause, () => IsPlaying);
            StopCommand = new RelayCommand(Stop, () => !IsStopped);
            AddToPlaylistCommand = new RelayCommand(AddToPlaylist, () => true);
            RemoveFromPlaylistCommand = new RelayCommand(RemoveFromPlaylist, () => SelectedSongInPlaylist!=null && Playlist.Count!=0);

            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += TimerOnTick;
            CurrentVolume = 20;
            ImagePath = "pack://application:,,,/Player;component/Images/Line.png";
        }


        public bool IsPlaying => wavePlayer != null && wavePlayer.PlaybackState == PlaybackState.Playing;
        public bool IsStopped => wavePlayer == null || wavePlayer.PlaybackState == PlaybackState.Stopped;
        public IEnumerable<string> InputPathHistory => inputPathHistory;

        public string ImagePath
        {
            get => imagePath;
            set
            {
                imagePath = value;
                OnPropertyChanged("ImagePath");
            }
        }

        private const double SliderMax = 10.0;
        private void TimerOnTick(object sender, EventArgs eventArgs)
        {
            if (reader == null) return;

            sliderPosition = Math.Min(SliderMax, reader.Position * SliderMax / reader.Length);
            SongPosition = TimeSpan.FromMilliseconds(reader.TotalTime.TotalMilliseconds * sliderPosition / SliderMax).ToString().Substring(0,8);
            OnPropertyChanged("SliderPosition");
            OnPropertyChanged("SongPosition");
        }

        public ObservableCollection<PlaylistViewModel> Playlist
        {
            get => playlist;
            set => SetProperty(ref playlist, value);
        }

        public PlaylistViewModel SelectedSongInPlaylist
        {
            get => selectedSongInPlaylist;
            set => SetProperty(ref selectedSongInPlaylist, value);
        }

        public double SliderPosition
        {
            get => sliderPosition;
            set
            {
                if (sliderPosition == value) return;

                sliderPosition = value;
                if (reader != null)
                {
                    var pos = (long)(reader.Length * sliderPosition / SliderMax);
                    reader.Position = pos;
                }
                OnPropertyChanged("SliderPosition");
            }
        }

        public string SongLength
        {
            get => songLength;
            set
            {
                SetProperty(ref songLength, value);
                OnPropertyChanged("SongLength");
            }
        }

        public string SongPosition
        {
            get => songPosition;
            set
            {
                SetProperty(ref songPosition, value);
                OnPropertyChanged("SongPosition");
            }
        }


        public float CurrentVolume
        {
            get => currentVolume;
            set {
                currentVolume = value;
                if (wavePlayer != null)
                {
                    wavePlayer.Volume = currentVolume/100;
                }
                OnPropertyChanged("CurrentVolume");
            }
        }

        private void SelectInputFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "All Supported Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenInputFile(openFileDialog.FileName);
            }
        }

        private bool OpenInputFile(string file)
        {
            bool isValid = false;
            try
            {
                using (var tempReader = new MediaFoundationReader(file))
                {
                    InputPath = file;
                    isValid = true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Not a supported input file ({e.Message})");
            }

            return isValid;
        }

        public string DefaultDecompressionFormat
        {
            get => defaultDecompressionFormat;
            set
            {
                defaultDecompressionFormat = value;
                OnPropertyChanged("DefaultDecompressionFormat");
            }
        }

        public string InputPath
        {
            get => inputPath;
            set
            {
                if (inputPath != value)
                {
                    inputPath = value;
                    AddToHistory(value);
                    OnPropertyChanged("InputPath");
                    if (!IsStopped)
                        Stop();
                    RenderWaveform();
                    reader?.Dispose();
                    if (wavePlayer == null)
                    {
                        CreatePlayer();
                    }

                    reader = new MediaFoundationReader(inputPath);
                    lastPlayed = inputPath;
                    wavePlayer.Init(reader);
                    
                    DefaultDecompressionFormat = reader.WaveFormat.ToString();
                    SongLength = reader.TotalTime.ToString().Substring(0, 8);
                    SongPosition = "00:00:00";

                }
            }
        }

        private void AddToHistory(string value)
        {
            if (!inputPathHistory.Contains(value))
            {
                inputPathHistory.Add(value);
            }
        }

        private void AddToPlaylist()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "All Supported Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                using (var tempReader = new MediaFoundationReader(openFileDialog.FileName))
                {
                    DefaultDecompressionFormat = tempReader.WaveFormat.ToString();
                    Playlist.Add(new PlaylistViewModel() {
                        Path = openFileDialog.FileName,
                        Name = openFileDialog.SafeFileName,
                        Length = tempReader.TotalTime.ToString().Substring(0, 8)
                    });
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Not a supported input file ({e.Message})");
            }
        }

        private void RemoveFromPlaylist()
        {
            Playlist.Remove(SelectedSongInPlaylist);
        }

        private void Stop()
        {
            if (wavePlayer == null) return;

            wavePlayer.Stop();
            SongPosition = SongPosition = "00:00:00";
        }

        private void Pause()
        {
            if (wavePlayer == null) return;

            wavePlayer.Pause();
            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsStopped");
        }

        private void Play()
        {
            if (string.IsNullOrEmpty(InputPath))
            {
                MessageBox.Show("Select a valid input file first");
                return;
            }
            if (wavePlayer == null)
            {
                CreatePlayer();
            }
            if (lastPlayed != inputPath && reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            if (reader == null)
            {
                reader = new MediaFoundationReader(inputPath);
                lastPlayed = inputPath;
                wavePlayer.Init(reader);
            }
            wavePlayer.Volume = currentVolume/100;
            wavePlayer.Play();
            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsStopped");
            timer.Start();
        }

        private void CreatePlayer()
        {
            wavePlayer = new WaveOutEvent();
            wavePlayer.PlaybackStopped += WavePlayerOnPlaybackStopped;  
        }

        private void WavePlayerOnPlaybackStopped(object sender, StoppedEventArgs stoppedEventArgs)
        {

            if (reader != null)
            {
                SliderPosition = 0;
                timer.Stop();
            }
            if (stoppedEventArgs.Exception != null)
            {
                MessageBox.Show(stoppedEventArgs.Exception.Message, "Error Playing File");
            }
            OnPropertyChanged("IsPlaying");
            OnPropertyChanged("IsStopped");
        }

        private void Load()
        {
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            SelectInputFile();
        }

        public void Dispose()
        {
            wavePlayer?.Dispose();
            reader?.Dispose();
        }

        public void SetSongAsCurrent()
        {
            if (SelectedSongInPlaylist != null && SelectedSongInPlaylist.Path != InputPath)
            {
                InputPath = SelectedSongInPlaylist.Path;
            }
            else if (SelectedSongInPlaylist == null)
            {
                AddToPlaylist();
            }
            SelectedSongInPlaylist = null;
        }

        private void RenderWaveform()
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "waveForms"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "waveForms");

            if (inputPath == null) return;
            var settings = new SoundCloudOriginalSettings
            {
                TopHeight = 40,
                BottomHeight = 40,
                Width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width
            };
            ImagePath = null;
            var peakProvider = new RmsPeakProvider(300);
        
            string path = AppDomain.CurrentDomain.BaseDirectory + "waveForms" + Path.DirectorySeparatorChar + inputPath.GetHashCode().ToString() + ".png";

            if(!File.Exists(path))
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Image image = null;

                    image = waveFormRenderer.Render(inputPath, peakProvider, settings);
                    image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    ImagePath = path;
                }).Start();
            else
                ImagePath = path;

        }
    }
}