using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Player.ViewModels
{
    public class PlaylistViewModel : ViewModelBase
    {
        private string path;
        public string Path
        {
            get => path;
            set => SetProperty(ref path, value);
        }

        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string length;
        public string Length
        {
            get => length;
            set => SetProperty(ref length, value);
        }
    }
}
