/* global AudioWorkletProcessor, registerProcessor */
class PcmRecorder extends AudioWorkletProcessor {
  constructor() {
    super()
    this.chunks = []
    this.finished = false
    this.port.onmessage = ({ data }) => {
      if (data === 'stop') this.finish()
    }
  }

  finish() {
    if (this.finished) return
    this.finished = true
    const length = this.chunks.reduce((total, chunk) => total + chunk.length, 0)
    const samples = new Float32Array(length)
    let offset = 0
    for (const chunk of this.chunks) {
      samples.set(chunk, offset)
      offset += chunk.length
    }
    this.chunks = []
    this.port.postMessage(samples, [samples.buffer])
  }

  process(inputs) {
    if (this.finished) return false
    const channel = inputs[0]?.[0]
    if (channel) this.chunks.push(channel.slice())
    return true
  }
}

registerProcessor('pcm-recorder', PcmRecorder)
