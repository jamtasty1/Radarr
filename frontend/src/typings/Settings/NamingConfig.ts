type ColonReplacementFormat =
  | 'delete'
  | 'dash'
  | 'spaceDash'
  | 'spaceDashSpace'
  | 'smart';

type EditionTagStyle = 'plex' | 'jellyfin';

export default interface NamingConfig {
  renameMovies: boolean;
  replaceIllegalCharacters: boolean;
  colonReplacementFormat: ColonReplacementFormat;
  standardMovieFormat: string;
  movieFolderFormat: string;
  editionTagStyle: EditionTagStyle;
}
