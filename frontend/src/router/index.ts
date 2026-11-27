
import { createRouter, createWebHistory } from 'vue-router'
import TermineView from '../views/TermineView.vue'
import ArchivView from '../views/ArchivView.vue'
import FrontpageView from '../views/FrontpageView.vue'
import ContactView from '../views/ContactView.vue'

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
      path: '/kontaktfolmular',
      name: 'kontaktformular',
      component: ContactView,
    },
  ],
})

export default router
