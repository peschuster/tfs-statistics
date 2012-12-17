using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace TfsStatisticsWpf
{
    internal class DirectoryUserImageService : IUserImageService
    {
        private readonly DirectoryEntry entry;

        private readonly string imageProperty;

        public DirectoryUserImageService(string domainController, string imageProperty)
        {
            this.imageProperty = imageProperty;

            this.entry = string.IsNullOrEmpty(domainController)
                         ? null
                         : new DirectoryEntry("LDAP://" + domainController);
        }

        public BitmapImage GetUserImage(string userName)
        {
            using (DirectorySearcher dsSearcher = new DirectorySearcher(this.entry))
            {
                dsSearcher.Filter = "(&(objectClass=user) (cn=" + userName.Split('\\').Last() + "*))";
                SearchResult result = dsSearcher.FindOne();

                if (result == null)
                    return null;

                using (DirectoryEntry user = new DirectoryEntry(result.Path))
                {
                    byte[] data = user.Properties[this.imageProperty].Value as byte[];

                    if (data == null)
                        return null;
                    
                    using (var stream = new MemoryStream(data))
                    {
                        BitmapImage image = new BitmapImage();

                        image.BeginInit();
                        image.StreamSource = stream;
                        image.EndInit();

                        return image;
                    }
                }
            }
        }
    }
}
