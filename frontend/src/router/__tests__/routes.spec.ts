import { describe, expect, it } from 'vitest'
import router from '../index'

describe('router', () => {
  it('registers the styleguide in development (tests run with import.meta.env.DEV = true)', () => {
    expect(import.meta.env.DEV).toBe(true)
    expect(router.hasRoute('styleguide')).toBe(true)
  })

  it('keeps the admin area behind requiresAuth', () => {
    const admin = router.getRoutes().find((route) => route.name === 'admin')
    expect(admin?.meta.requiresAuth).toBe(true)
  })
})
