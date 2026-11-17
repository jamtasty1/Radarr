// Edition names offered when adding an edition, most common first. Anything
// else can be typed in with the custom option.
export const COMMON_EDITION_NAMES = [
  'Extended',
  "Director's Cut",
  'Unrated',
  'Uncut',
  'IMAX',
  'Final Cut',
  'Ultimate',
  'Special Edition',
  "Collector's Edition",
  'Anniversary Edition',
  'Criterion',
  'Open Matte',
];

// Only usable once the standard edition is named for a cut, since until then
// untagged and theatrical releases are the standard edition.
export const THEATRICAL_EDITION_NAME = 'Theatrical';

export const CUSTOM_EDITION_KEY = '__custom__';

// Compare names the way people write them: case, apostrophes and the words
// "cut" and "edition" don't matter ("Directors Cut" = "Director's Cut").
export function getEditionNameKey(name: string | null | undefined) {
  return (name ?? '')
    .toLowerCase()
    .replace(/['’]/g, '')
    .split(/[^a-z0-9]+/)
    .filter((w) => w && w !== 'cut' && w !== 'edition' && w !== 'the')
    .sort()
    .join(' ');
}

export function findCommonEditionName(name: string | null | undefined) {
  const key = getEditionNameKey(name);

  if (!key) {
    return undefined;
  }

  return [...COMMON_EDITION_NAMES, THEATRICAL_EDITION_NAME].find(
    (n) => getEditionNameKey(n) === key
  );
}
