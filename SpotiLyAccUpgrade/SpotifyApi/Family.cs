using System.Collections.Generic;

namespace SpotiLyAccUpgrade.SpotifyApi
{
    public class Family
    {
        public string homeId { get; set; }
        public object name { get; set; }
        public string planType { get; set; }
        public List<Member> members { get; set; }
        public AccessControl accessControl { get; set; }
        public List<string> features { get; set; }
    }
    public class Member
    {
        public string name { get; set; }
        public string smallProfileImageUrl { get; set; }
        public string largeProfileImageUrl { get; set; }
        public string id { get; set; }
        public string country { get; set; }
        public bool explicitContentLockEnabled { get; set; }
        public bool isLoggedInUser { get; set; }
        public List<object> playlistSharingRequests { get; set; }
        public bool isMaster { get; set; }
        public bool isChildAccount { get; set; }
        public bool isNewUser { get; set; }
    }

    public class AccessControl
    {
        public bool planHasFreeSlots { get; set; }
        public bool onboardingRequired { get; set; }
        public bool addressUpdateRequired { get; set; }
    }
}