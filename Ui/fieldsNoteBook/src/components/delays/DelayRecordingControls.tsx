import { useEffect, useRef, useState } from 'react'
import { Mic, Send, Square, X } from 'lucide-react'
import { transcribeDelay } from '@/api/delayReports'
import { isAbortError, isApiError } from '@/api/errors'
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
  const recording = useRef<Blob | null>(null)
  const capture = useRef<AbortController | null>(null)
  const request = useRef<AbortController | null>(null)
  const stopCapture = useRef<(() => void) | null>(null)

  useEffect(() => () => {
    capture.current?.abort()
    request.current?.abort()
    recording.current = null
  }, [])

  function cancel() {
    capture.current?.abort()
    request.current?.abort()
    request.current = null
    recording.current = null
    onProcessingChange(false)
    setError(null)
    setState('idle')
  }

  async function start() {
    capture.current?.abort()
    const controller = new AbortController()
    capture.current = controller
    recording.current = null
    setError(null)
    setState('starting')
    try {
      stopCapture.current = await startAudioRecorder({
        onComplete(audio) {
          recording.current = audio
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
    stopCapture.current?.()
  }

  async function send() {
    if (recording.current === null || request.current !== null) return
    const controller = new AbortController()
    request.current = controller
    setError(null)
    setState('sending')
    onProcessingChange(true)
    try {
      const fields = await transcribeDelay(projectId, recording.current, { signal: controller.signal })
      completeSend(controller, fields)
    } catch (error: unknown) {
      failSend(controller, error)
    } finally {
      if (!controller.signal.aborted) {
        request.current = null
        onProcessingChange(false)
      }
    }
  }

  function completeSend(controller: AbortController, fields: DelaySpeechResponse) {
    if (controller.signal.aborted) return
    onTranscribed(fields)
    recording.current = null
    setState('idle')
  }

  function failSend(controller: AbortController, error: unknown) {
    if (isAbortError(error) || controller.signal.aborted) return
    setError(isApiError(error) ? error.message : 'Could not transcribe this recording. Try again.')
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
