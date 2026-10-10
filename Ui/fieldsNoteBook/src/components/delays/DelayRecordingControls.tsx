import { useEffect, useRef, useState } from 'react'
import { Mic, Send, Square, X } from 'lucide-react'
import { Button, IconButton } from '@/components/common/Button'

const RECORDING_SECONDS = 20

/** Audio stays local until the recording API is available. */
export function DelayRecordingControls({ disabled }: { disabled: boolean }) {
  const [state, setState] = useState<'idle' | 'starting' | 'recording' | 'ready'>('idle')
  const [remaining, setRemaining] = useState(RECORDING_SECONDS)
  const [error, setError] = useState<string | null>(null)
  const recording = useRef<Blob | null>(null)
  const recorder = useRef<MediaRecorder | null>(null)
  const stream = useRef<MediaStream | null>(null)
  const timer = useRef<ReturnType<typeof setInterval> | null>(null)
  const deadline = useRef<ReturnType<typeof setTimeout> | null>(null)
  const alive = useRef(false)
  const cancelled = useRef(false)
  const starting = useRef(false)

  function releaseCapture() {
    if (timer.current !== null) clearInterval(timer.current)
    if (deadline.current !== null) clearTimeout(deadline.current)
    stream.current?.getTracks().forEach((track) => track.stop())
    stream.current = null
  }

  function stop() {
    if (recorder.current?.state === 'recording') recorder.current.stop()
    releaseCapture()
  }

  function cancel() {
    cancelled.current = true
    stop()
    recording.current = null
    setState('idle')
    setRemaining(RECORDING_SECONDS)
  }

  useEffect(() => {
    alive.current = true
    return () => {
      alive.current = false
      cancelled.current = true
      if (recorder.current?.state === 'recording') recorder.current.stop()
      if (timer.current !== null) clearInterval(timer.current)
      if (deadline.current !== null) clearTimeout(deadline.current)
      stream.current?.getTracks().forEach((track) => track.stop())
      recording.current = null
    }
  }, [])

  function beginCapture(capture: MediaStream) {
    const chunks: Blob[] = []
    const next = new MediaRecorder(capture)
    recorder.current = next
    next.ondataavailable = (event) => { if (event.data.size > 0) chunks.push(event.data) }
    next.onstop = () => {
      if (!alive.current || cancelled.current) return
      recording.current = new Blob(chunks, { type: next.mimeType })
      setState('ready')
    }
    next.onerror = () => {
      cancelled.current = true
      releaseCapture()
      if (!alive.current) return
      setError('Recording stopped. Try again.')
      setState('idle')
    }
    next.start()
    const endsAt = Date.now() + RECORDING_SECONDS * 1000
    timer.current = setInterval(() => setRemaining(Math.max(0, Math.ceil((endsAt - Date.now()) / 1000))), 200)
    deadline.current = setTimeout(stop, RECORDING_SECONDS * 1000)
    setState('recording')
  }

  async function start() {
    if (starting.current) return
    starting.current = true
    cancelled.current = false
    recording.current = null
    setError(null)
    setRemaining(RECORDING_SECONDS)
    setState('starting')
    try {
      stream.current = await navigator.mediaDevices.getUserMedia({ audio: true })
      if (!alive.current || cancelled.current) { releaseCapture(); return }
      beginCapture(stream.current)
    } catch {
      releaseCapture()
      if (!alive.current) return
      setError('Microphone recording is unavailable or access was denied.')
      setState('idle')
    } finally {
      starting.current = false
    }
  }

  return (
    <div className="flex flex-wrap items-center justify-end gap-2 text-[12px]">
      {state === 'idle' ? <Button type="button" label="Speak" icon={Mic} variant="quiet" disabled={disabled} onClick={() => void start()} /> : null}
      {state === 'starting' ? <span role="status">Starting microphone…</span> : null}
      {state === 'recording' ? <>
        <span role="timer" aria-label="Recording time remaining">{remaining}s</span>
        <Button type="button" label="Stop" icon={Square} disabled={disabled} onClick={stop} />
      </> : null}
      {state === 'ready' ? <>
        <span role="status">Recording ready</span>
        <IconButton type="button" label="Send recording (API not available yet)" icon={Send} disabled title="Recording API is not available yet" />
      </> : null}
      {state !== 'idle' ? <IconButton type="button" label="Cancel recording" icon={X} disabled={disabled} onClick={cancel} /> : null}
      {error ? <span role="alert" className="text-[var(--urgent)]">{error}</span> : null}
    </div>
  )
}
