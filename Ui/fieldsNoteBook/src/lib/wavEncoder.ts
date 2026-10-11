export const WAV_SAMPLE_RATE = 16_000

// Package mono float samples as 16-bit PCM WAV for the API's Speech SDK file input.
export function encodeWav(samples: Float32Array): Blob {
  const buffer = new ArrayBuffer(44 + samples.length * 2)
  const view = new DataView(buffer)
  const text = (offset: number, value: string) => {
    for (let index = 0; index < value.length; index++) view.setUint8(offset + index, value.charCodeAt(index))
  }
  text(0, 'RIFF')
  view.setUint32(4, buffer.byteLength - 8, true)
  text(8, 'WAVE')
  text(12, 'fmt ')
  view.setUint32(16, 16, true)
  view.setUint16(20, 1, true) // PCM
  view.setUint16(22, 1, true) // Mono
  view.setUint32(24, WAV_SAMPLE_RATE, true)
  view.setUint32(28, WAV_SAMPLE_RATE * 2, true)
  view.setUint16(32, 2, true)
  view.setUint16(34, 16, true)
  text(36, 'data')
  view.setUint32(40, samples.length * 2, true)
  samples.forEach((value, index) => {
    const sample = Math.max(-1, Math.min(1, value))
    view.setInt16(44 + index * 2, Math.round(sample * (sample < 0 ? 0x8000 : 0x7fff)), true)
  })
  return new Blob([buffer], { type: 'audio/wav' })
}
