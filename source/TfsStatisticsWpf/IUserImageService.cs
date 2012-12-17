using System.Windows.Media.Imaging;

namespace TfsStatisticsWpf
{
    public interface IUserImageService
    {
        BitmapImage GetUserImage(string userName);
    }
}
