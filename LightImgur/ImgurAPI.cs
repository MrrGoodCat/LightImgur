using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;

namespace LightImgur
{
    public class ImgurAPI
    {

        static private string EndPoint = "https://api.imgur.com/3/";
       
        static private string ClientID = "59b901759d20a52";
        static private string ClientSecret = "3480e60e6a98105ebdf387dfe4dcb8aa56ee4561";
       
        static private int NumUploads = 0;
        
        static private string CurrentAccessToken = string.Empty;
        static private string CurrentRefreshToken = string.Empty;
        static private DateTime TokensExpireAt = DateTime.MinValue;
        
        static private System.Threading.Thread tokenThread = null;
        
        static public event AuthorizationEventHandler obtainedAuthorization;
        static public event AuthorizationEventHandler lostAuthorization;
        static public event AuthorizationEventHandler refreshedAuthorization;
        static public event NetworkEventHandler networkRequestFailed;

        public delegate void AuthorizationEventHandler();
        public delegate void NetworkEventHandler();


        Log Log;
        public ImgurAPI()
        {
            Log = new Log();
        }
        public void OpenAuthorizationPage()
        {
            string url = "https://api.imgur.com/oauth2/authorize?client_id=" + ClientID + "&response_type=pin&state=";

            System.Diagnostics.Process.Start(url);
        }

        public void RequestTokens(string PIN)
        {
            string url = "https://api.imgur.com/oauth2/token";

            string responseString = string.Empty;
            using (WebClient wc = new WebClient())
            {
                //t.Headers[HttpRequestHeader.Authorization] = "Client-ID " + m_ClientID;
                try
                {
                    var values = new System.Collections.Specialized.NameValueCollection
                    {
                        {
                            "client_id", ClientID
                        },
                        {
                            "client_secret", ClientSecret
                        },
                        {
                            "grant_type", "pin"
                        },
                        {
                            "pin", PIN
                        }
                    };
                    byte[] response = wc.UploadValues(url, "POST", values);
                    responseString = System.Text.Encoding.ASCII.GetString(response);
                }
                catch (System.Net.WebException ex)
                {
                    if (ex.Response == null)
                    {
                        if (networkRequestFailed != null) networkRequestFailed.Invoke();
                    }
                    else
                    {
                        System.IO.Stream stream = ex.Response.GetResponseStream();
                        int currByte = -1;
                        StringBuilder strBuilder = new StringBuilder();
                        while ((currByte = stream.ReadByte()) != -1)
                        {
                            strBuilder.Append((char)currByte);
                        }
                        responseString = strBuilder.ToString();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error("Unexpected Exception: " + ex.ToString());
                }
            }

            if (responseString == string.Empty)
            {
                return;
            }

            APIResponses.TokenResponse resp = JsonConvert.DeserializeObject<APIResponses.TokenResponse>(responseString, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });
            if (resp != null && resp.AccessToken != null && resp.RefreshToken != null)
            {
                StoreNewTokens(resp.ExpiresIn, resp.AccessToken, resp.RefreshToken);

                Log.Info("Received tokens from PIN");

                StartTokenThread();

                if (obtainedAuthorization != null) obtainedAuthorization.Invoke();
            }
            else
            {
                Log.Error("Something went wrong while trying to obtain access and refresh tokens");
            }
        }

        private void StartTokenThread()
        {
            tokenThread = new System.Threading.Thread(TokenThread);
            tokenThread.Start();
        }

        private void TokenThread()
        {
            Log.Info("Token thread started");
            while (true)
            {
                TimeSpan timeSpan = (TokensExpireAt > DateTime.Now) ? (TokensExpireAt - DateTime.Now) : (DateTime.Now.AddSeconds(60.0) - DateTime.Now);
                Log.Info("Token thread will refresh in " + timeSpan.TotalSeconds + " seconds");
                System.Threading.Thread.Sleep(timeSpan);
                if (!RefreshTokens())
                {
                    Log.Error("Could not refresh tokens on token thread, thread aborting");
                    break;
                }
            }
        }

