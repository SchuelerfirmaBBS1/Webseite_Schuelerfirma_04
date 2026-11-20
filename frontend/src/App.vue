<script setup lang="ts"></script>
import { RouterLink, RouterView } from 'vue-router'
import AppHeader from './components/AppHeader.vue'
import AppFooter from './components/AppFooter.vue'
  { to: '/', label: 'Home' },
  { to: '/ueber-uns', label: 'Über uns' },
  { to: '/kontakt', label: 'Kontakt' },
]
</script>
<template>
  <AppHeader />

  <RouterView />

  <AppFooter />
  <h1 class="text-3xl font-bold text-red-500">You did it!</h1>
  <p>
    Visit <a href="https://vuejs.org/" target="_blank" rel="noopener">vuejs.org</a> to read the
    documentation
  </p>
</template>
