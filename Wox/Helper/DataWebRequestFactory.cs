namespace Wox.Helper
{
    using System;
    using System.IO;
    using System.Net;

    public class DataWebRequestFactory : IWebRequestCreate
    {
        private class DataWebRequest : WebRequest
        {
            private readonly Uri m_uri;

            public DataWebRequest(Uri uri)
            {
                m_uri = uri;
            }

            #region Public

            public override WebResponse GetResponse()
            {
                return new DataWebResponse(m_uri);
            }

            #endregion
        }

        private class DataWebResponse : WebResponse
        {
            public override string ContentType
            {
                get => m_contentType;
                set => throw new NotSupportedException();
            }

            public override long ContentLength
            {
                get => m_data.Length;
                set => throw new NotSupportedException();
            }

            private readonly string m_contentType;
            private readonly byte[] m_data;

            public DataWebResponse(Uri uri)
            {
                var uriString = uri.AbsoluteUri;

                var commaIndex = uriString.IndexOf(',');
                var headers = uriString.Substring(0, commaIndex).Split(';');
                m_contentType = headers[0];
                var dataString = uriString.Substring(commaIndex + 1);
                m_data = Convert.FromBase64String(dataString);
            }

            #region Public

            public override Stream GetResponseStream()
            {
                return new MemoryStream(m_data);
            }

            #endregion
        }

        #region Public

        public WebRequest Create(Uri uri)
        {
            return new DataWebRequest(uri);
        }

        #endregion
    }
}