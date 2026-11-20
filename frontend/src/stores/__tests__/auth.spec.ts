import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAuthStore } from '../auth'

const me = { userId: 1, username: 'admin', email: 'admin@example.org', role: 'Admin' }

function mockFetch(...responses: Response[]) {
  const fetchMock = vi.fn()
  for (const response of responses) {
    fetchMock.mockResolvedValueOnce(response)
  }
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

describe('auth store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('starts logged out and not loaded', () => {
    const store = useAuthStore()
    expect(store.isLoggedIn).toBe(false)
    expect(store.loaded).toBe(false)
  })

  it('loads the user after a successful login', async () => {
    const fetchMock = mockFetch(new Response(null, { status: 204 }), Response.json(me))
    const store = useAuthStore()

    const accepted = await store.login({ username: 'admin', password: 'secret' })

    expect(accepted).toBe(true)
    expect(store.isLoggedIn).toBe(true)
    expect(store.user).toEqual(me)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/auth/login',
      expect.objectContaining({ method: 'POST' }),
    )
  })

  it('returns false and stays logged out when credentials are rejected', async () => {
    const fetchMock = mockFetch(new Response(null, { status: 401 }))
    const store = useAuthStore()

    const accepted = await store.login({ username: 'admin', password: 'wrong' })

    expect(accepted).toBe(false)
    expect(store.isLoggedIn).toBe(false)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('throws when the backend fails', async () => {
    mockFetch(new Response(null, { status: 500 }))
    const store = useAuthStore()

    await expect(store.login({ username: 'admin', password: 'secret' })).rejects.toThrow()
  })

  it('treats a 401 from /me as logged out', async () => {
    mockFetch(new Response(null, { status: 401 }))
    const store = useAuthStore()

    await store.loadUser()

    expect(store.loaded).toBe(true)
    expect(store.isLoggedIn).toBe(false)
  })

  it('clears the user on logout', async () => {
    mockFetch(Response.json(me), new Response(null, { status: 204 }))
    const store = useAuthStore()
    await store.loadUser()

    await store.logout()

    expect(store.isLoggedIn).toBe(false)
  })
})
