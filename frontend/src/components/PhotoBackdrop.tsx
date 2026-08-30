const photoModules = import.meta.glob<string>('../assets/child-photos/*.{png,jpg,jpeg,webp,gif}', {
  eager: true,
  query: '?url',
  import: 'default'
});

const photos = Object.values(photoModules);
const repeatedPhotos = photos.length === 0
  ? []
  : Array.from({ length: Math.min(photos.length * 2, 14) }, (_, index) => photos[index % photos.length]);

export function PhotoBackdrop() {
  if (photos.length === 0) {
    return <div className="photoBackdrop photoBackdropNeutral" aria-hidden="true" />;
  }

  return (
    <div className="photoBackdrop" aria-hidden="true">
      {repeatedPhotos.map((photo, index) => (
        <img key={`${photo}-${index}`} src={photo} className={`backdropPhoto photo${index + 1}`} alt="" />
      ))}
    </div>
  );
}
