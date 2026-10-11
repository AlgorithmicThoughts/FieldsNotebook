import type { AuthCredentialRow, SiteMapRow } from "../types";

/**
 * The client-side stand-ins that remain until real endpoints replace them:
 * the demo sign-in credentials and the Documents page's site image. Column
 * names match the schema exactly.
 */

export const SITE_MAPS: SiteMapRow[] = [
  {
    Id: 1,
    ProjectId: 1,
    Name: "Omaha Fabrication site plan",
    Uri: "https://placehold.co/400x400/png",
    IsActive: true,
  },
];

export const AUTH_CREDENTIALS: AuthCredentialRow[] = [
  { UserId: 1, Password: "admin" },
  { UserId: 2, Password: "admin" },
  { UserId: 3, Password: "admin" },
  { UserId: 4, Password: "admin" },
  { UserId: 5, Password: "admin" },
  { UserId: 6, Password: "admin" },
  { UserId: 7, Password: "admin" },
  { UserId: 8, Password: "admin" },
];
