import React, { useMemo } from 'react';
import { useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import Alert from 'Components/Alert';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import useModalOpenState from 'Helpers/Hooks/useModalOpenState';
import { icons, kinds } from 'Helpers/Props';
import { STANDARD_EDITION_ID } from 'Movie/Editions/MovieEdition';
import { useMovieEditions } from 'Movie/Editions/useMovieEditions';
import useMovie from 'Movie/useMovie';
import { MovieFile } from 'MovieFile/MovieFile';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import EditMovieEditionModal from './EditMovieEditionModal';
import MovieEditionRow from './MovieEditionRow';
import styles from './MovieEditionsTable.css';

const columns: Column[] = [
  {
    name: 'monitored',
    label: '',
    isVisible: true,
  },
  {
    name: 'name',
    label: () => translate('Edition'),
    isVisible: true,
  },
  {
    name: 'status',
    label: () => translate('Status'),
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

// The newest file in a slot is the one that counts, matching the backend.
function getFileForEdition(files: MovieFile[], movieEditionId: number) {
  return files
    .filter((f) => (f.movieEditionId ?? STANDARD_EDITION_ID) === movieEditionId)
    .reduce<MovieFile | undefined>(
      (newest, f) => (!newest || f.id > newest.id ? f : newest),
      undefined
    );
}

interface MovieEditionsTableProps {
  movieId: number;
}

function MovieEditionsTable({ movieId }: MovieEditionsTableProps) {
  const movie = useMovie(movieId);
  const movieFiles = useSelector((state: AppState) => state.movieFiles.items);
  const { data: editions, isLoading, error } = useMovieEditions(movieId);

  const [isAddModalOpen, setAddModalOpen, setAddModalClosed] =
    useModalOpenState(false);

  const filesForMovie = useMemo(
    () => movieFiles.filter((f) => f.movieId === movieId),
    [movieFiles, movieId]
  );

  const sortedEditions = useMemo(
    () => [...(editions ?? [])].sort(sortByProp('name')),
    [editions]
  );

  if (!movie) {
    return null;
  }

  const standardFile =
    movie.movieFileId > 0
      ? filesForMovie.find((f) => f.id === movie.movieFileId)
      : undefined;

  return (
    <div className={styles.container}>
      {isLoading ? <LoadingIndicator /> : null}

      {!isLoading && error ? (
        <Alert kind={kinds.DANGER}>{translate('EditionsLoadError')}</Alert>
      ) : null}

      {!isLoading && !error ? (
        <>
          <Table columns={columns}>
            <TableBody>
              <MovieEditionRow
                movieId={movieId}
                isMovieMonitored={movie.monitored}
                isMovieSaving={movie.isSaving}
                movieFile={standardFile}
              />

              {sortedEditions.map((edition) => (
                <MovieEditionRow
                  key={edition.id}
                  movieId={movieId}
                  isMovieMonitored={movie.monitored}
                  edition={edition}
                  movieFile={getFileForEdition(filesForMovie, edition.id)}
                />
              ))}
            </TableBody>
          </Table>

          <div className={styles.footer}>
            {sortedEditions.length ? null : (
              <div className={styles.help}>
                {translate('NoEditionsHelpText')}
              </div>
            )}

            <Button className={styles.addButton} onPress={setAddModalOpen}>
              <Icon name={icons.ADD} /> {translate('AddEdition')}
            </Button>
          </div>
        </>
      ) : null}

      <EditMovieEditionModal
        isOpen={isAddModalOpen}
        movieId={movieId}
        onModalClose={setAddModalClosed}
      />
    </div>
  );
}

export default MovieEditionsTable;
