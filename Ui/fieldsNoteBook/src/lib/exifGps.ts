/**
 * Reads EXIF GPS out of a JPEG. A photo without GPS is ordinary and the report
 * form asks for a location name or a map pin instead. Nothing here estimates,
 * interpolates, or otherwise invents a coordinate.
 */

export interface PhotoGps {
  latitude: number
  longitude: number
}

const JPEG_START_OF_IMAGE = 0xffd8
const APP1_MARKER = 0xffe1
/** The ASCII bytes 'Exif' that open an APP1 segment. */
const EXIF_SIGNATURE = 0x45786966
const LITTLE_ENDIAN_ORDER = 0x4949
const BIG_ENDIAN_ORDER = 0x4d4d
const GPS_IFD_POINTER_TAG = 0x8825
const GPS_LATITUDE_REF_TAG = 0x0001
const GPS_LATITUDE_TAG = 0x0002
const GPS_LONGITUDE_REF_TAG = 0x0003
const GPS_LONGITUDE_TAG = 0x0004
/** An IFD entry is tag, type, count, and value or pointer. */
const IFD_ENTRY_SIZE = 12
const RATIONAL_SIZE = 8
const NEGATIVE_HEMISPHERES = new Set(['S', 'W'])

interface TiffHeader {
  /** Byte offset of the TIFF header, which every pointer counts from. */
  start: number
  littleEndian: boolean
  firstIfd: number
}

function isApp1Exif(view: DataView, offset: number): boolean {
  return view.getUint16(offset) === APP1_MARKER && view.getUint32(offset + 4) === EXIF_SIGNATURE
}

/** The next JPEG segment, or null once the marker chain stops making sense. */
function nextSegmentOffset(view: DataView, offset: number): number | null {
  const length = view.getUint16(offset + 2)

  if ((view.getUint16(offset) & 0xff00) !== 0xff00 || length < 2) {
    return null
  }

  return offset + 2 + length
}

function findExifStart(view: DataView): number | null {
  if (view.byteLength < 4 || view.getUint16(0) !== JPEG_START_OF_IMAGE) {
    return null
  }

  let offset: number | null = 2

  while (offset !== null && offset + 10 <= view.byteLength) {
    if (isApp1Exif(view, offset)) {
      // Past the marker, the length, and the 'Exif\0\0' signature.
      return offset + 10
    }

    offset = nextSegmentOffset(view, offset)
  }

  return null
}

function readTiffHeader(view: DataView, start: number): TiffHeader | null {
  const byteOrder = view.getUint16(start)

  if (byteOrder !== LITTLE_ENDIAN_ORDER && byteOrder !== BIG_ENDIAN_ORDER) {
    return null
  }

  const littleEndian = byteOrder === LITTLE_ENDIAN_ORDER

  return { start, littleEndian, firstIfd: view.getUint32(start + 4, littleEndian) }
}

function findIfdEntry(
  view: DataView,
  header: TiffHeader,
  ifdOffset: number,
  tag: number,
): number | null {
  const base = header.start + ifdOffset
  const entryCount = view.getUint16(base, header.littleEndian)

  for (let index = 0; index < entryCount; index += 1) {
    const entry = base + 2 + index * IFD_ENTRY_SIZE

    if (view.getUint16(entry, header.littleEndian) === tag) {
      return entry
    }
  }

  return null
}

function readRational(view: DataView, header: TiffHeader, offset: number): number {
  const numerator = view.getUint32(offset, header.littleEndian)
  const denominator = view.getUint32(offset + 4, header.littleEndian)

  return denominator === 0 ? 0 : numerator / denominator
}

/** Degrees, minutes, and seconds as three rationals, combined into degrees. */
function readDegrees(view: DataView, header: TiffHeader, entry: number): number | null {
  const values = header.start + view.getUint32(entry + 8, header.littleEndian)

  if (values + RATIONAL_SIZE * 3 > view.byteLength) {
    return null
  }

  return (
    readRational(view, header, values) +
    readRational(view, header, values + RATIONAL_SIZE) / 60 +
    readRational(view, header, values + RATIONAL_SIZE * 2) / 3600
  )
}

/** A hemisphere letter is two ASCII bytes, so it sits inside the entry. */
function readHemisphere(
  view: DataView,
  header: TiffHeader,
  gpsIfd: number,
  tag: number,
): string | null {
  const entry = findIfdEntry(view, header, gpsIfd, tag)

  return entry === null ? null : String.fromCharCode(view.getUint8(entry + 8))
}

function readCoordinate(
  view: DataView,
  header: TiffHeader,
  gpsIfd: number,
  valueTag: number,
  hemisphereTag: number,
): number | null {
  const entry = findIfdEntry(view, header, gpsIfd, valueTag)
  const hemisphere = readHemisphere(view, header, gpsIfd, hemisphereTag)

  if (entry === null || hemisphere === null) {
    return null
  }

  const degrees = readDegrees(view, header, entry)

  if (degrees === null) {
    return null
  }

  return NEGATIVE_HEMISPHERES.has(hemisphere) ? -degrees : degrees
}

function readGps(view: DataView): PhotoGps | null {
  const exifStart = findExifStart(view)
  const header = exifStart === null ? null : readTiffHeader(view, exifStart)

  if (header === null) {
    return null
  }

  const pointer = findIfdEntry(view, header, header.firstIfd, GPS_IFD_POINTER_TAG)

  if (pointer === null) {
    return null
  }

  const gpsIfd = view.getUint32(pointer + 8, header.littleEndian)
  const latitude = readCoordinate(view, header, gpsIfd, GPS_LATITUDE_TAG, GPS_LATITUDE_REF_TAG)
  const longitude = readCoordinate(view, header, gpsIfd, GPS_LONGITUDE_TAG, GPS_LONGITUDE_REF_TAG)

  if (latitude === null || longitude === null) {
    return null
  }

  return { latitude, longitude }
}

/**
 * GPS from the file, or null when the file carries none. HEIC and PNG have no
 * JPEG marker chain, so they read as null rather than as an error.
 */
export async function readPhotoGps(file: File): Promise<PhotoGps | null> {
  try {
    return readGps(new DataView(await file.arrayBuffer()))
  } catch {
    // A truncated or unusual file is not a report failure. Treat it as no GPS.
    return null
  }
}
