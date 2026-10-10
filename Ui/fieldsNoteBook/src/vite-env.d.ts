/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Origin of the Field Notebook API, such as http://localhost:5240. */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
