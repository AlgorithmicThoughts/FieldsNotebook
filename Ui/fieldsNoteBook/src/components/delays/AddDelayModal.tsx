import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Send } from 'lucide-react'
import { createDelay, getDelayFormOptions } from '@/api/delayReports'
import { isAbortError, isApiError } from '@/api/errors'
import type { CreateDelayRequest, DelayFormOptions, DelaySummary } from '@/domain/delays'
import type { CurrentUser } from '@/domain/users'
import { Button } from '@/components/common/Button'
import { Modal } from '@/components/common/Modal'
import { DelayForm } from './DelayForm'
import {
  createDelayDraft,
  reporterError,
  toCreateDelayRequest,
  validateDraft,
  type DelayDraft,
  type PhotoDraft,
} from './delayDraft'

const FORM_ID = 'add-delay-form'

function messageOf(error: unknown): string {
  return isApiError(error)
    ? error.message
    : 'Field Notebook could not save this report. Try again.'
}

function revokePreviews(photos: PhotoDraft[]): void {
  for (const photo of photos) {
    URL.revokeObjectURL(photo.previewUrl)
  }
}

interface AddDelayModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  user: CurrentUser
  projectId: number
  /** The caller adds the returned delay to its own list. */
  onCreated: (delay: DelaySummary) => void
}

/**
 * The delay report form. Every value stays local to this modal, and a failed
 * save keeps all of it, including the chosen photos.
 */
export function AddDelayModal(props: AddDelayModalProps) {
  return props.open ? <OpenDelayModal key={`${props.projectId}:${props.user.id}`} {...props} /> : null
}

function OpenDelayModal({
  open,
  onOpenChange,
  user,
  projectId,
  onCreated,
}: AddDelayModalProps) {
  const [options, setOptions] = useState<DelayFormOptions | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [draft, setDraft] = useState<DelayDraft>(() => createDelayDraft(user))
  const [hasSubmitted, setHasSubmitted] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [isReadingPhotos, setIsReadingPhotos] = useState(false)
  const [isTranscribing, setIsTranscribing] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const savingRef = useRef(false)
  const aliveRef = useRef(false)

  useEffect(() => {
    if (!open) {
      return
    }

    const controller = new AbortController()

    getDelayFormOptions(projectId, { signal: controller.signal })
      .then((next) => {
        if (controller.signal.aborted) return
        setOptions(next)
        setLoadError(null)
      })
      .catch((error: unknown) => {
        if (!isAbortError(error)) {
          setLoadError(messageOf(error))
        }
      })

    // A late answer for the previous project must not replace the current one.
    return () => controller.abort()
  }, [open, projectId])

  const photosRef = useRef<PhotoDraft[]>([])

  useEffect(() => {
    photosRef.current = draft.photos
  }, [draft.photos])

  useEffect(() => {
    // Five full-resolution phone photos are real memory.
    aliveRef.current = true
    return () => {
      aliveRef.current = false
      revokePreviews(photosRef.current)
    }
  }, [])

  const draftErrors = validateDraft(draft)
  const permissionError = reporterError(draft, user)
  const isValid = permissionError === null && Object.keys(draftErrors).length === 0
  const topError = saveError ?? loadError ?? (hasSubmitted ? permissionError : null)

  function changeDraft(patch: Partial<DelayDraft>) {
    setDraft((current) => ({ ...current, ...patch }))
  }

  function discardReport() {
    revokePreviews(draft.photos)
    photosRef.current = []
    setDraft(createDelayDraft(user))
    setHasSubmitted(false)
    setSaveError(null)
    setIsReadingPhotos(false)
  }

  function reportCreated(created: DelaySummary) {
    if (!aliveRef.current) return
    discardReport()
    onCreated(created)
    onOpenChange(false)
  }

  function reportFailed(error: unknown) {
    if (aliveRef.current && !isAbortError(error)) setSaveError(messageOf(error))
  }

  function finishSaving() {
    savingRef.current = false
    if (aliveRef.current) setIsSaving(false)
  }

  function save(request: CreateDelayRequest) {
    if (savingRef.current) return
    savingRef.current = true
    setIsSaving(true)
    setSaveError(null)

    void createDelay(request).then(reportCreated).catch(reportFailed).finally(finishSaving)
  }

  function changeOpen(next: boolean) {
    if (savingRef.current) return
    if (!next) discardReport()
    onOpenChange(next)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isReadingPhotos || isTranscribing || options === null) return
    setHasSubmitted(true)

    if (!isValid) return
    const request = toCreateDelayRequest(draft, options.projectId, options.siteMap?.id ?? null)
    if (request === null) return

    void save(request)
  }

  return (
    <Modal
      open={open}
      title="Report a delay"
      onOpenChange={changeOpen}
      actions={
        <>
          <Button label="Cancel" disabled={isSaving} onClick={() => changeOpen(false)} />
          <Button
            label={isSaving ? 'Sending…' : 'Send report'}
            variant="primary"
            icon={Send}
            iconPosition="end"
            type="submit"
            form={FORM_ID}
            // A second click must not create a second delay.
            disabled={isSaving || isReadingPhotos || isTranscribing || options === null}
          />
        </>
      }
    >
      {topError ? (
        <p
          role="alert"
          className="mb-[18px] rounded-[var(--radius-control)] bg-[var(--urgent-bg)] px-[14px] py-3 text-[13px] text-[var(--urgent)]"
        >
          {topError}
        </p>
      ) : null}

      {options === null ? (
        <p className="py-6 text-center text-[13px] text-muted-foreground">
          Loading the project's crews, work items, and site map…
        </p>
      ) : (
        <DelayForm
          formId={FORM_ID}
          options={options}
          draft={draft}
          errors={hasSubmitted ? draftErrors : {}}
          disabled={isSaving || isTranscribing}
          signedInUserId={user.id}
          reporterCrewIds={user.crews.map((crew) => crew.id)}
          onPhotoReadingChange={setIsReadingPhotos}
          onSpeechProcessingChange={setIsTranscribing}
          onDraftChange={changeDraft}
          onSubmit={handleSubmit}
        />
      )}
    </Modal>
  )
}
