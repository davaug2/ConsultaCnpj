using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace WebApp.Models
{
    public class CookieAwareWebClient : WebClient
    {
        private int? _timeout = null;
        public int? Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
            }
        }
        public void SetTimeout(int timeout)
        {
            _timeout = timeout;
        }

        public CookieAwareWebClient()
            : this(new CookieContainer())
        { }
        public CookieAwareWebClient(CookieContainer c)
        {
            this.CookieContainer = c;
        }
        public CookieContainer CookieContainer { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            var castRequest = request as HttpWebRequest;
            if (castRequest != null)
            {
                castRequest.CookieContainer = this.CookieContainer;
                if (_timeout.HasValue)
                    castRequest.Timeout = _timeout.Value;
            }

            return request;
        }
    }
}