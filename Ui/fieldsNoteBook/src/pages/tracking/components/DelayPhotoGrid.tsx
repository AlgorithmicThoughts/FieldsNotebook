import {
  DELAY_PHOTO_PURPOSE_LABEL,
  type DelayPhotoDetail,
  type DelayPhotoGroup,
} from '@/domain/delays'
import { formatProjectTime } from '@/lib/projectTime'

/** A photo keeps the coordinates it carried. Nothing substitutes a default. */
function locationLine(photo: DelayPhotoDetail): string {
  if (photo.latitude === null || photo.longitude === null) {
    return 'No GPS on this photo'
  }

  return `${photo.latitude.toFixed(4)}, ${photo.longitude.toFixed(4)}`
}

interface DelayPhotoGridProps {
  groups: DelayPhotoGroup[]
}

/** The delay's photos by purpose. A thumbnail opens its full-resolution file. */
export function DelayPhotoGrid({ groups }: DelayPhotoGridProps) {
  return (
    <section className="detail-section">
      <h3>Photos</h3>

      {groups.length === 0 ? <p>No photo is attached to this delay.</p> : null}

      {groups.map((group) => (
        <div key={group.purpose} className="detail-section">
          <p>{DELAY_PHOTO_PURPOSE_LABEL[group.purpose]}</p>

          <div className="detail-photos">
            {group.photos.map((photo) => (
              <a
                key={photo.id}
                href={photo.originalUrl}
                target="_blank"
                rel="noreferrer"
                className="detail-photo"
              >
                <img src={photo.thumbnailUrl} alt={photo.fileName} />
                <span>{photo.fileName}</span>
                <span>
                  {formatProjectTime(photo.createdAt)} · {locationLine(photo)}
                </span>
              </a>
            ))}
          </div>
        </div>
      ))}
    </section>
  )
}
