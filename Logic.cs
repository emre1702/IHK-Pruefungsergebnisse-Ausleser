using HtmlAgilityPack;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Xml.Serialization;

namespace PruefungsErgebnisse
{
    internal class Logic
    {
        #region Private Fields

        private Config _config;
        private CookieContainer _cookieContainer;
        private Process _lastProcess;
        private LoginInfos _loginInfos;

        #endregion Private Fields

        #region Internal Constructors

        internal Logic()
        {
            _cookieContainer = new CookieContainer();
        }

        #endregion Internal Constructors

        #region Internal Methods

        internal bool AddSessionId()
        {
            string url = "https://ausbildung.ihk.de/pruefungsinfos/Peo/Willkommen.aspx?knr=155";

            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            webRequest.Host = "ausbildung.ihk.de";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36";
            webRequest.CookieContainer = _cookieContainer;

            var response = webRequest.GetResponse();
            response.Close();

            return true;
        }

        internal void GetLoginInfos()
        {
            string url = "https://ausbildung.ihk.de/pruefungsinfos/Peo/Login.aspx";

            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            webRequest.Host = "ausbildung.ihk.de";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36";
            webRequest.CookieContainer = _cookieContainer;

            var response = webRequest.GetResponse();

            using var webpageStream = response.GetResponseStream();
            var doc = new HtmlDocument();
            doc.Load(webpageStream);

            _loginInfos = new LoginInfos();

            var nodeCaptchaUrl = doc.DocumentNode.SelectSingleNode("//img[@id='ctl00_ContentPlaceHolder1_mRadCaptcha_CaptchaImageUP']");
            _loginInfos.CaptchaUrl = "https://" + nodeCaptchaUrl.Attributes["src"].Value.Replace("../..", "ausbildung.ihk.de").Replace("&amp;", "&");

            var nodeViewState = doc.DocumentNode.SelectSingleNode("//input[@id='__VIEWSTATE']");
            _loginInfos.ViewState = nodeViewState.GetAttributeValue("value", "?");

            var nodeViewStateGenerator = doc.DocumentNode.SelectSingleNode("//input[@id='__VIEWSTATEGENERATOR']");
            _loginInfos.ViewStateGenerator = nodeViewStateGenerator.GetAttributeValue("value", "?");
        }

        internal bool LoadConfig()
        {
            if (!File.Exists("config.xml"))
                return false;

            var serializer = new XmlSerializer(typeof(Config));
            _config = (Config)serializer.Deserialize(File.OpenRead("config.xml"));

            return true;
        }

