/** Every client function accepts this, so a page can cancel an obsolete call. */
export interface RequestOptions {
  signal?: AbortSignal
}

/**
 * Table-shaped rows for the client-side stand-ins that remain, named exactly
 * as the database columns are.
 */

/**
 * Sign-in rows for the mock API. Users holds no credential column, so the
 * password lives only in the fixtures until a real identity provider exists.
 */
export interface AuthCredentialRow {
  UserId: number
  Password: string
}

export interface SiteMapRow {
  Id: number
  ProjectId: number
  Name: string
  Uri: string
  IsActive: boolean
}
