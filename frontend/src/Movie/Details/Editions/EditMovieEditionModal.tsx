import React, { useCallback, useEffect, useState } from 'react';
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
import { MovieEdition } from 'Movie/Editions/MovieEdition';
import { useSaveMovieEdition } from 'Movie/Editions/useMovieEditions';
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

  const { mutate, isPending, error, reset } = useSaveMovieEdition(movieId);

  useEffect(() => {
    reset();
  }, [name, reset]);

  const handleNameChange = useCallback(({ value }: InputChanged<string>) => {
    setName(value);
  }, []);

  const handleMonitoredChange = useCallback(
    ({ value }: InputChanged<boolean>) => {
      setMonitored(value);
    },
    []
  );

  const handleSavePress = useCallback(() => {
    mutate(
      { id: edition?.id, movieId, name: name.trim(), monitored },
      { onSuccess: onModalClose }
    );
  }, [edition, movieId, name, monitored, mutate, onModalClose]);

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
          <FormGroup>
            <FormLabel>{translate('Name')}</FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="name"
              value={name}
              helpText={translate('EditionNameHelpText')}
              onChange={handleNameChange}
            />
          </FormGroup>

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