        internal void OutputMarks()
        {
            string url = "https://ausbildung.ihk.de/pruefungsinfos/Peo/Ergebnisse.aspx";

            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            webRequest.Host = "ausbildung.ihk.de";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36";
            webRequest.Referer = "https://ausbildung.ihk.de/pruefungsinfos/Peo/Login.aspx";
            webRequest.CookieContainer = _cookieContainer;

            var response = webRequest.GetResponse();

            using (var webpageStream = response.GetResponseStream())
            {
                //var reader = new StreamReader(webpageStream); var responseStr =
                //reader.ReadToEnd(); Console.WriteLine(responseStr);

                var doc = new HtmlDocument();
                doc.Load(webpageStream);

                var div = doc.DocumentNode.SelectNodes("//div[@class='contentBox']")[2];
                var body = div.Descendants().Where(d => d.Name == "tbody").First();

                Console.BackgroundColor = ConsoleColor.Black;

                foreach (var row in body.Descendants().Where(d => d.Name == "tr"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{row.ChildNodes[0].InnerText.Replace(" &nbsp;", "")}");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($": ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"{row.ChildNodes[1].InnerText} ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Punkte - Note: ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{row.ChildNodes[2].InnerText}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

            response.Close();
        }

        internal void TryLogin()
        {
            bool worked = false;
            while (!worked)
            {
                GetLoginInfos();
                ShowLoginCaptcha();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Trage das Captcha ein!");
                var captcha = Console.ReadLine();

                if (Login(captcha))
                    worked = true;
            }
        }

        #endregion Internal Methods

        #region Private Methods

        private bool Login(string captcha)
        {
            string url = "https://ausbildung.ihk.de/pruefungsinfos/Peo/Login.aspx";

            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "POST";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            webRequest.Host = "ausbildung.ihk.de";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36";
            webRequest.CookieContainer = _cookieContainer;
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Referer = "https://ausbildung.ihk.de/pruefungsinfos/peo/Login.aspx";
            webRequest.KeepAlive = true;
            webRequest.Headers.Add("accept-language", "de,en-US;q=0.9,en;q=0.8,tr;q=0.7");
            webRequest.Headers.Add("cache-control", "no-cache");
            webRequest.Headers.Add("pragma", "no-cache");
            webRequest.Headers.Add("sec-fetch-dest", "document");
            webRequest.Headers.Add("sec-fetch-mode", "navigate");
            webRequest.Headers.Add("sec-fetch-site", "same-origin");
            webRequest.Headers.Add("sec-fetch-user", "?1");
            webRequest.Headers.Add("upgrade-insecure-request", "1");

            string postData =
$"ctl00_ctl04_TSM=;;System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35:en-US:92dc34f5-462f-43bd-99ec-66234f705cd1:ea597d4b:b25378d2;Telerik.Web.UI, Version=2018.3.910.40, Culture=neutral, PublicKeyToken=121fae78165ba3d4:en-US:df8a796a-503f-421d-9d40-9475fc76f21f:16e4e7cd:11e117d7" +
$"&__EVENTTARGET=ctl00$ContentPlaceHolder1$mlbSubmit" +
$"&__EVENTARGUMENT=" +
$"&__VIEWSTATE={HttpUtility.UrlEncode(_loginInfos.ViewState)}" +
$"&__VIEWSTATEGENERATOR={HttpUtility.UrlEncode(_loginInfos.ViewStateGenerator)}" +
$"&ctl00$ContentPlaceHolder1$txtAzubiNr={_config.IdentNr}" +
$"&ctl00$ContentPlaceHolder1$txtPrueflingsNr={_config.PrueflingsNr}" +
$"&ctl00$ContentPlaceHolder1$mRadCaptcha$CaptchaTextBox={captcha}" +
$"&ctl00_ContentPlaceHolder1_mRadCaptcha_ClientState=";
            var data = Encoding.ASCII.GetBytes(postData);

            webRequest.ContentLength = data.Length;

            Stream requestStream = webRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            var response = (HttpWebResponse)webRequest.GetResponse();
            if (response.StatusCode != HttpStatusCode.Found && response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Fehler bei Captcha Eingabe, versuch es erneut!");
                response.Close();
                return false;
            }
            if (response.ResponseUri.AbsolutePath.Contains("Login.aspx"))
            {
                Console.WriteLine("Captcha ist falsch, versuch es nochmal.");
                response.Close();
                return false;
            }

            response.Close();
            _lastProcess?.Close();
            return true;
        }

        private void ShowLoginCaptcha()
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(_loginInfos.CaptchaUrl);
            webRequest.Method = "GET";
            webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            webRequest.Host = "ausbildung.ihk.de";
            webRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36";
            webRequest.CookieContainer = _cookieContainer;

            var response = webRequest.GetResponse();

            using var reader = new BinaryReader(response.GetResponseStream());
            Byte[] lnByte = reader.ReadBytes(1 * 1024 * 1024 * 10);
            using (FileStream lxFS = new FileStream("captcha.jpg", FileMode.Create))
            {
                lxFS.Write(lnByte, 0, lnByte.Length);
            }
            _lastProcess = Process.Start(@"cmd.exe ", @"/c .\captcha.jpg");
        }

        #endregion Private Methods
    }
}