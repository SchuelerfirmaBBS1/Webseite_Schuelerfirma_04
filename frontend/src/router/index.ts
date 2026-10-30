import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/ueber-uns',
      name: 'about',
      // Subpages are lazy-loaded, so they are only downloaded when visited
      component: () => import('../views/AboutView.vue'),
    },
    {
      path: '/kontakt',
      name: 'contact',
      component: () => import('../views/ContactView.vue'),
    },
  ],
})

export default router
