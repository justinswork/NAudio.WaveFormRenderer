using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NAudio.Wave;
using NAudio.WaveFormRenderer;

namespace WaveformRenderer_NetCore
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private string selectedFile;
		private string imageFile;
		private readonly WaveFormRenderer waveFormRenderer;
		private readonly WaveFormRendererSettings standardSettings;

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			waveFormRenderer = new WaveFormRenderer();

			standardSettings = new StandardWaveFormRendererSettings() { Name = "Standard" };
			var soundcloudOriginalSettings = new SoundCloudOriginalSettings() { Name = "SoundCloud Original" };

			var soundCloudLightBlocks = new SoundCloudBlockWaveFormSettings(Color.FromArgb(102, 102, 102), Color.FromArgb(103, 103, 103), Color.FromArgb(179, 179, 179),
				Color.FromArgb(218, 218, 218))
			{ Name = "SoundCloud Light Blocks" };

			var soundCloudDarkBlocks = new SoundCloudBlockWaveFormSettings(Color.FromArgb(52, 52, 52), Color.FromArgb(55, 55, 55), Color.FromArgb(154, 154, 154),
				Color.FromArgb(204, 204, 204))
			{ Name = "SoundCloud Darker Blocks" };

			var soundCloudOrangeBlocks = new SoundCloudBlockWaveFormSettings(Color.FromArgb(255, 76, 0), Color.FromArgb(255, 52, 2), Color.FromArgb(255, 171, 141),
				Color.FromArgb(255, 213, 199))
			{ Name = "SoundCloud Orange Blocks" };

			var topSpacerColor = Color.FromArgb(64, 83, 22, 3);
			var soundCloudOrangeTransparentBlocks = new SoundCloudBlockWaveFormSettings(Color.FromArgb(196, 197, 53, 0), topSpacerColor, Color.FromArgb(196, 79, 26, 0),
				Color.FromArgb(64, 79, 79, 79))
			{
				Name = "SoundCloud Orange Transparent Blocks",
				PixelsPerPeak = 2,
				SpacerPixels = 1,
				TopSpacerGradientStartColor = topSpacerColor,
				BackgroundColor = Color.Transparent
			};

			var topSpacerColor2 = Color.FromArgb(64, 224, 224, 224);
			var soundCloudGrayTransparentBlocks = new SoundCloudBlockWaveFormSettings(Color.FromArgb(196, 224, 225, 224), topSpacerColor2, Color.FromArgb(196, 128, 128, 128),
				Color.FromArgb(64, 128, 128, 128))
			{
				Name = "SoundCloud Gray Transparent Blocks",
				PixelsPerPeak = 2,
				SpacerPixels = 1,
				TopSpacerGradientStartColor = topSpacerColor2,
				BackgroundColor = Color.Transparent
			};


			BottomColor = System.Windows.Media.Color.FromArgb(standardSettings.BottomPeakPen.Color.A, standardSettings.BottomPeakPen.Color.R, standardSettings.BottomPeakPen.Color.G, standardSettings.BottomPeakPen.Color.B);
			TopColor = System.Windows.Media.Color.FromArgb(standardSettings.TopPeakPen.Color.A, standardSettings.TopPeakPen.Color.R, standardSettings.TopPeakPen.Color.G, standardSettings.TopPeakPen.Color.B);
			PeakStrategies.Add("Max Absolute Value");
			PeakStrategies.Add("Max Rms Value");
			PeakStrategies.Add("Sampled Peaks");
			PeakStrategies.Add("Scaled Average");
			_selectedPeakStrategy = PeakStrategies[0];

			RenderingStyles.Add(standardSettings);
			RenderingStyles.Add(soundcloudOriginalSettings);
			RenderingStyles.Add(soundCloudLightBlocks);
			RenderingStyles.Add(soundCloudDarkBlocks);
			RenderingStyles.Add(soundCloudOrangeBlocks);
			RenderingStyles.Add(soundCloudOrangeTransparentBlocks);
			RenderingStyles.Add(soundCloudGrayTransparentBlocks);

			SelectedRenderStyle = RenderingStyles[5];

			IsRendering = false;

			LoadAudioCommand = new SimpleCommand(() =>
			{
				var ofd = new OpenFileDialog();
				ofd.Filter = "MP3 Files|*.mp3|WAV files|*.wav";
				if (ofd.ShowDialog(this) == true)
				{
					selectedFile = ofd.FileName;
					RenderWaveform();
				}
			});
			LoadBackgroundImageCommand = new SimpleCommand(() =>
			{
				var ofd = new OpenFileDialog();
				ofd.Filter = "Image Files|*.bmp;*.png;*.jpg";
				if (ofd.ShowDialog() == true)
				{
					this.imageFile = ofd.FileName;
					RenderWaveform();
				}
			});

			RefreshCommand = new SimpleCommand(RenderWaveform);
			SaveCommand = new SimpleCommand(() =>
			{
				var sfd = new SaveFileDialog();
				sfd.Filter = "PNG files|*.png";
				if (sfd.ShowDialog(this) == true)
				{
					//pictureBox1.Image.Save(sfd.FileName);
				}
			});
		}

		private System.Windows.Media.Color _bottomColor;

		public System.Windows.Media.Color BottomColor
		{
			get { return _bottomColor; }
			set
			{
				_bottomColor = value;
				standardSettings.BottomPeakPen = new Pen(Color.FromArgb(value.A, value.R, value.G, value.B));
				RenderWaveform();
				OnPropertyChanged();
			}
		}

		private System.Windows.Media.Color _topColor;

		public System.Windows.Media.Color TopColor
		{
			get { return _topColor; }
			set 
			{ 
				_topColor = value;
				standardSettings.TopPeakPen = new Pen(Color.FromArgb(value.A, value.R, value.G, value.B));
				RenderWaveform();
				OnPropertyChanged();
			}
		}

		private string _selectedPeakStrategy;

		public string SelectedPeakStrategy
		{
			get { return _selectedPeakStrategy; }
			set { _selectedPeakStrategy = value; RenderWaveform(); OnPropertyChanged(); }
		}

		public ObservableCollection<string> PeakStrategies { get; } = new ObservableCollection<string>();

		private WaveFormRendererSettings _selectedRenderStyle;

		public WaveFormRendererSettings SelectedRenderStyle
		{
			get { return _selectedRenderStyle; }
			set { _selectedRenderStyle = value; RenderWaveform(); OnPropertyChanged(); }
		}

		private bool _isRendering;

		public bool IsRendering
		{
			get { return _isRendering; }
			set { _isRendering = value; OnPropertyChanged(); }
		}


		public ObservableCollection<WaveFormRendererSettings> RenderingStyles { get; } = new ObservableCollection<WaveFormRendererSettings>();

		private int _topHeight = 50;

		public int TopHeight
		{
			get { return _topHeight; }
			set { _topHeight = value; OnPropertyChanged(); }
		}

		private int _bottomHeight = 30;

		public int BottomHeight
		{
			get { return _bottomHeight; }
			set { _bottomHeight = value; OnPropertyChanged(); }
		}

		private int _imageWidth = 800;

		public int ImageWidth
		{
			get { return _imageWidth; }
			set { _imageWidth = value; OnPropertyChanged(); }
		}

		private bool _useDecibels;

		public bool UseDecibels
		{
			get { return _useDecibels; }
			set { _useDecibels = value; RenderWaveform(); OnPropertyChanged(); }
		}

		private int _blockSize = 200;

		public int BlockSize
		{
			get { return _blockSize; }
			set { _blockSize = value; OnPropertyChanged(); }
		}


		private System.Windows.Media.ImageSource? _imageSource;

		public System.Windows.Media.ImageSource? ImageSource
		{
			get { return _imageSource; }
			set { _imageSource = value; OnPropertyChanged(); }
		}

		public ICommand LoadAudioCommand { get; }
		public ICommand LoadBackgroundImageCommand { get; }
		public ICommand RefreshCommand { get; }
		public ICommand SaveCommand { get; }

		private WaveFormRendererSettings GetRendererSettings()
		{
			var settings = SelectedRenderStyle;
			settings.TopHeight = TopHeight;
			settings.BottomHeight = BottomHeight;
			settings.Width = ImageWidth;
			settings.DecibelScale = UseDecibels;
			return settings;
		}

		private IPeakProvider GetPeakProvider()
		{
			if (SelectedPeakStrategy == "Max Absolute Value")
				return new MaxPeakProvider();
			if (SelectedPeakStrategy == "Max Rms Value"){
				return new RmsPeakProvider(BlockSize);
			}
			if (SelectedPeakStrategy == "Sampled Peaks")
				return new SamplingPeakProvider(BlockSize);
			if (SelectedPeakStrategy == "Scaled Average")
				return new AveragePeakProvider(4);
			throw new InvalidOperationException("Unknown calculation strategy");

		}

		private void RenderWaveform()
		{
			if (selectedFile == null) return;
			var settings = GetRendererSettings();
			if (imageFile != null)
			{
				settings.BackgroundImage = new Bitmap(imageFile);
			}
			ImageSource = null;
			IsRendering = true;
			var peakProvider = GetPeakProvider();
			Task.Factory.StartNew(() => RenderThreadFunc(peakProvider, settings));
		}


		private void RenderThreadFunc(IPeakProvider peakProvider, WaveFormRendererSettings settings)
		{
			Image image = null;
			try
			{
				using (var waveStream = new AudioFileReader(selectedFile))
				{
					image = waveFormRenderer.Render(waveStream, peakProvider, settings);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message);
			}
			Dispatcher.Invoke(() => FinishedRender(image));
		}

		private void FinishedRender(Image image)
		{
			IsRendering = false;
			ImageSource = BitmapToImageSource(image);
		}

		BitmapImage BitmapToImageSource(Image bitmap)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
				memory.Position = 0;
				BitmapImage bitmapimage = new BitmapImage();
				bitmapimage.BeginInit();
				bitmapimage.StreamSource = memory;
				bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapimage.EndInit();

				return bitmapimage;
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}