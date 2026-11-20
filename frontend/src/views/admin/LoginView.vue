<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import type { LoginRequest } from '@/api/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const form = reactive<LoginRequest>({ username: '', password: '' })
const error = ref('')
const submitting = ref(false)

// Only follow redirects within this site, so a crafted link can't send users elsewhere after login
function redirectTarget(): string {
  const redirect = route.query.redirect
  if (typeof redirect === 'string' && redirect.startsWith('/') && !redirect.startsWith('//')) {
    return redirect
  }
  return '/admin'
}

async function submit() {
  error.value = ''
  submitting.value = true
  try {
    const accepted = await auth.login(form)
    if (!accepted) {
      error.value = 'Benutzername oder Passwort ist falsch.'
      return
    }
    await router.push(redirectTarget())
  } catch {
    error.value = 'Anmeldung zurzeit nicht möglich. Bitte später erneut versuchen.'
  } finally {
    form.password = ''
    submitting.value = false
  }
}
</script>

<template>
  <h1>Admin-Anmeldung</h1>

  <!-- Tailwind's base styles remove input borders, so "border" keeps the fields visible -->
  <form @submit.prevent="submit">
    <div>
      <label for="username" class="block">Benutzername</label>
      <input
        id="username"
        v-model="form.username"
        type="text"
        autocomplete="username"
        class="border"
        required
      />
    </div>

    <div>
      <label for="password" class="block">Passwort</label>
      <input
        id="password"
        v-model="form.password"
        type="password"
        autocomplete="current-password"
        class="border"
        required
      />
    </div>

    <p v-if="error" role="alert">{{ error }}</p>

    <button type="submit" class="border" :disabled="submitting">Anmelden</button>
  </form>
</template>
