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
  <main class="admin-page">
    <div class="login-content">
      <header class="page-head">
        <p class="page-label">PRINT&amp;BITE · INTERN</p>
        <h1>Admin-<span>Anmeldung.</span></h1>
        <p class="page-description">
          Dieser Bereich ist nur für das Team der Schülerfirma.
        </p>
      </header>

      <form class="login-form" @submit.prevent="submit">
        <div class="form-group">
          <label for="username">Benutzername</label>
          <input
            id="username"
            v-model="form.username"
            type="text"
            autocomplete="username"
            required
          />
        </div>

        <div class="form-group">
          <label for="password">Passwort</label>
          <input
            id="password"
            v-model="form.password"
            type="password"
            autocomplete="current-password"
            required
          />
        </div>

        <p v-if="error" class="form-error" role="alert">{{ error }}</p>

        <button type="submit" class="submit-button" :disabled="submitting">
          {{ submitting ? 'Anmelden …' : 'Anmelden' }}
          <span aria-hidden="true">→</span>
        </button>
      </form>
    </div>
  </main>
</template>

<style scoped src="../../assets/css/admin/admin.css"></style>

<style scoped>
.login-content {
  display: grid;
  grid-template-columns: 0.9fr 1.1fr;
  align-items: start;
  gap: 5rem;
  max-width: 1100px;
  margin: 0 auto;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1.2rem;
  padding: 2rem;
  background: #ffffff;
}

@media (max-width: 850px) {
  .login-content {
    grid-template-columns: 1fr;
    gap: 2.5rem;
  }
}
</style>
