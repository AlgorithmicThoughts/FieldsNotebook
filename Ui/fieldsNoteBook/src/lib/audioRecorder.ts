import { encodeWav, WAV_SAMPLE_RATE } from './wavEncoder'

interface RecordingCallbacks {
  onComplete: (audio: Blob) => void
  onError: (message: string) => void
}

/** Capture PCM audio for the same WAV input used by the Sound Demo. */
export async function startAudioRecorder(
  callbacks: RecordingCallbacks,
  signal: AbortSignal,
): Promise<() => void> {
  const context = new AudioContext({ sampleRate: WAV_SAMPLE_RATE })
  let stream: MediaStream | undefined
  let source: MediaStreamAudioSourceNode | undefined
  let processor: AudioWorkletNode | undefined
  let closed = false

  function cleanup() {
    closed = true
    signal.removeEventListener('abort', cleanup)
    stream?.getTracks().forEach((track) => { track.onended = null; track.stop() })
    source?.disconnect()
    processor?.disconnect()
    if (context.state !== 'closed') void context.close().catch(() => {})
  }

  function fail() {
    if (closed) return
    cleanup()
    if (!signal.aborted) callbacks.onError('Recording stopped. Try again.')
  }

  signal.addEventListener('abort', cleanup, { once: true })
  try {
    signal.throwIfAborted()
    await context.resume()
    stream = await navigator.mediaDevices.getUserMedia({ audio: { channelCount: 1 } })
    signal.throwIfAborted()
    await context.audioWorklet.addModule(`${import.meta.env.BASE_URL}pcm-recorder.js`)
    signal.throwIfAborted()
    source = context.createMediaStreamSource(stream)
    processor = new AudioWorkletNode(context, 'pcm-recorder', { channelCount: 1, channelCountMode: 'explicit' })
    processor.port.onmessage = ({ data }: MessageEvent<Float32Array>) => {
      if (closed) return
      const audio = encodeWav(data)
      cleanup()
      callbacks.onComplete(audio)
    }
    processor.onprocessorerror = fail
    stream.getTracks().forEach((track) => { track.onended = fail })
    source.connect(processor)
    // The worklet emits silence; connecting it keeps capture processing active.
    processor.connect(context.destination)

    return () => {
      void context.resume().then(() => {
        if (!closed) processor?.port.postMessage('stop')
      }).catch(fail)
    }
  } catch (error) {
    cleanup()
    throw error
  }
}
