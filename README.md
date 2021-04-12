# SpotiLy - AIO Bot

Spotify Bot for artificals streams

### SpotiLy (config.json) ###

```
{
  "UpgradesPath": C:\\Users\\user\\accounts.txt,
  "ProxyPathFree": "C:\\Users\\user\\proxies.txt",
  "AlbumPath": "C:\\Users\\user\\albums.txt",
  "Driver": 0,
  "PremiumThreads": 0,
  "FreeThreads": 10,
  "MinSkip": 45,
  "MaxSkip": 75,
  "StreamPerFreeAccount": -1,
  "FollowRatio": 4,
  "LikeRatio": 7,
  "GeneratePlaylistRatio": 30
}
```

### SpotiLyAccGen ###

Will generate Spotify accounts (residentials proxies are recommanded)

### SpotiLyAccUpgrade ###

Will upgrade Spotify account from Tokens and addresses.
2 files are required :

```
Token file fime ? [token:adresse]
Account file fime ? [email:pwd]
```

### SpotiLyInvitUpdate ###

Will update tokens from Owners Family Plan (to invite Spotify account)
1 file is required :

```
Owners upgrade file fime ? [email:pwd]
```

### SpotiLyMail ###

Custom SMTP Server (Port 25) - change it on the source code.
Will bypass Authentication, please just run dotnet server on a server and point your domain name to him.

For Example :

```
myname.com (your domain name)
127.0.0.1 (your server ip)
xxxxxxxxxx@myname.com, all e-mails will be stored on the mails folder
```
