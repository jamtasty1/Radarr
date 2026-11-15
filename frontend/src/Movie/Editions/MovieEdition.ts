import ModelBase from 'App/ModelBase';

// The standard (theatrical) edition is the movie's own file and has no
// MovieEdition row; files in it have movieEditionId 0.
export const STANDARD_EDITION_ID = 0;

export interface MovieEdition extends ModelBase {
  movieId: number;
  name: string;
  monitored: boolean;
  added: string;
  movieFileId?: number;
  hasFile: boolean;
}

export interface MovieEditionInput {
  id?: number;
  movieId: number;
  name: string;
  monitored: boolean;
}
