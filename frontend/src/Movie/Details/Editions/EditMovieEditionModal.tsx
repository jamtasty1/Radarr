import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import { THEATRICAL_EDITION_NAME } from 'Movie/Editions/commonEditions';
import EditionNameInput from 'Movie/Editions/EditionNameInput';
import { MovieEdition } from 'Movie/Editions/MovieEdition';
import {
  useMovieEditions,
  useSaveMovieEdition,
} from 'Movie/Editions/useMovieEditions';
import useMovie from 'Movie/useMovie';
import { InputChanged } from 'typings/inputs';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';

interface EditMovieEditionModalContentProps {
  movieId: number;
  edition?: MovieEdition;
  onModalClose: () => void;
}

function EditMovieEditionModalContent({
  movieId,
  edition,
  onModalClose,
}: EditMovieEditionModalContentProps) {
  const [name, setName] = useState(edition?.name ?? '');
  const [monitored, setMonitored] = useState(edition?.monitored ?? true);
  const [qualityProfileId, setQualityProfileId] = useState(
    edition?.qualityProfileId ?? 0
  );

  const qualityProfiles = useSelector(
    (state: AppState) => state.settings.qualityProfiles.items
  );

  // 0 keeps the movie's own profile.
  const profileOptions = useMemo(
    () => [
      { key: 0, value: translate('EditionUseMovieProfile') },
      ...qualityProfiles.map(({ id, name }) => ({ key: id, value: name })),
    ],
    [qualityProfiles]
  );

  const handleProfileChange = useCallback(
    ({ value }: InputChanged<string | number>) => {
      setQualityProfileId(Number(value));
    },
    []
  );

  const { mutate, isPending, error, reset } = useSaveMovieEdition(movieId);

  useEffect(() => {
    reset();
  }, [name, reset]);

  const movie = useMovie(movieId);
  const { data: editions = [] } = useMovieEditions(movieId);
  const standardEditionName = movie?.standardEditionName;

  // Other editions' names (and the standard edition's cut) can't be reused.
  const takenNames = useMemo(
    () => [
      ...editions.filter((e) => e.id !== edition?.id).map((e) => e.name),
      ...(standardEditionName ? [standardEditionName] : []),
    ],
    [editions, edition, standardEditionName]
  );

  // Theatrical can only be its own edition once the standard edition is a cut.
  const extraNames = useMemo(
    () => (standardEditionName ? [THEATRICAL_EDITION_NAME] : []),
    [standardEditionName]
  );

  const handleMonitoredChange = useCallback(
    ({ value }: InputChanged<boolean>) => {
      setMonitored(value);
    },
    []
  );

  const handleSavePress = useCallback(() => {
    mutate(
      {
        id: edition?.id,
        movieId,
        name: name.trim(),
        monitored,
        qualityProfileId,
      },
      { onSuccess: onModalClose }
    );
  }, [
    edition,
    movieId,
    name,
    monitored,
    qualityProfileId,
    mutate,
    onModalClose,
  ]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {edition ? translate('EditEdition') : translate('AddEdition')}
      </ModalHeader>

      <ModalBody>
        {error ? (
          <Alert kind={kinds.DANGER}>
            {getErrorMessage(error, translate('EditionSaveError'))}
          </Alert>
        ) : null}

        <Form>
          <EditionNameInput
            name={name}
            helpText={translate('EditionNameHelpText')}
            takenNames={takenNames}
            extraNames={extraNames}
            onChange={setName}
          />

          <FormGroup>
            <FormLabel>{translate('Monitored')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="monitored"
              value={monitored}
              helpText={translate('EditionMonitoredHelpText')}
              onChange={handleMonitoredChange}
            />
          </FormGroup>
          <FormGroup>
            <FormLabel>{translate('QualityProfile')}</FormLabel>

            <FormInputGroup
              type={inputTypes.SELECT}
              name="qualityProfileId"
              value={qualityProfileId}
              values={profileOptions}
              helpText={translate('EditionQualityProfileHelpText')}
              onChange={handleProfileChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <SpinnerButton
          kind={kinds.PRIMARY}
          isSpinning={isPending}
          isDisabled={!name.trim()}
          onPress={handleSavePress}
        >
          {translate('Save')}
        </SpinnerButton>
      </ModalFooter>
    </ModalContent>
  );
}

interface EditMovieEditionModalProps extends EditMovieEditionModalContentProps {
  isOpen: boolean;
}

function EditMovieEditionModal({
  isOpen,
  onModalClose,
  ...otherProps
}: EditMovieEditionModalProps) {
  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={onModalClose}>
      <EditMovieEditionModalContent
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default EditMovieEditionModal;
