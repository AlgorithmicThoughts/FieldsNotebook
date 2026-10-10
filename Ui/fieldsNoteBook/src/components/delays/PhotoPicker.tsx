import { useRef, type ChangeEvent } from 'react'
import { Camera, Images, MapPin, Trash2 } from 'lucide-react'
import { readPhotoGps } from '@/lib/exifGps'
import { Button, IconButton } from '@/components/common/Button'
import { Icon } from '@/components/common/Icon'
import { MAX_PHOTOS, type PhotoDraft } from './blockerDraft'

/** The approved upload formats. A phone camera supplies JPG or HEIC. */
const ACCEPTED_TYPES = 'image/jpeg,image/png,image/heic,image/heif'

async function toPhotoDraft(file: File): Promise<PhotoDraft> {
  const gps = await readPhotoGps(file)

  return {
    id: crypto.randomUUID(),
    file,
    previewUrl: URL.createObjectURL(file),
    latitude: gps?.latitude ?? null,
    longitude: gps?.longitude ?? null,
  }
}

function gpsLabel(photo: PhotoDraft): string {
  if (photo.latitude === null || photo.longitude === null) {
    return 'No photo GPS'
  }

  return `${photo.latitude.toFixed(5)}, ${photo.longitude.toFixed(5)}`
}

interface PhotoPickerProps {
  photos: PhotoDraft[]
  onChange: (photos: PhotoDraft[]) => void
  disabled?: boolean
}

/** One to five photos on the initial report, with EXIF GPS when a file has it. */
export function PhotoPicker({ photos, onChange, disabled }: PhotoPickerProps) {
  const cameraInput = useRef<HTMLInputElement>(null)
  const libraryInput = useRef<HTMLInputElement>(null)

  async function addFiles(event: ChangeEvent<HTMLInputElement>) {
    const chosen = Array.from(event.target.files ?? []).slice(0, MAX_PHOTOS - photos.length)

    // Clearing lets the same file be chosen again after a removal.
    event.target.value = ''

    onChange([...photos, ...(await Promise.all(chosen.map(toPhotoDraft)))])
  }

  function removePhoto(photo: PhotoDraft) {
    URL.revokeObjectURL(photo.previewUrl)
    onChange(photos.filter((candidate) => candidate.id !== photo.id))
  }

  const isFull = photos.length >= MAX_PHOTOS

  return (
    <div className="grid gap-3">
      {photos.length > 0 ? (
        <ul className="grid grid-cols-3 gap-[10px]">
          {photos.map((photo) => (
            <li
              key={photo.id}
              className="relative overflow-hidden rounded-[var(--radius-control)] border border-border bg-[var(--raised)]"
            >
              <img
                src={photo.previewUrl}
                alt={photo.file.name}
                className="block aspect-square w-full object-cover"
              />
              <IconButton
                label={`Remove ${photo.file.name}`}
                icon={Trash2}
                className="absolute top-1 right-1 bg-[var(--card)]"
                onClick={() => removePhoto(photo)}
                disabled={disabled}
              />
              <div className="grid gap-1 px-2 py-2 text-[12px]">
                <strong className="truncate font-[650]">{photo.file.name}</strong>
                <span className="flex items-center gap-1 text-muted-foreground">
                  <Icon icon={MapPin} size={13} />
                  {gpsLabel(photo)}
                </span>
              </div>
            </li>
          ))}
        </ul>
      ) : null}

      <div className="rounded-[12px] border border-dashed border-input bg-[var(--bg)] p-[18px] text-center">
        <div className="flex flex-wrap justify-center gap-[10px]">
          <Button
            label="Take photo"
            icon={Camera}
            disabled={disabled || isFull}
            onClick={() => cameraInput.current?.click()}
          />
          <Button
            label="Add photos"
            icon={Images}
            disabled={disabled || isFull}
            onClick={() => libraryInput.current?.click()}
          />
        </div>
        <p className="mt-[10px] text-[12px] text-muted-foreground">
          At least 1 photo · Maximum {MAX_PHOTOS} · JPG, PNG, or HEIC
        </p>
      </div>

      <input
        ref={cameraInput}
        type="file"
        accept={ACCEPTED_TYPES}
        capture="environment"
        hidden
        onChange={addFiles}
      />
      <input
        ref={libraryInput}
        type="file"
        accept={ACCEPTED_TYPES}
        multiple
        hidden
        onChange={addFiles}
      />
    </div>
  )
}
