import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import TermineView from '../views/TermineView.vue'
import ArchivView from '../views/ArchivView.vue'
import FrontpageView from '../views/FrontpageView.vue'
import ContactView from '../views/ContactView.vue'
import { useAuthStore } from '@/stores/auth'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
  }
}

// Dev-only pages. import.meta.env.DEV is replaced by `false` in production builds,
// so these routes and their components are dropped from the bundle entirely.
const devRoutes: RouteRecordRaw[] = import.meta.env.DEV
  ? [
      {
        path: '/styleguide',
        name: 'styleguide',
        component: () => import('../views/dev/StyleguideView.vue'),
      },
    ]
  : []

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'startseite',
      component: FrontpageView,
    },
    {
      path: '/termine',
      name: 'termine',
      component: TermineView,
    },
    {
      path: '/archiv',
      name: 'archiv',
      component: ArchivView,
    },
    {
      path: '/kontakt',
      name: 'kontakt',
      component: ContactView,
    },
    {
      path: '/admin/login',
      name: 'admin-login',
      // Admin pages are lazy-loaded, so public visitors never download them
      component: () => import('../views/admin/LoginView.vue'),
    },
    {
      path: '/admin',
      name: 'admin',
      component: () => import('../views/admin/AdminView.vue'),
      meta: { requiresAuth: true },
    },
    ...devRoutes,
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
