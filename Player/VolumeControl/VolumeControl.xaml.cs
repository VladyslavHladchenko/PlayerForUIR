using Player.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;


namespace Player
{
    /// <summary>
    /// Interaction logic for VolumeControl.xaml
    /// </summary>
    [ContentProperty("Volume")]
    public partial class VolumeControl : UserControl
    {
        private float prevVolume;
        private bool isVolumeOn;
        public RelayCommand ToggleVolumeCommand { get; }

        public VolumeControl()
        {
            ToggleVolumeCommand = new RelayCommand(ChangeVolumeIcon, () => true);
            isVolumeOn = true;
            InitializeComponent();
        }

        public float Volume
        {
            get
            {
                return (float)GetValue(VolumeProperty);
            }
            set
            {
                SetValue(VolumeProperty, value);
            }
        }
        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register("Volume",
            typeof(float), 
            typeof(VolumeControl), 
            new PropertyMetadata(20f));

        public string ImagePath
        {
            get
            {
                return (string)GetValue(ImagePathProperty);
            }
            set
            {
                SetValue(ImagePathProperty, value);
            }
        }
        public static readonly DependencyProperty ImagePathProperty = DependencyProperty.Register("ImagePath",
            typeof(string),
            typeof(VolumeControl),
            new PropertyMetadata("pack://application:,,,/Player;component/Images/volume_on.png"));

        private void ChangeVolumeIcon() {
            if (isVolumeOn)
            {
                isVolumeOn = false;
                prevVolume = Volume;
                Volume = 0f;
                ImagePath = "pack://application:,,,/Player;component/Images/volume_off.png";
            }
            else {
                isVolumeOn = true;
                Volume = prevVolume;
                ImagePath = "pack://application:,,,/Player;component/Images/volume_on.png";
            }
        }
    }
}
