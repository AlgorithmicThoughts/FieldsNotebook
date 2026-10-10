import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Send } from 'lucide-react'
import { createDelay, getDelayFormOptions } from '@/api/delays'
import { isAbortError, isApiError } from '@/api/errors'
import type { CreateDelayRequest, DelayFormOptions, DelaySummary } from '@/domain/delays'
import type { CurrentUser } from '@/domain/users'
import { Button } from '@/components/common/Button'
import { Modal } from '@/components/common/Modal'
import { BlockerForm } from './BlockerForm'
import {
  EMPTY_BLOCKER_DRAFT,
  reporterError,
  toCreateDelayRequest,
  validateDraft,
  type BlockerDraft,
  type PhotoDraft,
} from './blockerDraft'

const FORM_ID = 'add-blocker-form'

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

interface AddBlockerModalProps {
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
export function AddBlockerModal({
  open,
  onOpenChange,
  user,
  projectId,
  onCreated,
}: AddBlockerModalProps) {
  const [options, setOptions] = useState<DelayFormOptions | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [draft, setDraft] = useState<BlockerDraft>(EMPTY_BLOCKER_DRAFT)
  const [hasSubmitted, setHasSubmitted] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) {
      return
    }

    const controller = new AbortController()

    getDelayFormOptions(projectId, { signal: controller.signal })
      .then((next) => {
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
    return () => revokePreviews(photosRef.current)
  }, [])

  const draftErrors = validateDraft(draft)
  const permissionError = reporterError(draft, user)
  const topError = saveError ?? loadError ?? (hasSubmitted ? permissionError : null)

  function changeDraft(patch: Partial<BlockerDraft>) {
    setDraft((current) => ({ ...current, ...patch }))
  }

  async function save(request: CreateDelayRequest) {
    setIsSaving(true)
    setSaveError(null)

    try {
      const created = await createDelay(request)

      revokePreviews(draft.photos)
      setDraft(EMPTY_BLOCKER_DRAFT)
      setHasSubmitted(false)
      onCreated(created)
      onOpenChange(false)
    } catch (error) {
      // Cancellation is not a failure, and a failure keeps every entered value.
      if (!isAbortError(error)) {
        setSaveError(messageOf(error))
      }
    } finally {
      setIsSaving(false)
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setHasSubmitted(true)

    const request =
      options === null
        ? null
        : toCreateDelayRequest(draft, options.projectId, options.siteMap?.id ?? null)

    if (request === null || permissionError !== null || Object.keys(draftErrors).length > 0) {
      return
    }

    void save(request)
  }

  return (
    <Modal
      open={open}
      title="Report a blocker"
      onOpenChange={onOpenChange}
      actions={
        <>
          <Button label="Cancel" disabled={isSaving} onClick={() => onOpenChange(false)} />
          <Button
            label={isSaving ? 'Sending…' : 'Send report'}
            variant="primary"
            icon={Send}
            iconPosition="end"
            type="submit"
            form={FORM_ID}
            // A second click must not create a second delay.
            disabled={isSaving || options === null}
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
        <BlockerForm
          formId={FORM_ID}
          options={options}
          draft={draft}
          errors={hasSubmitted ? draftErrors : {}}
          disabled={isSaving}
          signedInUserId={user.id}
          onDraftChange={changeDraft}
          onSubmit={handleSubmit}
        />
      )}
    </Modal>
  )
}
