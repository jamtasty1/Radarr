namespace NzbDrone.Core.Organizer
{
    // How a renamed file in a non-standard edition is tagged when the naming format has no
    // {Edition Tags} token, so media servers can tell the editions in a movie's folder apart.
    public enum EditionTagStyle
    {
        // "Movie (2010) Bluray-1080p {edition-Director's Cut}"
        Plex = 0,

        // "Movie (2010) - Director's Cut Bluray-1080p": Jellyfin and Emby group files as versions
        // when each name starts with the folder name followed by " - ".
        Jellyfin = 1
    }
}
