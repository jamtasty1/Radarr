import React, { useCallback, useMemo, useRef, useState } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import {
  COMMON_EDITION_NAMES,
  CUSTOM_EDITION_KEY,
  findCommonEditionName,
  getEditionNameKey,
} from './commonEditions';

export interface EditionNameOption {
  key: string;
  value: string;
}

interface EditionNameInputProps {
  name: string;
  helpText: string;
  // Options shown above the common names, e.g. "Standard" when renaming the
  // standard edition.
  leadingOptions?: EditionNameOption[];
  // Extra common names, e.g. "Theatrical" once the standard edition is a cut.
  extraNames?: string[];
  // Names the movie already uses; they're shown but can't be picked.
  takenNames?: string[];
  onChange: (name: string) => void;
}

function getInitialKey(name: string, leadingOptions: EditionNameOption[]) {
  const leading = leadingOptions.find((o) => o.key === name);

  if (leading) {
    return leading.key;
  }

  return findCommonEditionName(name) ?? (name ? CUSTOM_EDITION_KEY : '');
}

// A drop down of common edition names with a custom option at the bottom that
// reveals a text box.
function EditionNameInput({
  name,
  helpText,
  leadingOptions = [],
  extraNames = [],
  takenNames = [],
  onChange,
}: EditionNameInputProps) {
  const [selectedKey, setSelectedKey] = useState(() =>
    getInitialKey(name, leadingOptions)
  );
  const [customName, setCustomName] = useState(() =>
    getInitialKey(name, leadingOptions) === CUSTOM_EDITION_KEY ? name : ''
  );

  // The edition being edited may keep its own name.
  const originalKey = useRef(getEditionNameKey(name)).current;

  const options = useMemo(() => {
    const takenKeys = new Set(takenNames.map(getEditionNameKey));

    return [
      ...(selectedKey ? [] : [{ key: '', value: translate('SelectEdition') }]),
      ...leadingOptions,
      ...[...COMMON_EDITION_NAMES, ...extraNames].map((n) => {
        const key = getEditionNameKey(n);
        const isTaken = takenKeys.has(key) && key !== originalKey;

        return {
          key: n,
          value: n,
          isDisabled: isTaken,
          hint: isTaken ? translate('EditionAlreadyTracked') : undefined,
        };
      }),
      { key: CUSTOM_EDITION_KEY, value: translate('CustomEditionName') },
    ];
  }, [originalKey, selectedKey, leadingOptions, extraNames, takenNames]);

  const handleSelectChange = useCallback(
    ({ value }: InputChanged<string>) => {
      setSelectedKey(value);
      onChange(value === CUSTOM_EDITION_KEY ? customName : value);
    },
    [customName, onChange]
  );

  const handleCustomChange = useCallback(
    ({ value }: InputChanged<string>) => {
      setCustomName(value);
      onChange(value);
    },
    [onChange]
  );

  return (
    <>
      <FormGroup>
        <FormLabel>{translate('Name')}</FormLabel>

        <FormInputGroup
          type={inputTypes.SELECT}
          name="editionName"
          value={selectedKey}
          values={options}
          helpText={helpText}
          onChange={handleSelectChange}
        />
      </FormGroup>

      {selectedKey === CUSTOM_EDITION_KEY ? (
        <FormGroup>
          <FormLabel>{translate('CustomEditionName')}</FormLabel>

          <FormInputGroup
            type={inputTypes.TEXT}
            name="customEditionName"
            value={customName}
            autoFocus={true}
            helpText={translate('CustomEditionNameHelpText')}
            onChange={handleCustomChange}
          />
        </FormGroup>
      ) : null}
    </>
  );
}

export default EditionNameInput;
