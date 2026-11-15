import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Modal from 'Components/Modal/Modal';
import { useMovieEditions } from 'Movie/Editions/useMovieEditions';
import FileEditModalContentConnector from './FileEditModalContentConnector';

function FileEditModalContentWithEditions(props) {
  const { data: editions = [], isLoading } = useMovieEditions(props.movieId ?? 0);

  return (
    <FileEditModalContentConnector
      {...props}
      editions={editions}
      isEditionsLoading={isLoading}
    />
  );
}

FileEditModalContentWithEditions.propTypes = {
  movieId: PropTypes.number
};

class FileEditModal extends Component {

  //
  // Render

  render() {
    const {
      isOpen,
      onModalClose,
      ...otherProps
    } = this.props;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <FileEditModalContentWithEditions
          {...otherProps}
          onModalClose={onModalClose}
        />
      </Modal>
    );
  }
}

FileEditModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default FileEditModal;
