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
  modalTitle: string;
  onEditionSelect(edition: string): void;
  onModalClose(): void;
}

function getInitialValue(
  edition: string,
  movieEditionId: number | undefined,
  editions: MovieEdition[]
) {
  if (!editions.length) {
    return edition;
  }

  return editions.find((e) => e.id === movieEditionId)?.name ?? STANDARD_VALUE;
}

function SelectEditionModalContent({
  edition: initialEdition,
  movieEditionId,
  editions,
  modalTitle,
  onEditionSelect,
  onModalClose,
}: SelectEditionModalContentProps) {
  const [edition, setEdition] = useState(() =>
    getInitialValue(initialEdition, movieEditionId, editions)
  );

  const options = useMemo(
    () => [
      { key: STANDARD_VALUE, value: translate('StandardEdition') },
      ...editions.map(({ name }) => ({ key: name, value: name })),
    ],
    [editions]
  );

  const handleChange = useCallback(({ value }: InputChanged<string>) => {
    setEdition(value);
  }, []);

  const handleSelectPress = useCallback(() => {
    onEditionSelect(edition === STANDARD_VALUE ? '' : edition);
  }, [edition, onEditionSelect]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('SetEditionModalTitle', { modalTitle })}
      </ModalHeader>

      <ModalBody scrollDirection={scrollDirections.NONE}>
        <Form>
          <FormGroup>
            <FormLabel>{translate('Edition')}</FormLabel>

            {editions.length ? (
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