        private bool RefreshTokens()
        {
            if (!HasBeenAuthorized())
            {
                return false;
            }

            string url = "https://api.imgur.com/oauth2/token";

            string responseString = string.Empty;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    var values = new System.Collections.Specialized.NameValueCollection
                    {
                        {
                            "client_id", ClientID
                        },
                        {
                            "client_secret", ClientSecret
                        },
                        {
                            "grant_type", "refresh_token"
                        },
                        {
                            "refresh_token", CurrentRefreshToken
                        }
                    };
                    byte[] response = wc.UploadValues(url, "POST", values);
                    responseString = System.Text.Encoding.ASCII.GetString(response);
                }
                catch (System.Net.WebException ex)
                {
                    if (ex.Response == null)
                    {
                        if (networkRequestFailed != null) networkRequestFailed.Invoke();
                    }
                    else
                    {
                        System.IO.Stream stream = ex.Response.GetResponseStream();
                        int currByte = -1;
                        StringBuilder strBuilder = new StringBuilder();
                        while ((currByte = stream.ReadByte()) != -1)
                        {
                            strBuilder.Append((char)currByte);
                        }
                        responseString = strBuilder.ToString();
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error("Unexpected Exception: " + ex.ToString());
                }
            }

            if (responseString == string.Empty)
            {
                return false;
            }

            APIResponses.TokenResponse resp = null;
            try
            {
                resp = Newtonsoft.Json.JsonConvert.DeserializeObject<APIResponses.TokenResponse>(responseString, new Newtonsoft.Json.JsonSerializerSettings { PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.Objects });
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                Log.Error("Newtonsoft.Json.JsonReaderException occurred while trying to deserialize the following string:\n" + responseString + " (Line: " + ex.LineNumber + ", Position: " + ex.LinePosition + ", Message: " + ex.Message + ")");
                resp = null;
            }
            catch (System.Exception ex)
            {
                Log.Error("Unexpected Exception: " + ex.ToString());
            }
            if (resp != null && resp.AccessToken != null && resp.RefreshToken != null)
            {
                StoreNewTokens(resp.ExpiresIn, resp.AccessToken, resp.RefreshToken);

                Log.Info("Refreshed tokens");

                if (refreshedAuthorization != null) refreshedAuthorization.Invoke();

                return true;
            }

            Log.Error("Something went wrong while trying to refresh access- and refresh-tokens");

            CurrentAccessToken = null;
            CurrentRefreshToken = null;

            Properties.Settings.Default.accessToken = null;
            Properties.Settings.Default.refreshToken = null;
            Properties.Settings.Default.Save();

            if (lostAuthorization != null) lostAuthorization.Invoke();

            return false;
        }

        public bool HasBeenAuthorized()
        {
            return (CurrentAccessToken != null && CurrentAccessToken != string.Empty && CurrentRefreshToken != null && CurrentRefreshToken != string.Empty && TokensExpireAt > DateTime.MinValue/*&& m_TokensExpireAt > DateTime.Now*/);
        }

        private void StoreNewTokens(int ExpiresInSeconds, string AccessToken, string RefreshToken)
        {
            TokensExpireAt = DateTime.Now.AddSeconds(ExpiresInSeconds / 2);

            CurrentAccessToken = AccessToken;
            CurrentRefreshToken = RefreshToken;

            Properties.Settings.Default.accessToken = CurrentAccessToken;
            Properties.Settings.Default.refreshToken = CurrentRefreshToken;
            Properties.Settings.Default.Save();
        }
        public async void GetImageFromAlbum()
        {
            var client = new ImgurClient("59b901759d20a52");
            var endpoint = new AlbumEndpoint(client);
            var image = await endpoint.GetAlbumImageAsync("buW", "ALBUM_ID");
        }
    }
}
