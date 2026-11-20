import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
  }
}

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
    {
      path: '/admin/login',
      name: 'admin-login',
      component: () => import('../views/admin/LoginView.vue'),
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('../views/admin/AdminView.vue'),
      meta: { requiresAuth: true },
    },
  ],
})

// The backend enforces access to admin data; this guard only keeps logged-out users off admin pages
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth || to.name === 'admin-login') {
    if (!auth.loaded) {
      try {
        await auth.loadUser()
      } catch {
        // Backend unreachable: treat as logged out, the login page shows the error on submit
      }
    }
  }

  if (to.meta.requiresAuth && !auth.isLoggedIn) {
    return { name: 'admin-login', query: { redirect: to.fullPath } }
  }
  if (to.name === 'admin-login' && auth.isLoggedIn) {
    return { name: 'admin' }
  }
})

export default router
