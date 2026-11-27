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
  <main class="admin-page">
    <div class="admin-content">
      <header class="admin-head">
        <div class="page-head">
          <p class="page-label">PRINT&amp;BITE · INTERN</p>
          <h1>Admin-<span>Bereich.</span></h1>
          <p class="page-description">
            Angemeldet als <strong>{{ auth.user?.username }}</strong>
          </p>
        </div>

        <button type="button" class="secondary-button" @click="logout">Abmelden</button>
      </header>

      <section class="admin-placeholder" aria-label="Verwaltung">
        <p>Platzhalter für die Verwaltung (Termine, Kontaktanfragen, Einstellungen).</p>
      </section>
    </div>
  </main>
</template>

<style scoped src="../../assets/css/admin/admin.css"></style>

<style scoped>
.admin-content {
  max-width: 1100px;
  margin: 0 auto;
}

.admin-head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 2rem;
  padding-bottom: 2rem;
  border-bottom: 1px solid var(--border);
}

.admin-placeholder {
  margin-top: 2.5rem;
  padding: 2rem;
  background: #ffffff;
  color: var(--muted);
  font-size: 0.9rem;
  line-height: 1.8;
}

.admin-placeholder p {
  margin: 0;
}

@media (max-width: 700px) {
  .admin-head {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
