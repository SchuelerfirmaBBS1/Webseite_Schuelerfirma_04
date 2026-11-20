import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import * as authApi from '@/api/auth'
import type { LoginRequest, MeResponse } from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<MeResponse | null>(null)
  // Whether the session has been checked against the backend since the page was loaded
  const loaded = ref(false)
  const isLoggedIn = computed(() => user.value !== null)

  async function loadUser() {
    user.value = await authApi.fetchMe()
    loaded.value = true
  }

  /** Returns false if the credentials were rejected. */
  async function login(request: LoginRequest): Promise<boolean> {
    const accepted = await authApi.login(request)
    if (accepted) {
      await loadUser()
    }
    return accepted
  }

  async function logout() {
    await authApi.logout()
    user.value = null
  }

  return { user, loaded, isLoggedIn, loadUser, login, logout }
})
