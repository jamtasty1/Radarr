import React, { useCallback, useMemo, useState } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, scrollDirections } from 'Helpers/Props';
import { MovieEdition } from 'Movie/Editions/MovieEdition';
import { getStandardEditionName } from 'Movie/Editions/useMovieEditions';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';

// Choosing from the movie's editions sends the edition's name, which the
// backend resolves back to that edition; an empty string is the standard one.
// Edition names can't be "Standard", so the sentinel can't collide.
const STANDARD_VALUE = '__standard__';

interface SelectEditionModalContentProps {
  edition: string;
  movieEditionId?: number;
  editions: MovieEdition[];
  standardEditionName?: string | null;
  modalTitle: string;
  onEditionSelect(edition: string): void;
  onModalClose(): void;
}

function getInitialValue(
  movieEditionId: number | undefined,
  editions: MovieEdition[]
) {
  return editions.find((e) => e.id === movieEditionId)?.name ?? STANDARD_VALUE;
}

function SelectEditionModalContent({
  edition: initialEdition,
  movieEditionId,
  editions,
  standardEditionName,
  modalTitle,
  onEditionSelect,
  onModalClose,
}: SelectEditionModalContentProps) {
  // A standard edition named for a cut is a choice in its own right.
  const hasChoices = editions.length > 0 || !!standardEditionName;

  const [edition, setEdition] = useState(() =>
    hasChoices ? getInitialValue(movieEditionId, editions) : initialEdition
  );

  const options = useMemo(
    () => [
      {
        key: STANDARD_VALUE,
        value: getStandardEditionName(standardEditionName),
      },
      ...editions.map(({ name }) => ({ key: name, value: name })),
    ],
    [editions, standardEditionName]
  );

  const handleChange = useCallback(({ value }: InputChanged<string>) => {
    setEdition(value);
  }, []);

  const handleSelectPress = useCallback(() => {
    // A standard edition named for a cut is chosen by its name; otherwise an
    // empty edition is the standard one.
    onEditionSelect(
      edition === STANDARD_VALUE ? standardEditionName ?? '' : edition
    );
  }, [edition, standardEditionName, onEditionSelect]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('SetEditionModalTitle', { modalTitle })}
      </ModalHeader>

      <ModalBody scrollDirection={scrollDirections.NONE}>
        <Form>
          <FormGroup>
            <FormLabel>{translate('Edition')}</FormLabel>

            {hasChoices ? (
              <FormInputGroup
                type={inputTypes.SELECT}
                name="edition"
                value={edition}
                values={options}
                helpText={translate('SelectEditionHelpText')}
                onChange={handleChange}
              />
            ) : (
              <FormInputGroup
                type={inputTypes.TEXT}
                name="edition"
                value={edition}
                autoFocus={true}
                helpText={translate('SelectEditionNoEditionsHelpText')}
                onChange={handleChange}
              />
            )}
          </FormGroup>
        </Form>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.SUCCESS} onPress={handleSelectPress}>
          {translate('SetEdition')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

interface SelectEditionModalProps extends SelectEditionModalContentProps {
  isOpen: boolean;
}

function SelectEditionModal({
  isOpen,
  onModalClose,
  ...otherProps
}: SelectEditionModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <SelectEditionModalContent {...otherProps} onModalClose={onModalClose} />
    </Modal>
  );
}

export default SelectEditionModal;
