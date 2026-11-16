import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import useModalOpenState from 'Helpers/Hooks/useModalOpenState';
import { icons, kinds } from 'Helpers/Props';
import { MovieEdition } from 'Movie/Editions/MovieEdition';
import {
  useDeleteMovieEdition,
  useSaveMovieEdition,
  useSetStandardEditionMonitored,
} from 'Movie/Editions/useMovieEditions';
import MovieQuality from 'Movie/MovieQuality';
import { MovieFile } from 'MovieFile/MovieFile';
import QualityProfileName from 'Settings/Profiles/Quality/QualityProfileName';
import { showMessage } from 'Store/Actions/appActions';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import EditMovieEditionModal from './EditMovieEditionModal';
import styles from './MovieEditionsTable.css';

interface MovieEditionRowProps {
  movieId: number;
  isMovieMonitored: boolean;
  isStandardEditionMonitored: boolean;
  hasEditions: boolean;
  movieQualityProfileId: number;
  // Undefined for the standard edition, which is the movie itself.
  edition?: MovieEdition;
  movieFile?: MovieFile;
}

function MovieEditionRow({
  movieId,
  isMovieMonitored,
  isStandardEditionMonitored,
  hasEditions,
  movieQualityProfileId,
  edition,
  movieFile,
}: MovieEditionRowProps) {
  const dispatch = useDispatch();
  const isStandard = !edition;
  const monitored = isStandard ? isStandardEditionMonitored : edition.monitored;
  const qualityProfileId = edition?.qualityProfileId || movieQualityProfileId;
  const isWanted = isMovieMonitored && monitored;

  const [isEditModalOpen, setEditModalOpen, setEditModalClosed] =
    useModalOpenState(false);
  const [isDeleteModalOpen, setDeleteModalOpen, setDeleteModalClosed] =
    useModalOpenState(false);

  const { mutate: saveEdition, isPending: isSaving } =
    useSaveMovieEdition(movieId);
  const { mutate: setStandardMonitored, isPending: isStandardSaving } =
    useSetStandardEditionMonitored(movieId);
  const { mutate: deleteEdition, isPending: isDeleting } =
    useDeleteMovieEdition(movieId);

  const showError = useCallback(
    (message: string) => {
      dispatch(
        showMessage({
          id: `movieEdition-${movieId}`,
          name: 'movieEdition',
          message,
          type: 'error',
          hideAfter: 8,
        })
      );
    },
    [dispatch, movieId]
  );

  const handleMonitorPress = useCallback(
    (value: boolean) => {
      if (isStandard) {
        setStandardMonitored(value);
        return;
      }

      saveEdition(
        { ...edition, monitored: value },
        {
          onError: (error) =>
            showError(getErrorMessage(error, translate('EditionSaveError'))),
        }
      );
    },
    [isStandard, edition, saveEdition, setStandardMonitored, showError]
  );

  const handleDeleteConfirm = useCallback(() => {
    if (!edition) {
      return;
    }

    deleteEdition(edition.id, {
      onSuccess: setDeleteModalClosed,
      onError: (error) => {
        setDeleteModalClosed();
        showError(getErrorMessage(error, translate('EditionDeleteError')));
      },
    });
  }, [edition, deleteEdition, setDeleteModalClosed, showError]);

  return (
    <TableRow>
      <TableRowCell className={styles.monitored}>
        <MonitorToggleButton
          monitored={monitored}
          // Turning off the only edition would want nothing; unmonitor the movie for that.
          isDisabled={
            !isMovieMonitored || (isStandard && !hasEditions && monitored)
          }
          isSaving={isStandard ? isStandardSaving : isSaving}
          onPress={handleMonitorPress}
        />
      </TableRowCell>

      <TableRowCell className={styles.name}>
        {isStandard ? (
          <span title={translate('StandardEditionHelpText')}>
            {translate('StandardEdition')}
          </span>
        ) : (
          edition.name
        )}
      </TableRowCell>

      <TableRowCell className={styles.status}>
        {movieFile ? (
          <div className={styles.file}>
            <MovieQuality
              quality={movieFile.quality}
              isCutoffNotMet={movieFile.qualityCutoffNotMet}
            />

            <span className={styles.path} title={movieFile.relativePath}>
              {movieFile.relativePath}
            </span>
          </div>
        ) : (
          <Label kind={isWanted ? kinds.DANGER : kinds.DEFAULT}>
            {isWanted ? translate('Missing') : translate('Unmonitored')}
          </Label>
        )}
      </TableRowCell>

      <TableRowCell className={styles.profile}>
        <QualityProfileName qualityProfileId={qualityProfileId} />
      </TableRowCell>

      <TableRowCell className={styles.actions}>
        {isStandard ? null : (
          <>
            <IconButton
              title={translate('EditEdition')}
              name={icons.EDIT}
              onPress={setEditModalOpen}
            />

            <IconButton
              title={
                movieFile
                  ? translate('EditionDeleteHasFile')
                  : translate('DeleteEdition')
              }
              name={icons.REMOVE}
              isDisabled={!!movieFile}
              onPress={setDeleteModalOpen}
            />
          </>
        )}
      </TableRowCell>

      {edition ? (
        <>
          <EditMovieEditionModal
            isOpen={isEditModalOpen}
            movieId={movieId}
            edition={edition}
            onModalClose={setEditModalClosed}
          />

          <ConfirmModal
            isOpen={isDeleteModalOpen}
            kind={kinds.DANGER}
            title={translate('DeleteEdition')}
            message={translate('DeleteEditionMessageText', {
              name: edition.name,
            })}
            confirmLabel={translate('Delete')}
            isSpinning={isDeleting}
            onConfirm={handleDeleteConfirm}
            onCancel={setDeleteModalClosed}
          />
        </>
      ) : null}
    </TableRow>
  );
}

export default MovieEditionRow;
