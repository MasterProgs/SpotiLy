using System;
using System.Collections.Generic;
using System.Text;

namespace SpotiLyAccUpgrade.SpotifyApi
{
    public class Address
    {
        public string mainText { get; set; }
        public string secondaryText { get; set; }
        public string address { get; set; }
        public string placeId { get; set; }
    }

    public class HomeHub
    {
        public List<Address> addresses { get; set; }
    }
}
