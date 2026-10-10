/** Preserve proportions and keep the thumbnail within 400 by 400 pixels. */
export async function createPhotoThumbnail(file: File): Promise<Blob> {
  const image = await createImageBitmap(file, { imageOrientation: 'from-image' })
  try {
    const scale = Math.min(1, 400 / Math.max(image.width, image.height))
    const canvas = document.createElement('canvas')
    canvas.width = Math.max(1, Math.round(image.width * scale))
    canvas.height = Math.max(1, Math.round(image.height * scale))
    const context = canvas.getContext('2d')
    if (context === null) throw new Error('Photo thumbnail could not be created.')
    context.drawImage(image, 0, 0, canvas.width, canvas.height)
    return await new Promise<Blob>((resolve, reject) => canvas.toBlob(
      (blob) => blob === null ? reject(new Error('Photo thumbnail could not be created.')) : resolve(blob),
      'image/jpeg', 0.85,
    ))
  } finally {
    image.close()
  }
}
