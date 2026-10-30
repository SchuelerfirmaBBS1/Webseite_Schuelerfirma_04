<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'

const navItems = [
  { to: '/', label: 'Home' },
  { to: '/ueber-uns', label: 'Über uns' },
  { to: '/kontakt', label: 'Kontakt' },
]
</script>

<template>
  <div class="flex min-h-screen flex-col">
    <header class="border-b border-gray-200">
      <nav class="mx-auto flex max-w-5xl items-center justify-between px-4 py-4">
        <RouterLink to="/" class="text-lg font-bold">Schülerfirma BBS1</RouterLink>
        <ul class="flex gap-6">
          <li v-for="item in navItems" :key="item.to">
            <RouterLink
              :to="item.to"
              class="hover:underline"
              exact-active-class="font-semibold underline"
            >
              {{ item.label }}
            </RouterLink>
          </li>
        </ul>
      </nav>
    </header>

    <main class="mx-auto w-full max-w-5xl flex-1 px-4 py-10">
      <RouterView />
    </main>

    <footer class="border-t border-gray-200">
      <p class="mx-auto max-w-5xl px-4 py-6 text-sm text-gray-500">
        Platzhalter für Footer (Impressum, Datenschutz)
      </p>
    </footer>
  </div>
</template>
