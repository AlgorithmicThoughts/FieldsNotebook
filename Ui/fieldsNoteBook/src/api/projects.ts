import type { ProjectSummary } from '@/domain/projects'
import { mockRequest } from './client'
import { ApiError } from './errors'
import { DISTRICTS, PROJECTS } from './fixtures'
import type { RequestOptions } from './types'

/** One project with its district resolved. Projects has no place column. */
export async function getProjectById(
  projectId: number,
  options?: RequestOptions,
): Promise<ProjectSummary> {
  return mockRequest(options, () => {
    const project = PROJECTS.find((candidate) => candidate.Id === projectId)

    if (project === undefined) {
      throw new ApiError(404, 'That project is not available in Field Notebook.')
    }

    const district = DISTRICTS.find((candidate) => candidate.Id === project.DistrictId)

    return {
      id: project.Id,
      name: project.Name,
      code: project.Code,
      districtName: district?.Name ?? null,
    }
  })
}
