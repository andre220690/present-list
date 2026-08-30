const photoModules = import.meta.glob<string>('../assets/child-photos/*.{png,jpg,jpeg,webp,gif}', {
  eager: true,
  query: '?url',
  import: 'default'
});

const photos = Object.values(photoModules);
const minBackdropTiles = 24;
const backdropTileCount = Math.ceil(Math.max(photos.length, minBackdropTiles) / 3) * 3;
const repeatedPhotos = photos.length === 0
  ? []
  : Array.from({ length: backdropTileCount }, (_, index) => photos[index % photos.length]);

export function PhotoBackdrop() {
  if (photos.length === 0) {
    return <div className="photoBackdrop photoBackdropNeutral" aria-hidden="true" />;
  }

  return (
    <div className="photoBackdrop" aria-hidden="true">
      {repeatedPhotos.map((photo, index) => (
        <img key={`${photo}-${index}`} src={photo} className="backdropPhoto" alt="" />
      ))}
    </div>
  );
}
