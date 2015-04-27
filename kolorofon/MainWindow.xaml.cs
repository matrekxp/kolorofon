using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kolorofon
{
    public partial class MainWindow : Window
    {
        private AudioPlayback audioPlayback;

        private string selectedFile;

        public ICommand OpenFileCommand { get; private set; }
        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand StopCommand { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            this.audioPlayback = new AudioPlayback();
            audioPlayback.FftCalculated += audioGraph_FftCalculated;

            PlayCommand = new DelegateCommand(Play);
            OpenFileCommand = new DelegateCommand(OpenFile);
            StopCommand = new DelegateCommand(Stop);
            PauseCommand = new DelegateCommand(Pause);

            DataContext = this;
        }

        void audioGraph_FftCalculated(object sender, FftEventArgs e)
        {
            spectrumAnalyser.Update(e.Result, e.currentFilterIndex);
        }

        private void Pause()
        {
            audioPlayback.Pause();
        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Supported Files (*.wav;*.mp3)|*.wav;*.mp3|All Files (*.*)|*.*";
            bool? result = openFileDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                this.selectedFile = openFileDialog.FileName;
                audioPlayback.Load(this.selectedFile);
            }
        }

        private void Play()
        {
            if (this.selectedFile == null)
            {
                OpenFile();
            }
            if (this.selectedFile != null)
            {
                audioPlayback.Play();
            }
        }

        private void Stop()
        {
            audioPlayback.Stop();
        }

        public void Dispose()
        {
            audioPlayback.Dispose();
        }
    }
}
