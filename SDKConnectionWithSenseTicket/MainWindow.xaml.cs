using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Qlik.Engine;
using Qlik.Sense.Communication.Communication.Security;
using Qlik.Sense.ExtendedFramework.Extensions;

namespace WpfGetSenseTicket
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Unmanaged code to retreive cookies
        [DllImport("wininet.dll", SetLastError = true)]
        public static extern bool InternetGetCookieEx(string url, string cookieName, StringBuilder cookieData, ref int size, Int32 dwFlags, IntPtr lpReserved);
        private const Int32 InternetCookieHttponly = 0x2000;

        public MainWindow()
        {
            InitializeComponent();
            Version.Content = "Not connected";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Version.Content = "Connecting";
            Connect.IsEnabled = false;
            ObjectBrowser.Navigate(Host.Text.ToString());
            ObjectBrowser.Navigated += OnNavigated;
        }

        private async void OnNavigated(object sender, NavigationEventArgs e)
        {
            Connect.IsEnabled = true;
            var uri = new Uri(Host.Text.ToString());
            Dictionary<string, string> cookies = new Dictionary<string, string>();

            // Extract the cookies from the webbrowser session.
            CookieContainer container = GetUriCookieContainer(uri);
            if (container != null)
            {
                CookieCollection cookie = container.GetCookies(uri);
                string newCookie = string.Empty;
                foreach (var c in cookie)
                {
                    newCookie += c.ToString();
                    cookies.Add(c.ToString().Split('=')[0], c.ToString().Split('=')[1]);
                }
            }

            ILocation location = await UseThisCookieToConnectWithTheSDK(cookies, uri);

            try
            {
                var hub = await location.HubAsync(noVersionCheck: true);
                Version.Content = await hub.ProductVersionAsync();
            }
            catch (Exception ex)
            {
                Version.Content = ex.Message;
            }
        }

        private async Task<ILocation> UseThisCookieToConnectWithTheSDK(Dictionary<string, string> cookies, Uri host)
        {
            var location = Location.FromUri(host);
            await location.AsNtlmUserViaProxyAsync(false);
            location.CustomUserCookies.AddRange(cookies);
            return location;
        }

        /// <summary>
        /// Gets the URI cookie container.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static CookieContainer GetUriCookieContainer(Uri uri)
        {
            CookieContainer cookies = null;
            // Determine the size of the cookie
            int datasize = 8192 * 16;
            StringBuilder cookieData = new StringBuilder(datasize);
            if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
            {
                if (datasize < 0)
                    return null;
                // Allocate stringbuilder large enough to hold the cookie
                cookieData = new StringBuilder(datasize);
                if (!InternetGetCookieEx(uri.ToString(), null, cookieData, ref datasize, InternetCookieHttponly, IntPtr.Zero))
                    return null;
            }
            if (cookieData.Length > 0)
            {
                cookies = new CookieContainer();
                cookies.SetCookies(uri, cookieData.ToString().Replace(';', ','));
            }
            return cookies;
        }
    }
}
