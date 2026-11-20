<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

async function logout() {
  await auth.logout()
  await router.push({ name: 'admin-login' })
}
</script>

<template>
  <h1>Admin-Bereich</h1>
  <p>Angemeldet als {{ auth.user?.username }}</p>
  <p>Platzhalter für die Verwaltung (Termine, Kontaktanfragen, Einstellungen).</p>
  <button type="button" class="border" @click="logout">Abmelden</button>
</template>
