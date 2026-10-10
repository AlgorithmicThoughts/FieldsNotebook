import type { BlockedWorkRow, CompletionEstimate } from '@/domain/upcoming'
import { apiGet } from './client'
import type { RequestOptions } from './types'

/** The flat shape the API sends; the union it stands for lives in `@/domain/upcoming`. */
interface CompletionEstimateResponse {
  source: CompletionEstimate['source']
  /** ISO 8601 UTC, absent for the `none` source. */
  at: string | null
  /** The predecessor the estimate came from, for the `workItem` source only. */
  workItemTitle: string | null
}

interface BlockedWorkResponse extends Omit<BlockedWorkRow, 'completion'> {
  completion: CompletionEstimateResponse
}

interface UpcomingResponse {
  blockedWork: BlockedWorkResponse[]
}

function toCompletion(completion: CompletionEstimateResponse): CompletionEstimate {
  if (completion.source === 'delayCycle' && completion.at !== null) {
    return { source: 'delayCycle', at: completion.at }
  }

  if (completion.source === 'workItem' && completion.at !== null && completion.workItemTitle !== null) {
    return { source: 'workItem', at: completion.at, workItemTitle: completion.workItemTitle }
  }

  return { source: 'none' }
}

/**
 * GET /api/projects/{projectId}/upcoming. The API applies the rules (which
 * delays are still unresolved, which work the next project day plans for, and
 * which completion time each row reads), so the page needs one call.
 */
export async function getProjectUpcoming(
  projectId: number,
  options?: RequestOptions,
): Promise<BlockedWorkRow[]> {
  const upcoming = await apiGet<UpcomingResponse>(`/api/projects/${projectId}/upcoming`, options)

  return upcoming.blockedWork.map((row) => ({ ...row, completion: toCompletion(row.completion) }))
}
