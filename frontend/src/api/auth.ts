// Client for the backend's /api/auth endpoints (AuthController).
// The session lives in an HttpOnly cookie, so the frontend never sees or stores a token.

export interface LoginRequest {
  username: string
  password: string
}

export interface MeResponse {
  userId: number
  username: string
  email: string
  role: string
}

/** Returns false if the credentials were rejected, throws on any other failure. */
export async function login(request: LoginRequest): Promise<boolean> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  // 401: wrong credentials, locked or deactivated account. 400: empty or too long input.
  if (response.status === 401 || response.status === 400) {
    return false
  }
  if (!response.ok) {
    throw new Error(`Login failed with status ${response.status}`)
  }
  return true
}

export async function logout(): Promise<void> {
  const response = await fetch('/api/auth/logout', { method: 'POST' })

  // 401 means the session had already ended, which is the goal anyway.
  if (!response.ok && response.status !== 401) {
    throw new Error(`Logout failed with status ${response.status}`)
  }
}

/** Returns the logged-in user, or null if there is no valid session. */
export async function fetchMe(): Promise<MeResponse | null> {
  const response = await fetch('/api/auth/me')

  if (response.status === 401) {
    return null
  }
  if (!response.ok) {
    throw new Error(`Loading the current user failed with status ${response.status}`)
  }
  return (await response.json()) as MeResponse
}
