// Production verification config: runs the same e2e suite against the
// Docker+Nginx deployment at http://localhost:8080 (no dev server).
import base from './playwright.config'

export default {
  ...base,
  use: { ...(base as any).use, baseURL: 'http://localhost:8080' },
  webServer: undefined
}
