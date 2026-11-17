import React, { useCallback, useEffect, useMemo, useState } from 'react';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds, sizes } from 'Helpers/Props';
import EditionNameInput from 'Movie/Editions/EditionNameInput';
import {
  useMovieEditions,
  useRenameStandardEdition,
} from 'Movie/Editions/useMovieEditions';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';

// Picked to go back to the plain standard edition. Edition names can't be
// "Standard", so it can't collide with a real name.
const PLAIN_STANDARD_KEY = '__standard__';

interface RenameStandardEditionModalContentProps {
  movieId: number;
  standardEditionName?: string | null;
  onModalClose: () => void;
}

function RenameStandardEditionModalContent({
  movieId,
  standardEditionName,
  onModalClose,
}: RenameStandardEditionModalContentProps) {
  const [name, setName] = useState(standardEditionName || PLAIN_STANDARD_KEY);
  const { data: editions = [] } = useMovieEditions(movieId);
  const { mutate, isPending, error, reset } = useRenameStandardEdition(movieId);

  useEffect(() => {
    reset();
  }, [name, reset]);

  const leadingOptions = useMemo(
    () => [{ key: PLAIN_STANDARD_KEY, value: translate('StandardEdition') }],
    []
  );

  const takenNames = useMemo(() => editions.map((e) => e.name), [editions]);

  const handleSavePress = useCallback(() => {
    mutate(name === PLAIN_STANDARD_KEY ? '' : name.trim(), {
      onSuccess: onModalClose,
    });
  }, [name, mutate, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('EditStandardEdition')}</ModalHeader>

      <ModalBody>
        {error ? (
          <Alert kind={kinds.DANGER}>
            {getErrorMessage(error, translate('EditionSaveError'))}
          </Alert>
        ) : null}

        <Form>
          <EditionNameInput
            name={name}
            helpText={translate('StandardEditionNameHelpText')}
            leadingOptions={leadingOptions}
            takenNames={takenNames}
            onChange={setName}
          />
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

interface RenameStandardEditionModalProps
  extends RenameStandardEditionModalContentProps {
  isOpen: boolean;
}

function RenameStandardEditionModal({
  isOpen,
  onModalClose,
  ...otherProps
}: RenameStandardEditionModalProps) {
  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={onModalClose}>
      <RenameStandardEditionModalContent
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default RenameStandardEditionModal;
