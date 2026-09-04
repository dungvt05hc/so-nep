import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'

// UI copy stays in Vietnamese on purpose: the users are teachers and pupils.
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <p>Hệ thống theo dõi nề nếp — giao diện sẽ được dựng ở Phase 1.</p>
  </StrictMode>,
)
