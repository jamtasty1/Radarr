import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import useApiQuery, { ApiError } from 'Helpers/Hooks/useApiQuery';
import { fetchMovieFiles } from 'Store/Actions/movieFileActions';
import translate from 'Utilities/String/translate';
import {
  MovieEdition,
  MovieEditionInput,
  STANDARD_EDITION_ID,
} from './MovieEdition';

const apiRoot = window.Radarr.apiRoot;

function getEditionsPath(movieId: number) {
  return `/movieedition?movieId=${movieId}`;
}

async function sendRequest<T>(
  path: string,
  method: 'POST' | 'PUT' | 'DELETE',
  body?: unknown
): Promise<T | undefined> {
  const url = apiRoot + path;

  const response = await fetch(url, {
    method,
    headers: {
      'Content-Type': 'application/json',
      'X-Api-Key': window.Radarr.apiKey,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    // eslint-disable-next-line init-declarations
    let errorBody;

    try {
      errorBody = await response.json();
    } catch {
      throw new ApiError(url, response.status, response.statusText);
    }

    throw new ApiError(url, response.status, response.statusText, errorBody);
  }

  const text = await response.text();

  return text ? (JSON.parse(text) as T) : undefined;
}

export function useMovieEditions(movieId: number) {
  return useApiQuery<MovieEdition[]>({
    path: getEditionsPath(movieId),
    queryOptions: {
      enabled: movieId > 0,
    },
  });
}

// Adding or removing an edition can move files between slots, so refresh the
// movie's files alongside the editions themselves.
function useRefreshEditions(movieId: number) {
  const queryClient = useQueryClient();
  const dispatch = useDispatch();

  return useCallback(() => {
    queryClient.invalidateQueries({
      queryKey: [apiRoot + getEditionsPath(movieId)],
    });

    dispatch(fetchMovieFiles({ movieId }));
  }, [queryClient, dispatch, movieId]);
}

export function useSaveMovieEdition(movieId: number) {
  const refresh = useRefreshEditions(movieId);

  return useMutation<MovieEdition | undefined, ApiError, MovieEditionInput>({
    mutationFn: (edition) =>
      edition.id
        ? sendRequest<MovieEdition>(
            `/movieedition/${edition.id}`,
            'PUT',
            edition
          )
        : sendRequest<MovieEdition>('/movieedition', 'POST', edition),
    onSuccess: refresh,
  });
}

export function useDeleteMovieEdition(movieId: number) {
  const refresh = useRefreshEditions(movieId);

  return useMutation<unknown, ApiError, number>({
    mutationFn: (id) => sendRequest(`/movieedition/${id}`, 'DELETE'),
    onSuccess: refresh,
  });
}

export interface EditionOption {
  key: number;
  value: string;
}

// Options for choosing which of a movie's editions a file fills.
export function getEditionOptions(
  editions: MovieEdition[] = []
): EditionOption[] {
  return [
    { key: STANDARD_EDITION_ID, value: translate('StandardEdition') },
    ...editions.map(({ id, name }) => ({ key: id, value: name })),
  ];
}

export function getEditionName(
  editions: MovieEdition[] | undefined,
  movieEditionId: number | undefined
) {
  if (!movieEditionId) {
    return translate('StandardEdition');
  }

  return editions?.find((e) => e.id === movieEditionId)?.name ?? '';
}
