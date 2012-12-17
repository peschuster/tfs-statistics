using System.Windows.Media.Imaging;

namespace TfsStatisticsWpf.Models
{
    public class UserViewModel
    {
        internal UserViewModel(string name, IUserImageService imageService = null)
        {
            this.Name = name;

            this.Image = imageService == null 
                ? null 
                : imageService.GetUserImage(name);
        }

        public string Name { get; private set; }

        public BitmapImage Image { get; private set; }
    }
}
