import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Images, MapPin, Trash2 } from 'lucide-react'
import { readPhotoGps } from '@/lib/exifGps'
import { Button, IconButton } from '@/components/common/Button'
import { Icon } from '@/components/common/Icon'
import { MAX_PHOTOS, type PhotoDraft } from './delayDraft'

/** The approved upload formats. */
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
  onReadingChange: (reading: boolean) => void
}

/** One to five photos for a report or a resolution's proof of work, with EXIF GPS when a file has it. */
export function PhotoPicker({ photos, onChange, disabled, onReadingChange }: PhotoPickerProps) {
  const libraryInputRef = useRef<HTMLInputElement>(null)
  const aliveRef = useRef(false)
  const readingRef = useRef(false)
  const [isReading, setIsReading] = useState(false)

  useEffect(() => {
    aliveRef.current = true
    return () => { aliveRef.current = false }
  }, [])

  function finishRead(added: PhotoDraft[]) {
    if (!aliveRef.current) {
      added.forEach((photo) => URL.revokeObjectURL(photo.previewUrl))
      return
    }
    onChange([...photos, ...added])
    setIsReading(false)
    onReadingChange(false)
  }

  async function addFiles(event: ChangeEvent<HTMLInputElement>) {
    if (readingRef.current || disabled) return
    readingRef.current = true
    setIsReading(true)
    onReadingChange(true)
    const chosen = Array.from(event.target.files ?? []).slice(0, MAX_PHOTOS - photos.length)

    // Clearing lets the same file be chosen again after a removal.
    event.target.value = ''

    try {
      finishRead(await Promise.all(chosen.map(toPhotoDraft)))
    } finally {
      readingRef.current = false
    }
  }

  function removePhoto(photo: PhotoDraft) {
    URL.revokeObjectURL(photo.previewUrl)
    onChange(photos.filter((candidate) => candidate.id !== photo.id))
  }

  const isFull = photos.length >= MAX_PHOTOS
  const interactionDisabled = disabled || isReading
  const uploadDisabled = interactionDisabled || isFull

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
                disabled={interactionDisabled}
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
            label="Add photo"
            icon={Images}
            disabled={uploadDisabled}
            onClick={() => libraryInputRef.current?.click()}
          />
        </div>
        <p className="mt-[10px] text-[12px] text-muted-foreground">
          At least 1 photo · Maximum {MAX_PHOTOS} · JPG, PNG, or HEIC
        </p>
      </div>

      <input
        ref={libraryInputRef}
        type="file"
        accept={ACCEPTED_TYPES}
        multiple
        hidden
        onChange={(event) => void addFiles(event)}
      />
    </div>
  )
}
