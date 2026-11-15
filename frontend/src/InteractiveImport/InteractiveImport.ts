import ModelBase from 'App/ModelBase';
import Language from 'Language/Language';
import Movie from 'Movie/Movie';
import { QualityModel } from 'Quality/Quality';
import CustomFormat from 'typings/CustomFormat';
import Rejection from 'typings/Rejection';

export interface InteractiveImportCommandOptions {
  path: string;
  folderName: string;
  movieId: number;
  releaseGroup?: string;
  // Empty string means the standard edition.
  edition?: string;
  quality: QualityModel;
  languages: Language[];
  indexerFlags: number;
  downloadId?: string;
  movieFileId?: number;
}

interface InteractiveImport extends ModelBase {
  path: string;
  relativePath: string;
  folderName: string;
  name: string;
  size: number;
  releaseGroup: string;
  edition?: string;
  // Edition slot the file would fill: 0 standard, -1 not tracked by the movie.
  movieEditionId?: number;
  quality: QualityModel;
  languages: Language[];
  movie?: Movie;
  qualityWeight: number;
  customFormats: CustomFormat[];
  indexerFlags: number;
  rejections: Rejection[];
  movieFileId?: number;
}

export default InteractiveImport;
