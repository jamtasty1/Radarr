import React from 'react';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import Column from 'Components/Table/Column';
import TableRow from 'Components/Table/TableRow';
import MovieSearchCell from 'Movie/MovieSearchCell';
import MovieStatus from 'Movie/MovieStatus';
import MovieTitleLink from 'Movie/MovieTitleLink';
import MovieFileLanguages from 'MovieFile/MovieFileLanguages';
import { SelectStateInputProps } from 'typings/props';
import styles from './CutoffUnmetRow.css';

interface CutoffUnmetRowProps {
  id: number;
  movieFileId?: number;
  inCinemas?: string;
  digitalRelease?: string;
  physicalRelease?: string;
  lastSearchTime?: string;
  title: string;
  year: number;
  titleSlug: string;
  // Set when the movie has extra editions: which of them are still wanted.
  wantedEditions?: string[];
  isSelected?: boolean;
  columns: Column[];
  onSelectedChange: (options: SelectStateInputProps) => void;
}

function CutoffUnmetRow({
  id,
  movieFileId,
  inCinemas,
  digitalRelease,
  physicalRelease,
  lastSearchTime,
  title,
  year,
  titleSlug,
  wantedEditions,
  isSelected,
  columns,
  onSelectedChange,
}: CutoffUnmetRowProps) {
  // A movie can be here for an extra edition alone, with no standard file.
  if (!movieFileId && !wantedEditions?.length) {
    return null;
  }

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChange}
      />

      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'movieMetadata.sortTitle') {
          return (
            <TableRowCell key={name}>
              <MovieTitleLink titleSlug={titleSlug} title={title} />
            </TableRowCell>
          );
        }

        if (name === 'movieMetadata.year') {
          return <TableRowCell key={name}>{year}</TableRowCell>;
        }

        if (name === 'movieMetadata.inCinemas') {
          return (
            <RelativeDateCell
              key={name}
              date={inCinemas}
              timeForToday={false}
            />
          );
        }

        if (name === 'movieMetadata.digitalRelease') {
          return (
            <RelativeDateCell
              key={name}
              date={digitalRelease}
              timeForToday={false}
            />
          );
        }

        if (name === 'movieMetadata.physicalRelease') {
          return (
            <RelativeDateCell
              key={name}
              date={physicalRelease}
              timeForToday={false}
            />
          );
        }

        if (name === 'languages') {
          return (
            <TableRowCell key={name} className={styles.languages}>
              {movieFileId ? (
                <MovieFileLanguages movieFileId={movieFileId} />
              ) : null}
            </TableRowCell>
          );
        }

        if (name === 'movies.lastSearchTime') {
          return (
            <RelativeDateCell
              key={name}
              date={lastSearchTime}
              includeSeconds={true}
            />
          );
        }

        if (name === 'editions') {
          return (
            <TableRowCell key={name} className={styles.editions}>
              {wantedEditions?.join(', ')}
            </TableRowCell>
          );
        }

        if (name === 'status') {
          return (
            <TableRowCell key={name} className={styles.status}>
              <MovieStatus
                movieId={id}
                movieFileId={movieFileId}
                movieEntity="wanted.cutoffUnmet"
              />
            </TableRowCell>
          );
        }

        if (name === 'actions') {
          return (
            <MovieSearchCell
              key={name}
              movieId={id}
              movieEntity="wanted.cutoffUnmet"
            />
          );
        }

        return null;
      })}
    </TableRow>
  );
}

export default CutoffUnmetRow;
