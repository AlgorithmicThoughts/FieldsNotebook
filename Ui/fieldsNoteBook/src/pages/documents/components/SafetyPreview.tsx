/** Sample section names. The approved design marks each one as a sample. */
const SECTIONS = ['Site contacts', 'Hazard controls', 'Emergency response', 'Access and permits']

/** The sample safety reference: section names only, never real instructions. */
export function SafetyPreview() {
  return (
    <>
      <h3>Sections in the site reference</h3>

      <ol className="mt-3 list-none">
        {SECTIONS.map((name, index) => (
          <li key={name} className="sample-doc-row">
            <span>
              {index + 1}. {name}
            </span>
            <span className="sample-doc-note">Sample section</span>
          </li>
        ))}
      </ol>
    </>
  )
}
