import { useEffect, useRef, useState } from 'react'
import { Mic, Send, Square, X } from 'lucide-react'
import { transcribeDelay } from '@/api/delayReports'
import { errorMessage, isAbortError } from '@/api/errors'
import type { DelaySpeechResponse } from '@/domain/delays'
import { startAudioRecorder } from '@/lib/audioRecorder'
import { Button, IconButton } from '@/components/common/Button'

interface DelayRecordingControlsProps {
  disabled: boolean
  projectId: number
  onTranscribed: (fields: DelaySpeechResponse) => void
  onProcessingChange: (processing: boolean) => void
}

export function DelayRecordingControls({
  disabled, projectId, onTranscribed, onProcessingChange,
}: DelayRecordingControlsProps) {
  const [state, setState] = useState<'idle' | 'starting' | 'recording' | 'stopping' | 'ready' | 'sending'>('idle')
  const [error, setError] = useState<string | null>(null)
  const recordingRef = useRef<Blob | null>(null)
  const captureRef = useRef<AbortController | null>(null)
  const requestRef = useRef<AbortController | null>(null)
  const stopCaptureRef = useRef<(() => void) | null>(null)

  useEffect(() => () => {
    captureRef.current?.abort()
    requestRef.current?.abort()
    recordingRef.current = null
  }, [])

  function cancel() {
    captureRef.current?.abort()
    requestRef.current?.abort()
    requestRef.current = null
    recordingRef.current = null
    onProcessingChange(false)
    setError(null)
    setState('idle')
  }

  async function start() {
    captureRef.current?.abort()
    const controller = new AbortController()
    captureRef.current = controller
    recordingRef.current = null
    setError(null)
    setState('starting')
    try {
      stopCaptureRef.current = await startAudioRecorder({
        onComplete(audio) {
          recordingRef.current = audio
          setState('ready')
        },
        onError(message) {
          setError(message)
          setState('idle')
        },
      }, controller.signal)
      if (!controller.signal.aborted) setState('recording')
    } catch {
      if (controller.signal.aborted) return
      setError('Microphone recording is unavailable or access was denied.')
      setState('idle')
    }
  }

  function stop() {
    setState('stopping')
    stopCaptureRef.current?.()
  }

  async function send() {
    if (recordingRef.current === null || requestRef.current !== null) return
    const controller = new AbortController()
    requestRef.current = controller
    setError(null)
    setState('sending')
    onProcessingChange(true)
    try {
      const fields = await transcribeDelay(projectId, recordingRef.current, { signal: controller.signal })
      completeSend(controller, fields)
    } catch (error: unknown) {
      failSend(controller, error)
    } finally {
      if (!controller.signal.aborted) {
        requestRef.current = null
        onProcessingChange(false)
      }
    }
  }

  function completeSend(controller: AbortController, fields: DelaySpeechResponse) {
    if (controller.signal.aborted) return
    onTranscribed(fields)
    recordingRef.current = null
    setState('idle')
  }

  function failSend(controller: AbortController, error: unknown) {
    if (isAbortError(error) || controller.signal.aborted) return
    setError(errorMessage(error, 'Could not transcribe this recording. Try again.'))
    setState('ready')
  }

  return (
    <div className="flex flex-wrap items-center justify-end gap-2 text-[12px]">
      {state === 'idle' ? <Button type="button" label="Speak" icon={Mic} variant="quiet" disabled={disabled} onClick={() => void start()} /> : null}
      {state === 'starting' ? <span role="status">Starting microphone…</span> : null}
      {state === 'recording' ? <>
        <span role="status">Recording…</span>
        <Button type="button" label="Stop" icon={Square} disabled={disabled} onClick={stop} />
      </> : null}
      {state === 'stopping' ? <span role="status">Preparing recording…</span> : null}
      {state === 'ready' ? <>
        <span role="status">Recording ready</span>
        <IconButton type="button" label="Send recording" icon={Send} disabled={disabled} onClick={() => void send()} />
      </> : null}
      {state === 'sending' ? <span role="status">Transcribing…</span> : null}
      {state !== 'idle' ? <IconButton type="button" label="Cancel recording" icon={X} disabled={disabled} onClick={cancel} /> : null}
      {error ? <span role="alert" className="text-[var(--urgent)]">{error}</span> : null}
    </div>
  )
}
