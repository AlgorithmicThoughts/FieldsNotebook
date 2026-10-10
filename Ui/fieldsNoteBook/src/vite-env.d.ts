/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
  /** Local-development identity sent as X-Demo-User-Id. */
  readonly VITE_DEMO_USER_ID?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
